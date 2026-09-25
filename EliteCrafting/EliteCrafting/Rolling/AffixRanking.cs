using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The two precise selections of sigils.md section 3, used by removals and by the Sigil of Preservation:
    /// the <b>culled</b> affix (dormant first, then lowest tier, then worst roll, then earliest) and the
    /// <b>protected</b> affix (live only: highest tier, then best roll, then earliest). The bound affix is never
    /// either. A roll's quality is its position inside its tier's range; flags, single-value tiers and tiers the
    /// definition no longer has count as 1.
    /// </summary>
    internal static class AffixRanking
    {
        /// <summary>Index of the affix a Culling removal takes, or -1 when every affix is bound or skipped.</summary>
        public static int Culled(ItemState state, string? skipId)
        {
            int best = -1;
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (!Removable(state, i, skipId))
                {
                    continue;
                }
                if (best < 0 || CullsBefore(state, i, best))
                {
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Index of the affix Preservation protects, or -1 when no live unbound affix exists.</summary>
        public static int Protected(ItemState state)
        {
            int best = -1;
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (!state.IsActiveAt(i) || state.IsBoundAt(i))
                {
                    continue;
                }
                if (best < 0 || ProtectsBefore(state, i, best))
                {
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Every affix but the bound one and <paramref name="skipId"/> may be removed; dormant ones included.</summary>
        public static bool Removable(ItemState state, int index, string? skipId) =>
            !state.IsBoundAt(index) && state.Affixes[index].Id != skipId;

        /// <summary>0 (worst) to 1 (best) inside the stored tier's current range.</summary>
        public static float Quality(ItemState state, int index)
        {
            AffixRoll roll = state.Affixes[index];
            AffixDef? def = state.DefinitionAt(index);
            AffixTierDef? row = def?.TierRow(roll.Tier);
            if (def == null || row == null || def.Value == AffixValueType.Flag)
            {
                return 1f;
            }
            return RollMath.Position(roll.Value, row.Min, row.Max);
        }

        // Strictly earlier in the cull order than the current pick (ties keep the earlier index).
        private static bool CullsBefore(ItemState state, int candidate, int current)
        {
            bool dormantA = !state.IsActiveAt(candidate);
            bool dormantB = !state.IsActiveAt(current);
            if (dormantA != dormantB)
            {
                return dormantA;
            }
            if (dormantA)
            {
                return false;
            }
            int tierA = state.Affixes[candidate].Tier;
            int tierB = state.Affixes[current].Tier;
            if (tierA != tierB)
            {
                return tierA < tierB;
            }
            return Quality(state, candidate) < Quality(state, current);
        }

        private static bool ProtectsBefore(ItemState state, int candidate, int current)
        {
            int tierA = state.Affixes[candidate].Tier;
            int tierB = state.Affixes[current].Tier;
            if (tierA != tierB)
            {
                return tierA > tierB;
            }
            return Quality(state, candidate) > Quality(state, current);
        }
    }
}
