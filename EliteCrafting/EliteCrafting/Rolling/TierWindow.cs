using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// Which tiers of an affix a roll may pick, and the pick itself (rarity.md section 4, item-tier.md section 6).
    /// Ordinary rolls: the tiers inside <c>[max(ceiling - window + 1, floor, 1), ceiling]</c>; an affix with none
    /// there but tiers below is eligible at its highest tier at or below the ceiling, if that is at least the floor.
    /// Chaotic rolls (the Serpent): every tier the affix defines, drawn uniformly.
    /// </summary>
    internal static class TierWindow
    {
        /// <summary>
        /// Fills <paramref name="into"/> with the eligible tier rows; empty = the affix cannot roll here. A tier row of
        /// weight 0 never rolls in an ordinary roll, and an affix whose every tier weighs 0 never rolls at all, not even
        /// chaotically (configuration.md section 4: "weight 0 ... on all its tiers: it never rolls").
        /// </summary>
        public static void Eligible(AffixDef def, RollContext context, List<AffixTierDef> into)
        {
            into.Clear();
            if (context.Chaotic)
            {
                AddChaotic(def, into);
                return;
            }
            int ceiling = RollMath.ClampCeiling(context.Ceiling);
            int floor = RollMath.EffectiveFloor(ceiling, context.TierFloor);
            int low = RollMath.WindowLow(ceiling, context.Rules.Economy.Rolling.TierWindow, floor);
            for (int i = 0; i < def.Tiers.Count; i++)
            {
                AffixTierDef row = def.Tiers[i];
                if (row.Weight > 0f && row.Tier >= low && row.Tier <= ceiling)
                {
                    into.Add(row);
                }
            }
            if (into.Count == 0)
            {
                AddFallback(def, ceiling, floor, into);
            }
        }

        // Every tier the affix defines, weights ignored (item-tier.md), unless no tier may roll at all.
        private static void AddChaotic(AffixDef def, List<AffixTierDef> into)
        {
            for (int i = 0; i < def.Tiers.Count; i++)
            {
                if (def.Tiers[i].Weight > 0f)
                {
                    into.AddRange(def.Tiers);
                    return;
                }
            }
        }

        /// <summary>Picks a row: by tier weight, or uniformly on a chaotic roll or when every weight is 0.</summary>
        public static AffixTierDef Pick(List<AffixTierDef> rows, RollContext context, List<float> scratch)
        {
            if (!context.Chaotic)
            {
                scratch.Clear();
                for (int i = 0; i < rows.Count; i++)
                {
                    scratch.Add(rows[i].Weight);
                }
                int index = RollMath.PickWeighted(scratch, context.Random);
                if (index >= 0)
                {
                    return rows[index];
                }
            }
            return rows[context.Random.Next(rows.Count)];
        }

        // The affix stopped scaling below the window: its highest tier at or below the ceiling, never below the floor.
        private static void AddFallback(AffixDef def, int ceiling, int floor, List<AffixTierDef> into)
        {
            AffixTierDef? best = null;
            for (int i = 0; i < def.Tiers.Count; i++)
            {
                AffixTierDef row = def.Tiers[i];
                if (row.Weight > 0f && row.Tier <= ceiling && (best == null || row.Tier > best.Tier))
                {
                    best = row;
                }
            }
            if (best != null && best.Tier >= Math.Max(floor, 1))
            {
                into.Add(best);
            }
        }
    }
}
