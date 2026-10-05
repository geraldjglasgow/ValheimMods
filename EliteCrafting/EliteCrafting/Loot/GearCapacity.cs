using System.Collections.Generic;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// How many affixes a fresh roll can put on an item type at its item level (drops.md section 8, rarity.md section 4
    /// "The pool is exhausted", classes-and-tiers.md sections 5 and 6): candidates in the class's pool that pass
    /// <c>requires</c> and have a tier open at the level (<see cref="TierEligibility.HasEligible"/>), where an exclusion
    /// group counts once, split by kind, then held to a rarity's prefix and suffix limits. Computed at pool build,
    /// never per kill.
    /// </summary>
    internal readonly struct GearCapacity
    {
        private GearCapacity(int prefixes, int suffixes)
        {
            Prefixes = prefixes;
            Suffixes = suffixes;
        }

        public int Prefixes { get; }
        public int Suffixes { get; }

        /// <summary>The most a fresh roll at this rarity can hold: each kind up to its limit, all up to the maximum count.</summary>
        public int For(RarityDef rarity)
        {
            int fits = System.Math.Min(Prefixes, rarity.MaxPrefixes) + System.Math.Min(Suffixes, rarity.MaxSuffixes);
            return System.Math.Min(fits, rarity.MaxAffixes);
        }

        public static GearCapacity Of(ClassInfo info, int level, RuleSet rules)
        {
            HashSet<string> groups = new HashSet<string>(System.StringComparer.Ordinal);
            int prefixes = 0, suffixes = 0;
            foreach (PoolEntry entry in rules.Affixes.Pool(info.ClassId))
            {
                AffixDef def = entry.Def;
                if (!ItemClasses.Satisfies(info, def.Requires) || !TierEligibility.HasEligible(def, entry.Fit, level, rules))
                {
                    continue;
                }
                if (def.ExclusionGroup == null || groups.Add(def.ExclusionGroup))
                {
                    prefixes += def.Kind == AffixKind.Prefix ? 1 : 0;
                    suffixes += def.Kind == AffixKind.Suffix ? 1 : 0;
                }
            }
            return new GearCapacity(prefixes, suffixes);
        }
    }
}
