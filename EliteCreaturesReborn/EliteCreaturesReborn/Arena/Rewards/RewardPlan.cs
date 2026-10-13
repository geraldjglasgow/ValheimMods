using EliteCraftingLink;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// What a trial pays (features/arena.md section 3, "Rewards"), worked out once on the core's owner as the trial ends,
    /// from the core's ZDO: the waves cleared (cleared or swept, <see cref="ArenaState.Swept"/>), the boss waves beaten
    /// (<see cref="ArenaState.BossesBeaten"/>) and the score. Every player of the trial gets the same list; the numbers are
    /// <see cref="RewardTable"/>'s. Items the game here does not have (an idol renamed, a rune EliteCrafting no longer
    /// makes) are left out; EliteCrafting's runes and essence come only when it is installed
    /// (<see cref="CraftingLink.Present"/>, through ValheimModLibs' EliteCraftingLink: its plugin and API found in the
    /// chainloader, nothing referenced).
    /// </summary>
    internal static class RewardPlan
    {
        public static RewardList For(ArenaState state)
        {
            int cleared = Mathf.Max(0, state.Swept);
            long beaten = state.BossesBeaten;
            RewardList list = new RewardList
            {
                Trial = state.TrialId,
                XpFactor = Mathf.Min(RewardTable.MaxXpFactor, cleared / RewardTable.WavesForFullXp),
            };
            Idols(list, cleared, beaten);
            Add(list, "Coins", (int)Mathf.Min(int.MaxValue, state.Score / RewardTable.ScorePerCoin), RewardList.Kind.Coins);
            if (CraftingLink.Present)
            {
                RewardRunes.Add(list, cleared, beaten);
            }
            return list;
        }

        /// <summary>True when the <paramref name="boss"/>-th boss wave (1 for wave 10) was beaten.</summary>
        public static bool Beaten(long beaten, int boss) =>
            (beaten & ArenaState.BossBit(boss * ArenaTable.WavesPerBiome)) != 0L;

        /// <summary>Adds an item the game has; one it has not is left out.</summary>
        public static void Add(RewardList list, string prefab, int count, RewardList.Kind kind)
        {
            if (count > 0 && ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(prefab) != null)
            {
                list.Add(prefab, count, kind);
            }
        }

        private static void Idols(RewardList list, int cleared, long beaten)
        {
            for (int boss = 1; boss <= cleared / ArenaTable.WavesPerBiome; boss++)
            {
                if (Beaten(beaten, boss))
                {
                    Add(list, RewardTable.IdolFor(boss), 1, RewardList.Kind.Item);
                }
            }
        }
    }
}
