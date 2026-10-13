using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// What a biome gives its creatures at one world tier, read the way the rollers roll: on a difficulty from the
    /// preset's cell (<see cref="PresetCell"/>), on Custom from the biome's own `star chances` and `mutation chance` rows
    /// with the tier's boosts (<c>TraitRoller</c>). The most stars, the share of creatures with any, and the share of
    /// creatures with no stars that carry a mutation.
    /// </summary>
    internal readonly struct BiomeOdds
    {
        public readonly int Cap;
        public readonly float Starred;
        public readonly float Mutated;

        private BiomeOdds(int cap, float starred, float mutated)
        {
            Cap = cap;
            Starred = starred;
            Mutated = mutated;
        }

        public static BiomeOdds For(RuleSet set, int tier, Heightmap.Biome biome)
        {
            if (set.Difficulty != Difficulty.Custom)
            {
                PresetCell cell = PresetCell.For(set.Difficulty, tier, PresetTables.Rank(biome));
                return new BiomeOdds(cell.Cap, cell.StarredPercent, cell.Mutation);
            }
            BiomeRules rules = set.For(biome);
            float starred = StarredShare(rules.StarChances, set.Tiers.StarBoostAt(tier));
            return new BiomeOdds(rules.StarCeiling, starred, MutatedShare(set, rules, set.Tiers.MutationBoostAt(tier)));
        }

        /// <summary>The most stars any biome can reach at any tier on this server's difficulty.</summary>
        public static int MostStars(RuleSet set)
        {
            int most = 0;
            foreach (Heightmap.Biome biome in PageText.Biomes)
            {
                for (int tier = 0; tier <= PresetTables.TopTier; tier++)
                {
                    most = Mathf.Max(most, set.Difficulty == Difficulty.Custom
                        ? set.For(biome).StarCeiling
                        : PresetCell.For(set.Difficulty, tier, PresetTables.Rank(biome)).Cap);
                }
            }
            return most;
        }

        // Custom: each star count's weight times boost^stars, as TraitRoller draws it.
        private static float StarredShare(float[] chances, float boost)
        {
            float total = 0f;
            float factor = 1f;
            for (int stars = 0; stars < chances.Length; stars++)
            {
                total += Mathf.Max(0f, chances[stars]) * factor;
                factor *= Mathf.Max(0f, boost);
            }
            return total <= 0f ? 0f : 100f - Mathf.Max(0f, chances[0]) * 100f / total;
        }

        // Custom: every enabled mutation rolls on its own, so a plain creature carries one unless all of them miss.
        private static float MutatedShare(RuleSet set, BiomeRules rules, float boost)
        {
            float none = 1f;
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if (set.IsEnabled(mutation))
                {
                    none *= 1f - Mathf.Clamp(rules.ChanceOf(mutation, 0) * boost, 0f, 100f) / 100f;
                }
            }
            return (1f - none) * 100f;
        }
    }
}
