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
    /// entire lifetime of a headless dedicated server (no <c>Minimap.instance</c>, no local player). Every portal the
    /// player may target is drawn, tagged or not; the portal the player stands at is drawn still, marked "You are
    /// here", and takes no clicks. Icons are made once and kept: an icon panned or zoomed off the map, or every icon
    /// when the map closes, is only hidden and shows again as it was; the icon of a portal that left the list goes back
    /// to a spare pool for the next portal that needs one. Nothing runs for a hidden icon.</summary>
    public static class MapOverlay
    {
        private static readonly Dictionary<ZDOID, PortalIcon> icons = new Dictionary<ZDOID, PortalIcon>();   // shown or hidden
        private static readonly List<PortalIcon> spare = new List<PortalIcon>();                               // hidden, any portal
        private static readonly HashSet<ZDOID> seen = new HashSet<ZDOID>();                                    // reused every frame
        private static readonly HashSet<ZDOID> listed = new HashSet<ZDOID>();
        private static readonly List<ZDOID> gone = new List<ZDOID>();
        private static GameObject driver;
        private static bool showing;
        private static int keptFor = -1;   // the registry version the icons were last sorted against

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
                if (entry.Key == here || zone == null || !zone.gameObject.activeInHierarchy ||
                    !RectTransformUtility.RectangleContainsScreenPoint(zone, screenPos, null))
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
                HideAll();
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
            ReleaseGone();
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            ZDOID here = TargetingSession.SourceId;
            seen.Clear();
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (!Shown(info, info.Id == here, playerId, isAdmin))
                    continue;
                seen.Add(info.Id);
                Place(info, info.Id == here);
            }
            HideUnseen();
            showing = true;
        }

        private static bool Shown(PortalInfo info, bool isHere, long playerId, bool isAdmin)
        {
            if (!isHere && !PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                return false;
            return Minimap.instance.IsPointVisible(info.Position, Minimap.instance.m_mapImageLarge);
        }

        private static void Place(PortalInfo info, bool isHere)
        {
            if (!icons.TryGetValue(info.Id, out PortalIcon icon) || icon.Root == null)
                icons[info.Id] = icon = Take();
            if (!icon.Root.gameObject.activeSelf)
                icon.Root.gameObject.SetActive(true);
            Minimap.instance.WorldToMapPoint(info.Position, out float mx, out float my);
            icon.Root.anchoredPosition = Minimap.instance.MapPointToLocalGuiPos(mx, my, Minimap.instance.m_mapImageLarge);
            float size = MapIconLayer.IconSize(pulse: !isHere);
            icon.Root.sizeDelta = new Vector2(size, size);
            float click = MapIconLayer.ClickSize();
            icon.Zone.sizeDelta = new Vector2(click, click);
            icon.Show(info.Id, isHere, WayfareConfig.ShowTags.Value ? info.Tag ?? "" : null);
        }

        /// <summary>A spare icon (one whose map is gone is dropped), else a new one.</summary>
        private static PortalIcon Take()
        {
            while (spare.Count > 0)
            {
                PortalIcon icon = spare[spare.Count - 1];
                spare.RemoveAt(spare.Count - 1);
                if (icon.Root != null)
                    return icon;
            }
            return PortalIcon.Make(MapIconLayer.Root);
        }

        /// <summary>Hides the icons of listed portals not drawn this frame (off the visible map, or no longer open to
        /// this player); they keep their place in <see cref="icons"/>.</summary>
        private static void HideUnseen()
        {
            foreach (KeyValuePair<ZDOID, PortalIcon> entry in icons)
            {
                if (!seen.Contains(entry.Key))
                    Hide(entry.Value);
            }
        }

        /// <summary>When the portal list has changed: the icons of portals no longer in it go to the spare pool, and
        /// icons whose map is gone (another world loaded) are forgotten.</summary>
        private static void ReleaseGone()
        {
            if (keptFor == PortalRegistry.Version)
                return;
            keptFor = PortalRegistry.Version;
            listed.Clear();
            foreach (PortalInfo info in PortalRegistry.Portals)
                listed.Add(info.Id);
            gone.Clear();
            foreach (KeyValuePair<ZDOID, PortalIcon> entry in icons)
            {
                if (!listed.Contains(entry.Key) || entry.Value.Root == null)
                    gone.Add(entry.Key);
            }
            foreach (ZDOID id in gone)
            {
                PortalIcon icon = icons[id];
                icons.Remove(id);
                if (icon.Root != null)
                    spare.Add(Hide(icon));
            }
        }

        /// <summary>The map closed or icons are off: every icon is hidden, once.</summary>
        private static void HideAll()
        {
            if (!showing)
                return;
            showing = false;
            foreach (PortalIcon icon in icons.Values)
                Hide(icon);
        }

        private static PortalIcon Hide(PortalIcon icon)
        {
            if (icon.Root != null && icon.Root.gameObject.activeSelf)
                icon.Root.gameObject.SetActive(false);
            return icon;
        }

        private sealed class Ticker : MonoBehaviour
        {
            private void LateUpdate() => Tick(); // after Minimap.Update moved the map this frame, so icons do not trail it
        }
    }
}
