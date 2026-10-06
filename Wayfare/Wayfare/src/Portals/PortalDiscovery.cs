using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

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
    /// clients, and it decides what <see cref="Targeting.TeleportGate"/> accepts as a real portal). The one pass over
    /// every ZDO that moves the widened prefabs into the portal list runs right after a world loads
    /// (<see cref="WorldLoadPatch"/>), inside the game's own loading, when the scene is ready by then; only otherwise
    /// does the ticker's first run do it.</summary>
    public static class PortalDiscovery
    {
        private const float IntervalSeconds = 5f;

        // Not the Game instance we last augmented: a new game session (e.g. returning to the main menu and
        // rejoining) creates a fresh Game and Game.PortalPrefabHash, which needs the scan run again.
        private static Game scannedFor;
        private static GameObject driver;
        private static bool loadFailed;

        // The prefab hashes the scan for scannedFor added: a world loaded after the scan moves its ZDOs of these.
        private static readonly HashSet<int> added = new HashSet<int>();

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
            Discover(game, scene);
            scannedFor = game;
            Reclassify(added);
        }

        /// <summary>After <c>ZDOMan.LoadChunks</c> or <c>Load</c> (the server's world): ZDOs saved outside the portal
        /// chunk sit in the sector lists. Never throws into the game's world load.</summary>
        internal static void WorldLoaded()
        {
            try
            {
                if (scannedFor != null && scannedFor == Game.instance && ZDOMan.instance != null)
                    Reclassify(added);
                else
                    EnsureDiscovered();
            }
            catch (Exception e)
            {
                if (!loadFailed)
                    Plugin.Log.LogError($"Wayfare: portal discovery after the world load failed (logged once): {e}");
                loadFailed = true;
            }
        }

        private static void Discover(Game game, ZNetScene scene)
        {
            added.Clear();
            HashSet<int> known = new HashSet<int>(game.PortalPrefabHash);
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

    /// <summary>A loaded world is reclassified at once, while the game is still loading, not on a later frame of
    /// play: the chunked save here, the older single-file one in <see cref="WorldFileLoadPatch"/>.</summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.LoadChunks))]
    public static class WorldLoadPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => PortalDiscovery.WorldLoaded();
    }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.Load))]
    public static class WorldFileLoadPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => PortalDiscovery.WorldLoaded();
    }
}
