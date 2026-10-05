using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Core;
using Wayfare.Portals;
using Wayfare.Targeting;

namespace Wayfare.SeaGates
{
    /// <summary>Sea gates on the large map: an icon of their own (<see cref="SeaGateMapSprites"/>) and the gate's name,
    /// on the same layer above every pin as the portal icons (<see cref="MapIconLayer"/>, <see cref="MapOverlay"/>),
    /// never the game's own pins. Shown while the picker is open, and on the ordinary large map while the player has
    /// map icons toggled on (<see cref="HotkeyToggle"/>), never during portal targeting, where a sea gate is no
    /// destination. Only the gates the local player may target are drawn, plus, in the picker, the gate the ship
    /// stopped in, always, in gold. The list is asked for again every <see cref="RequestSeconds"/> while icons show.
    /// Icons are placed through the map image's own rectangle, the same
    /// arithmetic the game uses for its pins, so they sit right whatever the layer's anchors are.</summary>
    internal static class SeaGateMapIcons
    {
        private const float IconSize = 24f;
        private const float SourceSize = 30f;
        private const float RequestSeconds = 5f;
        private static readonly Color SourceLabel = new Color(1f, 0.85f, 0.4f, 1f);

        private sealed class Icon
        {
            public RectTransform Root;
            public Image Image;
            public Text Label;
        }

        private static readonly Dictionary<long, Icon> icons = new Dictionary<long, Icon>();
        private static readonly HashSet<long> seen = new HashSet<long>();   // reused every frame
        private static float requestedAt = float.NegativeInfinity;
        private static string defaultName;

        /// <summary>Asks the server for the list now and restarts the poll.</summary>
        internal static void RequestNow()
        {
            requestedAt = Time.time;
            SeaGateIndex.RequestFromServer();
        }

        internal static void Tick()
        {
            if (!ShouldShow())
            {
                Clear();
                return;
            }
            if (Time.time - requestedAt >= RequestSeconds)
                RequestNow();
            defaultName = Localization.instance.Localize(SeaGateWords.DefaultName);
            seen.Clear();
            LoadedGate source = SeaGatePicker.Source;
            ShowListed(source != null ? source.Id : 0L, seen);
            if (source != null)
                Show(source.Id, (source.Anchor.transform.position + source.Partner.transform.position) * 0.5f, source.Name, true, seen);
            RemoveStale(seen);
        }

        private static bool ShouldShow()
        {
            Minimap map = Minimap.instance;
            if (Player.m_localPlayer == null || map == null || map.m_mode != Minimap.MapMode.Large || MapIconLayer.Root == null)
                return false;
            return SeaGatePicker.Active || (HotkeyToggle.IconsOn && !TargetingSession.Active);
        }

        private static void ShowListed(long sourceId, HashSet<long> seen)
        {
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            foreach (SeaGateInfo info in SeaGateIndex.Gates)
            {
                if (info.Id != sourceId && PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                    Show(info.Id, info.Position, info.Name, false, seen);
            }
        }

        private static void Show(long id, Vector3 position, string name, bool isSource, HashSet<long> seen)
        {
            seen.Add(id);
            if (!icons.TryGetValue(id, out Icon icon) || icon.Root == null)
                icons[id] = icon = BuildIcon();
            bool visible = Place(icon.Root, position);
            if (icon.Root.gameObject.activeSelf != visible)
                icon.Root.gameObject.SetActive(visible);
            if (visible)
                Style(icon, name, isSource);
        }

        /// <summary>Puts an icon over a world position, as the game places its own pins; false when that spot is off the
        /// visible part of the map.</summary>
        internal static bool Place(RectTransform root, Vector3 world)
        {
            Minimap map = Minimap.instance;
            RawImage image = map.m_mapImageLarge;
            if (!map.IsPointVisible(world, image))
                return false;
            map.WorldToMapPoint(world, out float mx, out float my);
            Vector2 local = map.MapPointToLocalGuiPos(mx, my, image);
            RectTransform rect = image.rectTransform;
            root.position = rect.TransformPoint(rect.rect.min + local);
            return true;
        }

        private static void Style(Icon icon, string name, bool isSource)
        {
            float size = MapIconLayer.IconSize(isSource ? SourceSize / IconSize : 1f);
            icon.Root.sizeDelta = new Vector2(size, size);
            icon.Image.sprite = isSource ? SeaGateMapSprites.Source : SeaGateMapSprites.Gate;
            icon.Label.text = string.IsNullOrEmpty(name) ? defaultName : name;
            icon.Label.color = isSource ? SourceLabel : Color.white;
            if (isSource)
                icon.Root.SetAsLastSibling();
        }

        /// <summary>The gate whose icon is under a screen point, the nearest when icons overlap.</summary>
        internal static bool TryHit(Vector2 screenPos, out long gateId)
        {
            gateId = 0L;
            float best = float.MaxValue;
            foreach (KeyValuePair<long, Icon> entry in icons)
            {
                RectTransform root = entry.Value.Root;
                if (root == null || !root.gameObject.activeInHierarchy || !RectTransformUtility.RectangleContainsScreenPoint(root, screenPos, null))
                    continue;
                float distance = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, root.position), screenPos);
                if (distance < best)
                {
                    best = distance;
                    gateId = entry.Key;
                }
            }
            return gateId != 0L;
        }

        private static void RemoveStale(HashSet<long> seen)
        {
            List<long> stale = null;
            foreach (long id in icons.Keys)
            {
                if (!seen.Contains(id))
                    (stale ??= new List<long>()).Add(id);
            }
            if (stale == null)
                return;
            foreach (long id in stale)
            {
                if (icons[id].Root != null)
                    Object.Destroy(icons[id].Root.gameObject);
                icons.Remove(id);
            }
        }

        internal static void Clear()
        {
            if (icons.Count == 0)
                return;
            foreach (Icon icon in icons.Values)
            {
                if (icon.Root != null)
                    Object.Destroy(icon.Root.gameObject);
            }
            icons.Clear();
        }

        private static Icon BuildIcon()
        {
            GameObject go = new GameObject("Wayfare.SeaGateIcon", typeof(RectTransform), typeof(Image));
            RectTransform root = (RectTransform)go.transform;
            root.SetParent(MapIconLayer.Root, worldPositionStays: false);
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
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }
    }
}
