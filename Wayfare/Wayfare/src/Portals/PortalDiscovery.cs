using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.Portals
{
    /// <summary>Widens the game's own portal classification (<c>Game.PortalPrefabHash</c>, the hash list that puts
    /// a ZDO into the world-wide-distributed portal channel) to every prefab carrying a <see cref="TeleportWorld"/>
    /// component, not just the four vanilla ones. Runs lazily and idempotently: <see cref="EnsureDiscovered"/> is
    /// cheap to call repeatedly and does nothing until both <c>Game.instance</c> and <c>ZNetScene.instance</c>
    /// exist, so it never depends on Unity's Awake order. <see cref="EnsureRunning"/> starts a small ticker that
    /// calls it on every machine, including a headless dedicated server - <see cref="PortalRegistry"/>'s own tick
    /// only runs where there is a local player, which a dedicated server never has, and the server is exactly the
    /// machine whose <c>PortalPrefabHash</c> matters most (it decides what gets the world-wide distribution every
    /// client relies on, and what <see cref="Targeting.TeleportGate"/> accepts as a real portal).</summary>
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
            if (game == null || scene == null || scannedFor == game)
                return;
            HashSet<int> known = new HashSet<int>(game.PortalPrefabHash);
            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null || prefab.GetComponentInChildren<TeleportWorld>() == null)
                    continue;
                int hash = prefab.name.GetStableHashCode();
                if (known.Add(hash))
                    game.PortalPrefabHash.Add(hash);
            }
            scannedFor = game;
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
