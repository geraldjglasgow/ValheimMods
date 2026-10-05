using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The game's hammer build table (the "Hammer" item's build pieces in the world's item database) and OpenKeep's
    /// entries in it. The entries are appended after the game's pieces and taken out again; nothing else in the table is
    /// touched. They carry one build category the hammer does not use (the first of Deep North, Feasts, Food, Meads that
    /// neither the table's categories nor any of its pieces use, picked once per table and logged), so the hammer's own
    /// category lists and their selected places never change; while entries are in, that category is also listed in the
    /// table's categories with the label "Blueprints".
    /// </summary>
    public static class HammerTable
    {
        private const string HammerItem = "Hammer";

        private static readonly Piece.PieceCategory[] Candidates =
        {
            Piece.PieceCategory.DeepNorth, Piece.PieceCategory.Feasts, Piece.PieceCategory.Food, Piece.PieceCategory.Meads,
        };

        private static PieceTable pickedFor;
        private static Piece.PieceCategory? picked;
        private static GameObject lastPut;

        /// <summary>The hammer's build table, or null before the item database is up.</summary>
        public static PieceTable Find()
        {
            GameObject hammer = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(HammerItem) : null;
            ItemDrop drop = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared.m_buildPieces : null;
        }

        /// <summary>The table is the hammer's.</summary>
        public static bool Is(PieceTable table) => table != null && table == Find();

        /// <summary>The entries last put in are still the table's last pieces (no other mod rebuilt the list since).</summary>
        public static bool Intact(PieceTable table)
        {
            List<GameObject> pieces = table.m_pieces;
            return lastPut == null || (pieces.Count > 0 && pieces[pieces.Count - 1] == lastPut);
        }

        /// <summary>Replaces OpenKeep's entries in the table with these (none: the category goes too).</summary>
        public static void Put(PieceTable table, List<GameObject> entries)
        {
            table.m_pieces.RemoveAll(IsEntry);
            lastPut = null;
            if (entries.Count == 0)
            {
                RemoveCategory(table);
                return;
            }
            Piece.PieceCategory? category = CategoryFor(table);
            foreach (GameObject entry in entries)
                entry.GetComponent<Piece>().m_category = category ?? Piece.PieceCategory.Misc;
            table.m_pieces.AddRange(entries);
            lastPut = entries[entries.Count - 1];
            if (category.HasValue && !table.m_categories.Contains(category.Value))
            {
                table.m_categories.Add(category.Value);
                table.m_categoryLabels.Add(BlueprintWords.Tab);
            }
        }

        private static bool IsEntry(GameObject go) => go != null && BlueprintMenu.IsOurs(go.GetComponent<Piece>());

        /// <summary>The category is listed no more; a hammer that had it selected goes back to its first category.</summary>
        private static void RemoveCategory(PieceTable table)
        {
            if (table != pickedFor || !picked.HasValue)
                return;
            int index = table.m_categories.IndexOf(picked.Value);
            if (index < 0)
                return;
            table.m_categories.RemoveAt(index);
            if (index < table.m_categoryLabels.Count)
                table.m_categoryLabels.RemoveAt(index);
            if (table.GetSelectedCategory() == picked.Value)
                table.SetCategory(0);
        }

        /// <summary>The category for this table's entries, picked once: null (Misc, not listed) when the hammer uses all four.</summary>
        private static Piece.PieceCategory? CategoryFor(PieceTable table)
        {
            if (table == pickedFor)
                return picked;
            HashSet<Piece.PieceCategory> used = new HashSet<Piece.PieceCategory>(table.m_categories);
            foreach (GameObject go in table.m_pieces)
            {
                Piece piece = go != null ? go.GetComponent<Piece>() : null;
                if (piece != null)
                    used.Add(piece.m_category);
            }
            pickedFor = table;
            picked = Pick(used);
            Plugin.Log.LogInfo(picked.HasValue
                ? $"OpenKeep: the hammer's Blueprints entries use build category {picked.Value}"
                : "OpenKeep: the hammer uses every spare build category; the Blueprints entries share Misc");
            return picked;
        }

        private static Piece.PieceCategory? Pick(HashSet<Piece.PieceCategory> used)
        {
            foreach (Piece.PieceCategory candidate in Candidates)
            {
                if (!used.Contains(candidate))
                    return candidate;
            }
            return null;
        }
    }
}
