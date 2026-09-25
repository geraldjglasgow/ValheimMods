using System.Collections.Generic;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// How many affixes a fresh roll can put on an item type at its tier ceiling (drops.md section 8, rarity.md section
    /// 4 "The pool is exhausted"): candidates that list the slot, pass <c>requires</c> and have a tier at or below the
    /// ceiling (the window never makes an affix ineligible, only its lowest tier being above the ceiling does), where an
    /// exclusion group counts once. Computed at pool build, never per kill.
    /// </summary>
    internal static class GearCapacity
    {
        public static int Of(SlotInfo slot, int ceiling, AffixRules affixes)
        {
            HashSet<string> groups = new HashSet<string>(System.StringComparer.Ordinal);
            int count = CountPool(affixes.Pool(slot.Slot, false), slot, ceiling, groups);
            return count + CountPool(affixes.Pool(slot.Slot, true), slot, ceiling, groups);
        }

        private static int CountPool(IReadOnlyList<AffixDef> pool, SlotInfo slot, int ceiling, HashSet<string> groups)
        {
            int count = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                AffixDef def = pool[i];
                if (def.Tiers.Count == 0 || def.MinTier > ceiling || !ItemSlots.Satisfies(slot, def.Requires))
                {
                    continue;
                }
                if (def.ExclusionGroup == null || groups.Add(def.ExclusionGroup))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
