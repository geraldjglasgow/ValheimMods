using System.Collections.Generic;
using EliteCraftingLink;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// EliteCrafting's part of the reward (features/arena.md section 3, "Rewards"), only where it is installed: for each
    /// wave cleared, runes drawn by EliteCrafting's default weights at the wave's tier (the biome, as EliteCrafting ties its
    /// tiers to biomes: Meadows 1 to the Deep North 8) and a trophy's worth of essence; for each boss wave beaten, that
    /// boss's runes and its trophy's essence instead (a swept boss wave pays nothing of it). So the further the trial got,
    /// the more runes and essence, and the later the runes: Ascension from the Black Forest on, the Sealed Rune from the
    /// Swamp. Amounts in <see cref="RewardTable"/>. Drawn once on the core's owner; every player gets the same.
    /// </summary>
    internal static class RewardRunes
    {
        private static readonly Dictionary<string, int> Drawn = new Dictionary<string, int>();

        public static void Add(RewardList list, int start, int cleared, long beaten)
        {
            Drawn.Clear();
            int essence = 0;
            for (int wave = 1; wave <= cleared; wave++)
            {
                essence += Wave(start, wave, beaten);
            }
            foreach (KeyValuePair<string, int> rune in Drawn)
            {
                RewardPlan.Add(list, rune.Key, rune.Value, RewardList.Kind.Rune);
            }
            RewardPlan.Add(list, CraftingPrefabs.Essence, essence, RewardList.Kind.Essence);
            Drawn.Clear();
        }

        // One wave's runes (drawn into Drawn) and its essence (returned).
        private static int Wave(int start, int wave, long beaten)
        {
            int index = RewardTable.Index(ArenaTable.BiomeNumber(start, wave));
            if (!ArenaTable.IsBossWave(wave))
            {
                Draw(index, RewardTable.RunesPerWave[index]);
                return RewardTable.EssencePerWave[index];
            }
            int boss = wave / ArenaTable.WavesPerBiome;
            if (!RewardPlan.Beaten(beaten, boss))
            {
                return 0;
            }
            Draw(index, RewardTable.BossRunes[index]); // the boss of the wave's biome
            return RewardTable.BossEssence[index];
        }

        private static void Draw(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string rune = Pick(index);
                Drawn[rune] = (Drawn.TryGetValue(rune, out int had) ? had : 0) + 1;
            }
        }

        // One rune by the tier's weights.
        private static string Pick(int index)
        {
            int total = 0;
            foreach (int[] weights in RewardTable.RuneWeights)
            {
                total += weights[index];
            }
            int roll = Random.Range(0, Mathf.Max(1, total));
            for (int r = 0; r < RewardTable.Runes.Length; r++)
            {
                if ((roll -= RewardTable.RuneWeights[r][index]) < 0)
                {
                    return RewardTable.Runes[r];
                }
            }
            return RewardTable.Runes[0];
        }
    }
}
