using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// Who is in the column and in what order. The members are the game's armour and weight plates (ranks
    /// <see cref="Column.ArmorRank"/> and <see cref="Column.WeightRank"/>) and every object named as a column box,
    /// <c>PlateColumn_plate_&lt;rank:D4&gt;_&lt;id&gt;</c>, active or not; lower ranks sit higher, equal ranks by name.
    /// The rank lives in the name so every copy of this library, each merged into its own mod, reads the same order from
    /// the scene. The hidden <c>PlateColumn_mark_first</c> / <c>_last</c> markers older copies left on the panel are
    /// not members and are no longer used.
    /// </summary>
    internal static class ColumnLayout
    {
        private const string PlatePrefix = "PlateColumn_plate_";

        /// <summary>Before this library, OpenKeep made its trash plate under this name, between the game's two plates.</summary>
        private const string LegacyTrash = "OpenKeep_trash";
        private const int LegacyTrashRank = 200;

        public static string NameOf(PlateSpec spec) => $"{PlatePrefix}{spec.Rank:D4}_{spec.Id}";

        public static bool IsMember(Transform t, GamePlates game) => KeyOf(t, game) != null;

        /// <summary>The members among <paramref name="parent"/>'s children, top to bottom.</summary>
        public static List<RectTransform> Sorted(Transform parent, GamePlates game)
        {
            List<KeyValuePair<string, RectTransform>> found = new List<KeyValuePair<string, RectTransform>>();
            foreach (Transform child in parent)
            {
                if (child is RectTransform rect && KeyOf(child, game) is string key)
                {
                    found.Add(new KeyValuePair<string, RectTransform>(key, rect));
                }
            }
            found.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return found.ConvertAll(pair => pair.Value);
        }

        /// <summary>The member's sort key (rank, then name), null for anything that is not a member.</summary>
        private static string? KeyOf(Transform t, GamePlates game)
        {
            if (t == game.Armor)
            {
                return SortKey(Column.ArmorRank, "");
            }
            if (t == game.Weight)
            {
                return SortKey(Column.WeightRank, "");
            }
            return RankOf(t.name) is int rank ? SortKey(rank, t.name) : null;
        }

        private static string SortKey(int rank, string name) => $"{rank:D4}{name}";

        private static int? RankOf(string name)
        {
            if (name == LegacyTrash)
            {
                return LegacyTrashRank;
            }
            if (!name.StartsWith(PlatePrefix, StringComparison.Ordinal) || name.Length < PlatePrefix.Length + 4)
            {
                return null;
            }
            return int.TryParse(name.Substring(PlatePrefix.Length, 4), out int rank) ? rank : (int?)null;
        }
    }
}
