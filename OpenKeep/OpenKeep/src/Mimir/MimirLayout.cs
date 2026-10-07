using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The container panel while a Mímir's Chest is open (user, 2026-10-07): one toolbar row taller (<see cref="Strip"/>)
    /// between the title and the grid, so the search field and the quick filters cover neither the chest's name nor its
    /// buttons; no Take all (a chest that never fills would pour everything into the player) and no Sort below
    /// the panel (the sort buttons take Take all's place); the scroll bar at the right
    /// edge, running from the top of the first row to the bottom of the grid. The panel's pivot is its top-left corner,
    /// so it grows downwards: the grid's top inset grows by the row and the centre-anchored title buttons move up by half
    /// the growth to stay where they were. Every rect touched is saved first and put back as it was when any other
    /// container opens. It only moves what the panel holds, so it works with or without PackPanel.
    /// </summary>
    public static class MimirLayout
    {
        public const float Strip = 44f;
        public const float TitleRow = 46f;
        public const float Margin = 16f;

        /// <summary>The right end of the toolbar row, left of the scroll bar's column.</summary>
        public const float RowRight = 570f - 28f;

        private const float ScrollWidth = 10f;
        private const float ScrollRight = 4f;
        private const float ScrollTop = 4f;
        private const float ScrollBottom = 10f;
        private const string OwnPrefix = "OpenKeep_Mimir";

        /// <summary>OpenKeep's Sort button under the panel: the chest sorts itself (<see cref="MimirSortButtons"/>).</summary>
        private const string SortButton = "OpenKeep_ok_store_sort";

        private struct Saved
        {
            public Vector2 AnchorMin, AnchorMax, Pivot, Position, Size;
            public bool Active;
        }

        private static readonly Dictionary<RectTransform, Saved> saved = new Dictionary<RectTransform, Saved>();
        private static RectTransform grown;

        /// <summary>Every frame the container grid draws: lays the panel out for a Mímir's Chest, puts it back for anything else.</summary>
        public static void Apply(InventoryGui gui, bool mimir)
        {
            RectTransform panel = gui.m_container;
            if (panel == null || mimir == (grown == panel))
                return;
            Restore();
            if (mimir)
                Grow(gui, panel);
        }

        private static void Grow(InventoryGui gui, RectTransform panel)
        {
            grown = panel;
            Save(panel);
            float half = panel.sizeDelta.y / 2f;
            panel.sizeDelta += new Vector2(0f, Strip);
            Transform grid = gui.m_containerGrid != null ? gui.m_containerGrid.transform : null;
            Transform scroll = gui.m_containerGrid != null && gui.m_containerGrid.m_scrollbar != null ? gui.m_containerGrid.m_scrollbar.transform : null;
            foreach (Transform child in panel)
            {
                if (!(child is RectTransform rect) || child.name.StartsWith(OwnPrefix))
                    continue;
                if (child == grid)
                    MoveGrid(rect);
                else if (child == scroll)
                    PlaceScroll(rect);
                else if ((gui.m_takeAllButton != null && child == gui.m_takeAllButton.transform) || child.name == SortButton)
                    Hide(rect);
                else
                    MoveTitleRow(rect, half);
            }
        }

        private static void MoveGrid(RectTransform rect)
        {
            Save(rect);
            rect.anchoredPosition -= new Vector2(0f, Strip / 2f);
            rect.sizeDelta -= new Vector2(0f, Strip);
        }

        /// <summary>Right edge, from the first row's top to the grid's bottom.</summary>
        private static void PlaceScroll(RectTransform rect)
        {
            Save(rect);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-ScrollRight - ScrollWidth, ScrollBottom);
            rect.offsetMax = new Vector2(-ScrollRight, -(TitleRow + Strip + ScrollTop));
        }

        private static void Hide(RectTransform rect)
        {
            Save(rect);
            rect.gameObject.SetActive(false);
        }

        /// <summary>Centre-anchored children: those in the title row stay at the top, the rest move down with the grid.</summary>
        private static void MoveTitleRow(RectTransform rect, float half)
        {
            if (!Mathf.Approximately(rect.anchorMin.y, 0.5f) || !Mathf.Approximately(rect.anchorMax.y, 0.5f))
                return;
            Save(rect);
            bool titleRow = rect.anchoredPosition.y > half - TitleRow;
            rect.anchoredPosition += new Vector2(0f, titleRow ? Strip / 2f : -Strip / 2f);
        }

        private static void Save(RectTransform rect)
        {
            if (!saved.ContainsKey(rect))
                saved[rect] = new Saved
                {
                    AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax, Pivot = rect.pivot,
                    Position = rect.anchoredPosition, Size = rect.sizeDelta, Active = rect.gameObject.activeSelf,
                };
        }

        private static void Restore()
        {
            foreach (KeyValuePair<RectTransform, Saved> entry in saved)
            {
                RectTransform rect = entry.Key;
                if (rect == null)
                    continue;
                rect.anchorMin = entry.Value.AnchorMin;
                rect.anchorMax = entry.Value.AnchorMax;
                rect.pivot = entry.Value.Pivot;
                rect.anchoredPosition = entry.Value.Position;
                rect.sizeDelta = entry.Value.Size;
                rect.gameObject.SetActive(entry.Value.Active);
            }
            saved.Clear();
            grown = null;
        }
    }
}
