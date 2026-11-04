using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a dock costs, all in its material's one item (wood, core wood or fine wood): 1 per 2 m² of deck, 1 per 4 m of
    /// its length (joists, cap beams and curbs) and 1 per 4 m of pilings, rounded up for each section, so the section's
    /// piece gives back exactly what it took when it is removed with the hammer (its own requirements, set from its saved
    /// shape on every peer, <see cref="KwPier"/>; the game's own drop on removal or when it breaks). Plain numbers.
    /// </summary>
    public static class KwPierCost
    {
        private const float PerDeckArea = 0.5f, PerLength = 0.25f, PerPiling = 0.25f;

        /// <summary>One section's cost, in its material's item (at least 1).</summary>
        public static int Amount(KwPierSpec spec)
        {
            float piles = 0f;
            foreach (Vector2 depth in spec.Depths)
                piles += depth.x + depth.y;
            float cost = spec.Length * spec.Width * PerDeckArea + spec.Length * PerLength + piles * PerPiling;
            return Mathf.Max(1, Mathf.CeilToInt(cost - 0.001f));
        }

        /// <summary>Several sections' cost together.</summary>
        public static int Amount(IEnumerable<KwPierSpec> sections)
        {
            int total = 0;
            foreach (KwPierSpec spec in sections)
                total += Amount(spec);
            return total;
        }

        /// <summary>
        /// The cost as the game's requirements: the item and amount (recovered: a built section's, given back when it is
        /// removed; not: the tool's, which the game takes at the click). None when the game lacks the item.
        /// </summary>
        public static Piece.Requirement[] Requirements(KwPierKind kind, int amount, bool recover)
        {
            ItemDrop? drop = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(kind.Item)?.GetComponent<ItemDrop>() : null;
            if (drop == null || amount <= 0)
                return new Piece.Requirement[0];
            return new[] { new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = recover } };
        }

        /// <summary>The cost as the item list <see cref="KwBpCost"/> checks and words.</summary>
        public static List<(string Item, int Amount)> Items(KwPierKind kind, int amount) => new List<(string Item, int Amount)> { (kind.Item, amount) };
    }
}
