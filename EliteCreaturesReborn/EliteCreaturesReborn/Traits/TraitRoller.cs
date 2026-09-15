using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Rolls a fresh creature's traits from its biome's rules: a star count drawn from the biome's straight star-chance
    /// distribution, then every one of the nine mutations rolled independently at its own biome-and-star chance. A
    /// creature that fails every roll is plain; one that passes several carries several. `max mutations` caps the set.
    /// </summary>
    public static class TraitRoller
    {
        public static CreatureTraits Roll(BiomeRules rules, int maxMutations)
        {
            int stars = RollStars(rules);
            int mask = RollMutations(rules, stars, maxMutations);
            return new CreatureTraits(stars, mask);
        }

        public static int RollStars(BiomeRules rules)
        {
            float[] chances = rules.StarChances;
            float total = 0f;
            foreach (float weight in chances)
            {
                total += Mathf.Max(0f, weight);
            }
            if (total <= 0f)
            {
                return 0;
            }
            return PickIndex(chances, Random.Range(0f, total));
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

        private static int RollMutations(BiomeRules rules, int stars, int maxMutations)
        {
            List<Mutation> hits = new List<Mutation>();
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if (Dice.Percent(rules.ChanceOf(mutation, stars)))
                {
                    hits.Add(mutation);
                }
            }
            Cap(hits, maxMutations);
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
