using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Screen room for the stat column right of the minimap. The game draws the small map 40 units in from the screen's
    /// right edge, less than the column needs, so while the column shows a box and the map is on (small or big) the map
    /// moves left by what is missing, and the status effects left of it move with it so the two never overlap. With no box
    /// shown, the map off (a world without a map, the player's choice) or PackPanel off, both go back where they were.
    /// Positions only, local only.
    /// </summary>
    public static class HudRoom
    {
        /// <summary>Units between the column's right edge and the screen's.</summary>
        private const float Margin = 10f;

        private static readonly Spot map = new Spot();
        private static readonly Spot effects = new Spot();

        /// <summary>Moves the map and the status effects for this column (null: no column, both go home).</summary>
        public static void Fit(RectTransform column)
        {
            Minimap minimap = Minimap.instance;
            RectTransform small = minimap != null && minimap.m_smallRoot != null ? minimap.m_smallRoot.transform as RectTransform : null;
            if (small == null) return;
            bool used = column != null && minimap.m_mode != Minimap.MapMode.None && Shows(column);
            float shift = used ? Mathf.Max(0f, Need(column) + map.Home(small)) : 0f;
            map.Shift(small, shift);
            RectTransform list = Hud.instance != null ? Hud.instance.m_statusEffectListRoot : null;
            if (list != null) effects.Shift(list, shift);
        }

        /// <summary>From the map's right edge to the screen's: the gap, the column's width on screen and the margin.</summary>
        private static float Need(RectTransform column) =>
            column.anchoredPosition.x + column.rect.width * column.localScale.x + Margin;

        private static bool Shows(Transform column)
        {
            for (int i = 0; i < column.childCount; i++)
                if (column.GetChild(i).gameObject.activeSelf) return true;
            return false;
        }

        /// <summary>
        /// Where one piece of the game's HUD sat before it was moved: its x, anchored to the screen's right edge (negative,
        /// the room to its right), read the first time the piece is seen, again for a new one after a new world loads.
        /// </summary>
        private sealed class Spot
        {
            private RectTransform rect;
            private float home;

            public float Home(RectTransform target)
            {
                if (rect != target)
                {
                    rect = target;
                    home = target.anchoredPosition.x;
                }
                return home;
            }

            public void Shift(RectTransform target, float shift)
            {
                float x = Home(target) - shift;
                if (!Mathf.Approximately(target.anchoredPosition.x, x))
                    target.anchoredPosition = new Vector2(x, target.anchoredPosition.y);
            }
        }
    }
}
