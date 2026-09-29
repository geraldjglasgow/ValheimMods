using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// The HUD row's container, <c>PlateColumn_hudboxes</c>: a child of the minimap's small root
    /// (<c>Minimap.m_smallRoot</c>, the framed square at the top right, which clips nothing; only its map image does),
    /// anchored to that square's bottom-right corner with its pivot at its own top-right, <see cref="HudRow.Gap"/> units
    /// below it and scaled to <see cref="HudRow.Scale"/>. A <c>HorizontalLayoutGroup</c> (right aligned, children keep
    /// their size) puts the active boxes side by side and a <c>ContentSizeFitter</c> makes the row as wide as they are,
    /// so it grows to the left from the map's right edge. Found by name, so every copy of this library shares it.
    /// </summary>
    internal static class HudContainer
    {
        public const string Name = "PlateColumn_hudboxes";

        /// <summary>The container, made on first use; null while there is no minimap.</summary>
        public static RectTransform? Get()
        {
            GameObject? small = Minimap.instance != null ? Minimap.instance.m_smallRoot : null;
            if (small == null)
            {
                return null;
            }
            return small.transform.Find(Name) is RectTransform found ? found : Make(small.transform);
        }

        private static RectTransform Make(Transform small)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform));
            RectTransform row = (RectTransform)go.transform;
            row.SetParent(small, false);
            row.anchorMin = row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(1f, 1f);
            row.anchoredPosition = new Vector2(0f, -HudRow.Gap);
            row.localScale = Vector3.one * HudRow.Scale;
            HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.spacing = Column.Spacing;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<ColumnWatch>();
            return row;
        }
    }
}
