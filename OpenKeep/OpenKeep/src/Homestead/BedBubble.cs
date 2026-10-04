using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The click area around a bed while the player chooses where to wake (asked 2026-10-04: "a bubble area around it
    /// to click", then: no bigger than the bed icon at its largest, and invisible). An empty rect centred on each bed
    /// icon, as wide as the icon at the top of its pulse (the game's large map pin size, doubled for the choice), so the
    /// area holds still while the icon pulses. A click anywhere inside it picks that bed (<see cref="BedChoiceClickPatch"/>,
    /// the nearest bed when two overlap): <see cref="WorldRadius"/> turns its size on screen into metres on the map at
    /// the current zoom. Nothing is drawn and nothing takes the pointer, so the click still reaches the map.
    /// </summary>
    public static class BedBubble
    {
        private const string Name = "OpenKeep_bedbubble";

        private static RectTransform measured;
        private static readonly Vector3[] corners = new Vector3[4];

        /// <summary>A click area on the bed icon, unless it has one.</summary>
        public static void Add(RectTransform icon, Minimap map)
        {
            if (icon == null || map == null || icon.Find(Name) != null)
                return;
            GameObject go = new GameObject(Name, typeof(RectTransform));
            go.layer = icon.gameObject.layer;
            go.transform.SetParent(icon, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            float largest = map.m_pinSizeLarge * 2f;
            rect.sizeDelta = new Vector2(largest, largest);
            measured = rect;
        }

        /// <summary>The click area's radius in metres on the map now; 0 while none shows.</summary>
        public static float WorldRadius(Minimap map)
        {
            if (map == null || measured == null || !measured.gameObject.activeInHierarchy)
                return 0f;
            measured.GetWorldCorners(corners);
            Vector3 centre = (corners[0] + corners[2]) / 2f;
            Vector3 edge = centre + new Vector3((corners[2].x - corners[0].x) / 2f, 0f, 0f);
            return BedPoints.MapDistance(map.ScreenToWorldPoint(centre), map.ScreenToWorldPoint(edge));
        }
    }
}
