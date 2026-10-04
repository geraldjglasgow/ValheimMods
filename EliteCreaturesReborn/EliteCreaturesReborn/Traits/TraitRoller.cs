using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Rolls a fresh creature's traits from its rules - its biome's, with its own `creatures:` entry's mutation keys on
    /// top (<see cref="RuleSet.For(Heightmap.Biome, string)"/>): a star count drawn from the biome's star-chance
    /// distribution, then every enabled mutation rolled independently at its own biome-and-star chance. A creature
    /// that fails every roll is plain; one that passes several carries several. `max mutations` caps the set. The world
    /// tier leans both rolls: its star boost multiplies each star count's weight once per star, and its mutation boost
    /// multiplies every mutation chance, so a hardened world keeps each biome's character but pushes it upward. A large
    /// creature never rolls Gilded or Relentless (<see cref="BodySize.Barred"/>): they are left out before the dice, so
    /// its chance at every other mutation is unchanged. That is the Custom difficulty; under a preset the roll is
    /// <see cref="PresetRoller"/>'s instead.
    /// </summary>
    public static class TraitRoller
    {
        /// <summary>A fresh creature's roll; <paramref name="barred"/> is the trait mask its body rules out.</summary>
        public static CreatureTraits Roll(BiomeRules rules, RuleSet ruleSet, int tier, Heightmap.Biome biome, int barred)
        {
            if (ruleSet.Difficulty != Difficulty.Custom)
            {
                return PresetRoller.Roll(rules, ruleSet, tier, biome, barred);
            }
            int stars = RollStars(rules.StarChances, ruleSet.Tiers.StarBoostAt(tier));
            int mask = RollMutations(rules, stars, ruleSet, ruleSet.Tiers.MutationBoostAt(tier), barred);
            return new CreatureTraits(stars, mask) { Tier = tier };
        }

        /// <summary>True when a creature may roll it: on in `mutations enabled` and not barred by its body.</summary>
        internal static bool MayRoll(Mutation mutation, RuleSet ruleSet, int barred) =>
            ruleSet.IsEnabled(mutation) && (barred & (1 << (int)mutation)) == 0;

        /// <summary>A plain draw from the distribution, no tier - the boss table's roll.</summary>
        public static int RollStars(BiomeRules rules) => RollStars(rules.StarChances, 1f);

        private static int RollStars(float[] chances, float boost) => PickWeighted(Boosted(chances, boost));

        /// <summary>An index drawn in proportion to its weight (weights at or below 0 never win); 0 when all are 0.</summary>
        internal static int PickWeighted(float[] weights)
        {
            float total = 0f;
            foreach (float weight in weights)
            {
                total += weight;
            }
            if (total <= 0f)
            {
                return 0;
            }
            return PickIndex(weights, Random.Range(0f, total));
        }

        // Each star count's weight times boost^stars. A boost of 1 leaves the distribution exactly as written; the
        // draw is taken against the new total, which is the same as scaling the row back to 100.
        private static float[] Boosted(float[] chances, float boost)
        {
            float[] weights = new float[chances.Length];
            float factor = 1f;
            for (int stars = 0; stars < chances.Length; stars++)
            {
                weights[stars] = Mathf.Max(0f, chances[stars]) * factor;
                factor *= Mathf.Max(0f, boost);
            }
            return weights;
        }

        private static int PickIndex(float[] chances, float draw)
        {
            float running = 0f;
            for (int stars = 0; stars < chances.Length; stars++)
            {
                running += Mathf.Max(0f, chances[stars]);
                if (draw < running)
                {
                    return stars;
                }
            }
            return chances.Length - 1;
        }

        private static int RollMutations(BiomeRules rules, int stars, RuleSet ruleSet, float boost, int barred)
        {
            List<Mutation> hits = new List<Mutation>();
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                float chance = Mathf.Min(100f, rules.ChanceOf(mutation, stars) * boost);
                if (MayRoll(mutation, ruleSet, barred) && Dice.Percent(chance))
                {
                    hits.Add(mutation);
                }
            }
            Cap(hits, ruleSet.MaxMutations);
            return Pack(hits);
        }

        private static void Cap(List<Mutation> hits, int maxMutations)
        {
            if (maxMutations <= 0 || hits.Count <= maxMutations)
            {
                return;
            }
            for (int i = hits.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (hits[i], hits[j]) = (hits[j], hits[i]);
            }
            hits.RemoveRange(maxMutations, hits.Count - maxMutations);
        }

        private static int Pack(List<Mutation> hits)
        {
            int mask = 0;
            foreach (Mutation mutation in hits)
            {
                mask |= 1 << (int)mutation;
            }
            return mask;
        }
    }
}
