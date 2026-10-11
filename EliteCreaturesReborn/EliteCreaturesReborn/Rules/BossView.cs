using System.Collections.Generic;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Presents the boss table as a <see cref="BiomeRules"/> so a boss travels the same scaling path a creature does -
    /// <c>StatMath</c>, <c>DamageMath</c> and the drop patch all read <c>Rules.Star</c> and need no boss branch of
    /// their own. Mutation chance is zero throughout, which is what keeps bosses free of mutations by construction
    /// rather than by a check every caller would have to remember. A boss that keeps its game level (boss stars off)
    /// gets neutral star power, <paramref name="unstarred"/>. A boss another mod fixed mutations for
    /// (<see cref="Registrations"/>) takes their numbers - `mutation power` and the large-star enhancement - from the rules
    /// of the biome it was rolled in, <paramref name="mutations"/>: the numbers a creature of that biome has.
    /// </summary>
    public static class BossView
    {
        public static BiomeRules For(BossRules boss, bool unstarred = false, BiomeRules? mutations = null)
        {
            BiomeRules view = new BiomeRules
            {
                StarChances = boss.StarChances,
                Star = unstarred ? new StarPower() : boss.Star,
                MutationChance = new[] { 0f },
                LargeStarPower = mutations?.LargeStarPower ?? 1f, // a plain boss has no mutations to enhance
            };
            if (mutations != null)
            {
                ShareNumbers(mutations, view);
            }
            return view;
        }

        // Shared, not cloned: the view only reads them, as every creature of that biome does.
        private static void ShareNumbers(BiomeRules from, BiomeRules to)
        {
            foreach (KeyValuePair<Mutation, Dictionary<string, float>> pair in from.MutationPower)
            {
                to.MutationPower[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<Mutation, Dictionary<string, string>> pair in from.MutationText)
            {
                to.MutationText[pair.Key] = pair.Value;
            }
        }
    }
}
