using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Build cost, applied to the prefab only because placement reads the prefab's Piece. The vanilla amounts are
    /// recorded once at discovery and every application recomputes from them, so repeated changes do not compound.
    /// </summary>
    public static class ShipBuildCost
    {
        /// <summary>The vanilla amount of every material; null without a Piece or without materials.</summary>
        public static int[] ReadOriginals(Piece piece)
        {
            if (piece == null || piece.m_resources == null || piece.m_resources.Length == 0)
                return null;

            int[] amounts = new int[piece.m_resources.Length];
            for (int i = 0; i < amounts.Length; i++)
                amounts[i] = piece.m_resources[i] != null ? piece.m_resources[i].m_amount : 0;
            return amounts;
        }

        /// <summary>Writes vanilla amount times effective cost into the prefab's materials, rounded, never below 1.</summary>
        public static void Apply(Piece piece, ShipEntries entries)
        {
            int[] originals = entries.OriginalBuildCosts;
            if (piece == null || originals == null || piece.m_resources == null)
                return;

            float cost = entries.EffectiveBuildCost;
            int count = Mathf.Min(originals.Length, piece.m_resources.Length);
            for (int i = 0; i < count; i++)
            {
                Piece.Requirement requirement = piece.m_resources[i];
                if (requirement != null)
                    requirement.m_amount = Mathf.Max(1, Mathf.RoundToInt(originals[i] * cost));
            }
        }
    }
}
