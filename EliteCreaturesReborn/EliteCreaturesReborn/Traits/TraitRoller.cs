using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Rolls a fresh creature's traits from its biome's rules: a star count drawn from the biome's star-chance
    /// distribution, then every enabled mutation rolled independently at its own biome-and-star chance. A creature
    /// that fails every roll is plain; one that passes several carries several. `max mutations` caps the set. The world
    /// tier leans both rolls: its star boost multiplies each star count's weight once per star, and its mutation boost
    /// multiplies every mutation chance, so a hardened world keeps each biome's character but pushes it upward.
    /// </summary>
    public static class TraitRoller
    {
        public static CreatureTraits Roll(BiomeRules rules, RuleSet ruleSet, int tier)
        {
            int stars = RollStars(rules.StarChances, ruleSet.Tiers.StarBoostAt(tier));
            int mask = RollMutations(rules, stars, ruleSet, ruleSet.Tiers.MutationBoostAt(tier));
            return new CreatureTraits(stars, mask) { Tier = tier };
        }

        /// <summary>A plain draw from the distribution, no tier - the boss table's roll.</summary>
        public static int RollStars(BiomeRules rules) => RollStars(rules.StarChances, 1f);

        private static int RollStars(float[] chances, float boost)
        {
            float[] weights = Boosted(chances, boost);
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

        private static int RollMutations(BiomeRules rules, int stars, RuleSet ruleSet, float boost)
        {
            List<Mutation> hits = new List<Mutation>();
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if (ruleSet.IsEnabled(mutation) && Dice.Percent(Mathf.Min(100f, rules.ChanceOf(mutation, stars) * boost)))
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
