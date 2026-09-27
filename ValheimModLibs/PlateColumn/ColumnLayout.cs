using System.Collections.Generic;
using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// Where each plate goes. The members are the game's armour and weight plates and every active child of the panel
    /// named as a column plate, sorted by rank. They are spaced evenly over the span between the game's own two plate
    /// positions, centred on its middle, but never closer than <see cref="MinGap"/> apart: two plates keep the game's
    /// places, three share them, and a fourth pushes the column out past them evenly. The game's positions are recorded
    /// once per panel on two hidden markers, before anything moves, so the layout reads the same numbers however many
    /// times, and by however many mods, it is applied.
    /// </summary>
    internal static class ColumnLayout
    {
        private const string PlatePrefix = "PlateColumn_plate_";
        private const string FirstMark = "PlateColumn_mark_first";
        private const string LastMark = "PlateColumn_mark_last";
        private const float MinGap = 8f;

        /// <summary>Before this library, OpenKeep made its trash plate under this name, between the game's two plates.</summary>
        private const string LegacyTrash = "OpenKeep_trash";
        private const int LegacyTrashRank = 200;

        public static string NameOf(PlateSpec spec) => $"{PlatePrefix}{spec.Rank:D4}_{spec.Id}";

        /// <summary>Keeps where the game put its two plates, once per panel, on markers that draw nothing.</summary>
        public static void RecordOrigin(RectTransform panel, RectTransform armor, RectTransform weight)
        {
            if (panel.Find(FirstMark) == null)
            {
                Mark(panel, FirstMark, armor.anchoredPosition);
            }
            if (panel.Find(LastMark) == null)
            {
                Mark(panel, LastMark, weight.anchoredPosition);
            }
        }

        public static void Apply(RectTransform panel, GamePlates game)
        {
            RectTransform? first = panel.Find(FirstMark) as RectTransform;
            RectTransform? last = panel.Find(LastMark) as RectTransform;
            List<RectTransform> column = Members(panel, game);
            if (first == null || last == null || column.Count < 2)
            {
                return;
            }
            Vector2 top = first.anchoredPosition;
            Vector2 bottom = last.anchoredPosition;
            float step = Mathf.Max(Mathf.Abs(top.y - bottom.y) / (column.Count - 1), game.Armor.rect.height + MinGap);
            float start = (top.y + bottom.y) / 2f + step * (column.Count - 1) / 2f;
            for (int i = 0; i < column.Count; i++)
            {
                GamePlates.PinTopRight(column[i], panel);
                Vector2 at = new Vector2(top.x, start - step * i);
                if (column[i].anchoredPosition != at)
                {
                    column[i].anchoredPosition = at;
                }
            }
        }

        private static void Mark(RectTransform panel, string name, Vector2 position)
        {
            GameObject marker = new GameObject(name, typeof(RectTransform));
            marker.SetActive(false);
            RectTransform rect = (RectTransform)marker.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = position;
        }

        private static List<RectTransform> Members(RectTransform panel, GamePlates game)
        {
            List<KeyValuePair<string, RectTransform>> found = new List<KeyValuePair<string, RectTransform>>
            {
                new KeyValuePair<string, RectTransform>(SortKey(Column.ArmorRank, ""), game.Armor),
                new KeyValuePair<string, RectTransform>(SortKey(Column.WeightRank, ""), game.Weight),
            };
            foreach (Transform child in panel)
            {
                if (child.gameObject.activeSelf && child is RectTransform rect && RankOf(child.name) is int rank)
                {
                    found.Add(new KeyValuePair<string, RectTransform>(SortKey(rank, child.name), rect));
                }
            }
            found.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            List<RectTransform> column = new List<RectTransform>(found.Count);
            foreach (KeyValuePair<string, RectTransform> pair in found)
            {
                column.Add(pair.Value);
            }
            return column;
        }

        private static string SortKey(int rank, string name) => $"{rank:D4}{name}";

        private static int? RankOf(string name)
        {
            if (name == LegacyTrash)
            {
                return LegacyTrashRank;
            }
            if (!name.StartsWith(PlatePrefix) || name.Length < PlatePrefix.Length + 4)
            {
                return null;
            }
            return int.TryParse(name.Substring(PlatePrefix.Length, 4), out int rank) ? rank : (int?)null;
        }
    }
}
