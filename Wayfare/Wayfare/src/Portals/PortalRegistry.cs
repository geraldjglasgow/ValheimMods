using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>The live, client-side snapshot of every known portal, rebuilt roughly every 5 seconds - the same
    /// cadence the game's own portal-reconnect tick runs at - from <c>ZDOMan.GetPortalList()</c>. Never runs on a
    /// pure dedicated server (no local player, nothing to display to); every read no-ops when the world is not
    /// loaded, so it is safe to poll from the moment the plugin starts.</summary>
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
            bool unownedIsPublic = WayfareConfig.UnownedPortalsArePublic.Value;
            snapshot.Clear();
            foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
            {
                if (zdo == null || !zdo.IsValid() || !PortalDiscovery.IsPortalPrefab(zdo.GetPrefab()))
                    continue;
                snapshot.Add(new PortalInfo(zdo.m_uid, zdo.GetPosition(), zdo.GetString(ZDOVars.s_tag),
                    PortalFields.GetMode(zdo, unownedIsPublic), PortalFields.GetOwner(zdo)));
            }
        }

        private sealed class PortalRegistryTicker : MonoBehaviour
        {
            private void Awake() => InvokeRepeating(nameof(Tick), 0f, IntervalSeconds);

            private void Tick() => Refresh();
        }
    }
}
