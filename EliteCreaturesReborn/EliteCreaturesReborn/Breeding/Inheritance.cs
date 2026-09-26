using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Breeding
{
    /// <summary>
    /// The inheritance roll, and nothing else - no ZDO, no game objects. Stars are drawn with equal odds from 0 up to
    /// the stronger parent's, so a line only holds its strength if the player keeps the good ones. The mutation is one
    /// of the parents' own, picked at random from both, whenever either has one (at the rule file's chance, 100 by
    /// default); a mutation switched off in `mutations enabled` is never passed on. A newborn never rolls anything new.
    /// </summary>
    internal static class Inheritance
    {
        public static CreatureTraits Offspring(CreatureTraits mother, CreatureTraits? sire, RuleSet rules)
        {
            int cap = Mathf.Max(mother.Stars, sire?.Stars ?? 0);
            int stars = Random.Range(0, cap + 1);
            int mask = PickMutation(mother.Mask | (sire?.Mask ?? 0), rules);
            return new CreatureTraits(stars, mask);
        }

        private static int PickMutation(int pool, RuleSet rules)
        {
            List<Mutation> candidates = new List<Mutation>();
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if ((pool & (1 << (int)mutation)) != 0 && rules.IsEnabled(mutation))
                {
                    candidates.Add(mutation);
                }
            }
            if (candidates.Count == 0 || !Dice.Percent(rules.Breeding.MutationChance))
            {
                return 0;
            }
            return 1 << (int)candidates[Random.Range(0, candidates.Count)];
        }
    }
}
