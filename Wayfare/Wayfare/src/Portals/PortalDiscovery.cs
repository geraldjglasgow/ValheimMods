using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.Portals
{
    /// <summary>Widens the game's own portal classification (<c>Game.PortalPrefabHash</c>, the hash list that puts
    /// a ZDO into <c>ZDOMan.m_portalObjects</c> - the always-loaded portal list and its own save chunk) to every
    /// prefab carrying a <see cref="TeleportWorld"/> component, not just the four vanilla ones. Runs lazily and
    /// idempotently: <see cref="EnsureDiscovered"/> is cheap to call repeatedly and does nothing until
    /// <c>Game.instance</c>, <c>ZNetScene.instance</c> and <c>ZDOMan.instance</c> all exist, so it never depends
    /// on Unity's Awake order. <see cref="EnsureRunning"/> starts a small ticker that calls it on every machine,
    /// including a headless dedicated server - <see cref="PortalRegistry"/>'s own tick only runs where there is a
    /// local player, which a dedicated server never has, and the server is exactly the machine whose
    /// <c>PortalPrefabHash</c> matters most (its portal list is the one <see cref="PortalSync"/> serves to
    /// clients, and it decides what <see cref="Targeting.TeleportGate"/> accepts as a real portal).</summary>
    public static class PortalDiscovery
    {
        private const float IntervalSeconds = 5f;

        // Not the Game instance we last augmented: a new game session (e.g. returning to the main menu and
        // rejoining) creates a fresh Game and Game.PortalPrefabHash, which needs the scan run again.
        private static Game scannedFor;
        private static GameObject driver;

        public static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.PortalDiscovery") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(driver);
            driver.AddComponent<Ticker>();
        }

        public static void EnsureDiscovered()
        {
            Game game = Game.instance;
            ZNetScene scene = ZNetScene.instance;
            if (game == null || scene == null || ZDOMan.instance == null || scannedFor == game)
                return;
            HashSet<int> known = new HashSet<int>(game.PortalPrefabHash);
            HashSet<int> added = new HashSet<int>();
            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null || prefab.GetComponentInChildren<TeleportWorld>() == null)
                    continue;
                int hash = prefab.name.GetStableHashCode();
                if (known.Add(hash))
                {
                    game.PortalPrefabHash.Add(hash);
                    added.Add(hash);
                }
            }
            scannedFor = game;
            Reclassify(added);
        }

        /// <summary>ZDOs loaded or received before the scan ran were classified against the narrower hash list
        /// and sit in the ordinary sector lists. Once their prefab is in <c>PortalPrefabHash</c> the save-writer
        /// skips them there (<c>ZDOMan.GetSaveClonePerChunk</c>) while the portal chunk is written from
        /// <c>m_portalObjects</c> - left un-moved they would be silently dropped from the next world save.</summary>
        private static void Reclassify(HashSet<int> addedHashes)
        {
            if (addedHashes.Count == 0)
                return;
            List<ZDO> toMove = new List<ZDO>();
            foreach (var pair in ZDOMan.instance.m_objectsByID)
            {
                if (addedHashes.Contains(pair.Value.GetPrefab()))
                    toMove.Add(pair.Value);
            }
            foreach (ZDO zdo in toMove)
                ZDOMan.instance.AddIfPortal(zdo, zdo.GetPrefab());
        }

        public static bool IsPortalPrefab(int prefabHash)
        {
            Game game = Game.instance;
            return game != null && game.PortalPrefabHash.Contains(prefabHash);
        }

        private sealed class Ticker : MonoBehaviour
        {
            private void Awake() => InvokeRepeating(nameof(Tick), 0f, IntervalSeconds);

            private void Tick() => EnsureDiscovered();
        }
    }
}
