using EliteCraftingLink;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// What a trial pays (features/arena.md section 3, "Rewards"), worked out once on the core's owner as the trial ends,
    /// from the core's ZDO: the waves cleared (cleared or swept, <see cref="ArenaState.Swept"/>), the boss waves beaten
    /// (<see cref="ArenaState.BossesBeaten"/>). Every player of the trial gets the same list, and their own coins on top
    /// (<see cref="CoinsFor"/>: their unspent souls over 2); the numbers are <see cref="RewardTable"/>'s. Items the game here does not have (an idol renamed, a rune EliteCrafting no longer
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
            Idols(list, state, cleared, beaten);
            if (CraftingLink.Present)
            {
                RewardRunes.Add(list, state.Span, cleared, beaten);
            }
            return list;
        }

        /// <summary>One player's reward: the trial's list, with their unspent souls over 2 as coins.</summary>
        public static RewardList CoinsFor(RewardList shared, ZDO core, long peer)
        {
            RewardList mine = shared.Copy();
            long coins = SoulWallets.Souls(core, peer) / RewardTable.SoulsPerCoin;
            Add(mine, "Coins", (int)Mathf.Min(int.MaxValue, coins), RewardList.Kind.Coins);
            return mine;
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

        private static void Idols(RewardList list, ArenaState state, int cleared, long beaten)
        {
            for (int boss = 1; boss <= cleared / ArenaTable.WavesPerBiome; boss++)
            {
                if (Beaten(beaten, boss))
                {
                    int tier = state.TierOf(boss * ArenaTable.WavesPerBiome);
                    Add(list, RewardTable.IdolFor(boss, tier), 1, RewardList.Kind.Item);
                }
            }
        }
    }
}
