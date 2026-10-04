using UnityEngine;
using Wayfare.Core;

namespace Wayfare.Targeting
{
    /// <summary>The layer Wayfare's map icons live on: one plain RectTransform stretched over the large map and kept as
    /// its last child, so portal and sea gate icons draw over the game's pins, the player and ship markers and other
    /// mods' map overlays. Its lower-left corner is where the game measures pin positions from
    /// (<c>Minimap.MapPointToLocalGuiPos</c>; the game's pin prefab is anchored there), so an icon anchored at (0, 0)
    /// takes that position as is. While the player is choosing a destination (portal targeting, the sea gate picker)
    /// icons are larger and slowly grow and shrink, so they stand out; on the ordinary map they keep their normal size.</summary>
    public static class MapIconLayer
    {
        private const float NormalSize = 24f;
        private const float ChoosingSize = 36f;
        private const float PulseAmount = 0.15f;
        private const float PulseSeconds = 1.2f;

        private static RectTransform layer;

        /// <summary>The layer, made on first use under the map and put back on top when something was added after it;
        /// null while there is no large map.</summary>
        public static RectTransform Root
        {
            get
            {
                Minimap map = Minimap.instance;
                RectTransform mapRect = map != null && map.m_pinRootLarge != null ? map.m_pinRootLarge.parent as RectTransform : null;
                if (mapRect == null)
                    return null;
                if (layer == null)
                    layer = Build(mapRect);
                if (layer.GetSiblingIndex() != mapRect.childCount - 1)
                    layer.SetAsLastSibling();
                return layer;
            }
        }

        public static bool Choosing => TargetingSession.Active || SeaGates.SeaGatePicker.Active;

        /// <summary>An icon's side this frame: normal, or larger and pulsing while choosing; <paramref name="factor"/>
        /// scales it further (the sea gate picker's own gate is drawn larger still).</summary>
        public static float IconSize(float factor = 1f)
        {
            float size = (Choosing ? ChoosingSize : NormalSize) * factor * Mathf.Max(0.25f, WayfareConfig.IconScale.Value);
            return Choosing ? size * (1f + PulseAmount * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / PulseSeconds)) : size;
        }

        private static RectTransform Build(RectTransform mapRect)
        {
            GameObject go = new GameObject("Wayfare.MapIcons", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(mapRect, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            return rect;
        }
    }
}
