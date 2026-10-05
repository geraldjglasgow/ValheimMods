using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>Draws Wayfare's own portal icons on the large map - a plain <c>UnityEngine.UI</c> overlay on Wayfare's
    /// own map layer (<see cref="MapIconLayer"/>, above every pin and marker), kept deliberately separate from Minimap's <c>PinData</c>/<c>AddPin</c> system
    /// (see PLAN.md: that system's own click-to-toggle and save behaviour would otherwise run on a portal icon
    /// too). Visible whenever a targeting session is active, or the player toggled icons on with the hotkey while
    /// the large map is open; every frame it runs is guarded by <see cref="ShouldShow"/>, which is false for the
    /// entire lifetime of a headless dedicated server (no <c>Minimap.instance</c>, no local player). Only portals with a
    /// tag are drawn (<see cref="PortalFields.HasTag(string)"/>); the portal the player stands at is drawn still, marked
    /// "You are here", and takes no clicks.</summary>
    public static class MapOverlay
    {
        private static readonly Dictionary<ZDOID, PortalIcon> icons = new Dictionary<ZDOID, PortalIcon>();
        private static GameObject driver;

        public static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.MapOverlay") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(driver);
            driver.AddComponent<Ticker>();
        }

        /// <summary>The portal whose click area holds the point, the nearest when two overlap; never the portal the
        /// player stands at.</summary>
        public static bool TryHitTest(Vector2 screenPos, out ZDOID hit)
        {
            hit = ZDOID.None;
            ZDOID here = TargetingSession.SourceId;
            float best = float.MaxValue;
            foreach (KeyValuePair<ZDOID, PortalIcon> entry in icons)
            {
                RectTransform zone = entry.Value.Zone;
                if (entry.Key == here || !RectTransformUtility.RectangleContainsScreenPoint(zone, screenPos, null))
                    continue;
                float distance = ((Vector2)zone.position - screenPos).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    hit = entry.Key;
                }
            }
            return hit != ZDOID.None;
        }

        private static bool ShouldShow()
        {
            return WayfareConfig.Enabled.Value && Player.m_localPlayer != null && Minimap.instance != null &&
                   Minimap.instance.m_mode == Minimap.MapMode.Large && (TargetingSession.Active || HotkeyToggle.IconsOn) &&
                   !SeaGates.SeaGatePicker.Active; // the sea gate picker shows sea gates only
        }

        private static void Tick()
        {
            if (!ShouldShow())
            {
                Clear();
                FavouritesPanel.Clear();
                return;
            }
            Rebuild();
            FavouritesPanel.Tick();
        }

        private static void Rebuild()
        {
            if (MapIconLayer.Root == null)
                return;
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            ZDOID here = TargetingSession.SourceId;
            HashSet<ZDOID> seen = new HashSet<ZDOID>();
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (!Shown(info, info.Id == here, playerId, isAdmin))
                    continue;
                seen.Add(info.Id);
                Place(info, info.Id == here);
            }
            RemoveStale(seen);
        }

        private static bool Shown(PortalInfo info, bool isHere, long playerId, bool isAdmin)
        {
            if (!PortalFields.HasTag(info.Tag))
                return false;
            if (!isHere && !PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                return false;
            return Minimap.instance.IsPointVisible(info.Position, Minimap.instance.m_mapImageLarge);
        }

        private static void Place(PortalInfo info, bool isHere)
        {
            if (!icons.TryGetValue(info.Id, out PortalIcon icon))
                icons[info.Id] = icon = PortalIcon.Make(MapIconLayer.Root);
            Minimap.instance.WorldToMapPoint(info.Position, out float mx, out float my);
            icon.Root.anchoredPosition = Minimap.instance.MapPointToLocalGuiPos(mx, my, Minimap.instance.m_mapImageLarge);
            float size = MapIconLayer.IconSize(pulse: !isHere);
            icon.Root.sizeDelta = new Vector2(size, size);
            float click = MapIconLayer.ClickSize();
            icon.Zone.sizeDelta = new Vector2(click, click);
            icon.Image.sprite = IconFactory.Portal;
            icon.Image.color = IconFactory.Gold;
            icon.Ring.gameObject.SetActive(PlayerFavourites.IsFavourite(info.Id));
            icon.Here.gameObject.SetActive(isHere);
            bool showTag = WayfareConfig.ShowTags.Value;
            icon.Label.gameObject.SetActive(showTag);
            if (showTag)
                icon.Label.text = info.Tag;
        }

        private static void RemoveStale(HashSet<ZDOID> seen)
        {
            List<ZDOID> stale = null;
            foreach (ZDOID id in icons.Keys)
            {
                if (!seen.Contains(id))
                    (stale ??= new List<ZDOID>()).Add(id);
            }
            if (stale == null)
                return;
            foreach (ZDOID id in stale)
            {
                Object.Destroy(icons[id].Root.gameObject);
                icons.Remove(id);
            }
        }

        private static void Clear()
        {
            foreach (PortalIcon icon in icons.Values)
            {
                if (icon.Root != null)
                    Object.Destroy(icon.Root.gameObject);
            }
            icons.Clear();
        }

        private sealed class Ticker : MonoBehaviour
        {
            private void Update() => Tick();
        }
    }
}
