using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>Draws Wayfare's own portal icons on the large map - a plain <c>UnityEngine.UI</c> overlay parented
    /// under Minimap's own pin root, kept deliberately separate from Minimap's <c>PinData</c>/<c>AddPin</c> system
    /// (see PLAN.md: that system's own click-to-toggle and save behaviour would otherwise run on a portal icon
    /// too). Visible whenever a targeting session is active, or the player toggled icons on with the hotkey while
    /// the large map is open; every frame it runs is guarded by <see cref="ShouldShow"/>, which is false for the
    /// entire lifetime of a headless dedicated server (no <c>Minimap.instance</c>, no local player).</summary>
    public static class MapOverlay
    {
        private sealed class Icon
        {
            public RectTransform Root;
            public Image Image;
            public Text Label;
        }

        private static readonly Dictionary<ZDOID, Icon> icons = new Dictionary<ZDOID, Icon>();
        private static GameObject driver;

        public static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.MapOverlay") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(driver);
            driver.AddComponent<Ticker>();
        }

        public static bool TryHitTest(Vector2 screenPos, out ZDOID hit)
        {
            foreach (KeyValuePair<ZDOID, Icon> entry in icons)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(entry.Value.Root, screenPos, null))
                {
                    hit = entry.Key;
                    return true;
                }
            }
            hit = ZDOID.None;
            return false;
        }

        private static bool ShouldShow()
        {
            return WayfareConfig.Enabled.Value && Player.m_localPlayer != null && Minimap.instance != null &&
                   Minimap.instance.m_mode == Minimap.MapMode.Large && (TargetingSession.Active || HotkeyToggle.IconsOn);
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
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            HashSet<ZDOID> seen = new HashSet<ZDOID>();
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (!PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                    continue;
                if (!Minimap.instance.IsPointVisible(info.Position, Minimap.instance.m_mapImageLarge))
                    continue;
                seen.Add(info.Id);
                Place(info);
            }
            RemoveStale(seen);
        }

        private static void Place(PortalInfo info)
        {
            if (!icons.TryGetValue(info.Id, out Icon icon))
                icons[info.Id] = icon = BuildIcon();
            Minimap.instance.WorldToMapPoint(info.Position, out float mx, out float my);
            icon.Root.anchoredPosition = Minimap.instance.MapPointToLocalGuiPos(mx, my, Minimap.instance.m_mapImageLarge);
            float size = 24f * Mathf.Max(0.25f, WayfareConfig.IconScale.Value);
            icon.Root.sizeDelta = new Vector2(size, size);
            icon.Image.sprite = PlayerFavourites.IsFavourite(info.Id) ? IconFactory.Favourite : IconFactory.Portal;
            bool showTag = WayfareConfig.ShowTags.Value && !string.IsNullOrEmpty(info.Tag);
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
            foreach (Icon icon in icons.Values)
            {
                if (icon.Root != null)
                    Object.Destroy(icon.Root.gameObject);
            }
            icons.Clear();
        }

        private static Icon BuildIcon()
        {
            GameObject go = new GameObject("Wayfare.PortalIcon", typeof(RectTransform), typeof(Image));
            RectTransform root = (RectTransform)go.transform;
            root.SetParent(Minimap.instance.m_pinRootLarge, worldPositionStays: false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return new Icon { Root = root, Image = image, Label = BuildLabel(root) };
        }

        private static Text BuildLabel(RectTransform parent)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -2f);
            rect.sizeDelta = new Vector2(160f, 20f);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private sealed class Ticker : MonoBehaviour
        {
            private void Update() => Tick();
        }
    }
}
