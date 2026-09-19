using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>The live, client-side snapshot of every known portal, rebuilt roughly every 5 seconds - the same
    /// cadence the game's own portal-reconnect tick runs at. Where this machine is the server (single player, a
    /// host) it reads <c>ZDOMan.GetPortalList()</c> directly; on a pure client that list only ever holds nearby
    /// portals (the game never distributes distant portal ZDOs to peers), so the tick instead asks the server for
    /// a snapshot through <see cref="PortalSync"/>. Never runs on a pure dedicated server (no local player,
    /// nothing to display to); every read no-ops when the world is not loaded, so it is safe to poll from the
    /// moment the plugin starts.</summary>
    public static class PortalRegistry
    {
        private const float IntervalSeconds = 5f;

        private static readonly List<PortalInfo> snapshot = new List<PortalInfo>();
        private static GameObject driver;

        public static IReadOnlyList<PortalInfo> Portals => snapshot;

        public static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.PortalRegistry");
            driver.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(driver);
            driver.AddComponent<PortalRegistryTicker>();
        }

        internal static void Refresh()
        {
            if (!WayfareConfig.Enabled.Value || ZDOMan.instance == null || Player.m_localPlayer == null)
                return;
            PortalDiscovery.EnsureDiscovered();
            if (ZNet.instance != null && !ZNet.instance.IsServer())
            {
                PortalSync.RequestFromServer();
                return;
            }
            SetSnapshot(ReadLocal());
        }

        /// <summary>The local machine's own <c>GetPortalList()</c> read - complete only where this machine is
        /// the server, which is why <see cref="PortalSync"/> uses it there to answer clients.</summary>
        internal static List<PortalInfo> ReadLocal()
        {
            bool unownedIsPublic = WayfareConfig.UnownedPortalsArePublic.Value;
            List<PortalInfo> portals = new List<PortalInfo>();
            foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
            {
                if (zdo == null || !zdo.IsValid() || !PortalDiscovery.IsPortalPrefab(zdo.GetPrefab()))
                    continue;
                portals.Add(new PortalInfo(zdo.m_uid, zdo.GetPosition(), zdo.GetString(ZDOVars.s_tag),
                    PortalFields.GetMode(zdo, unownedIsPublic), PortalFields.GetOwner(zdo)));
            }
            return portals;
        }

        internal static void SetSnapshot(List<PortalInfo> portals)
        {
            snapshot.Clear();
            snapshot.AddRange(portals);
        }

        private sealed class PortalRegistryTicker : MonoBehaviour
        {
            private void Awake() => InvokeRepeating(nameof(Tick), 0f, IntervalSeconds);

            private void Tick() => Refresh();
        }
    }
}
