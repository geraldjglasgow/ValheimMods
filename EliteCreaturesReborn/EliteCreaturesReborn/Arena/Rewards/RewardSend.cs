using System.Collections.Generic;
using EliteCreaturesReborn.Arena.Trial;
using EliteCreaturesReborn.Util;
using PatchGuard;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// Paying a trial's reward as it ends (features/arena.md section 3, "Rewards"), on the core's owner: the reward is
    /// planned once (<see cref="RewardPlan"/>), each player's own coins added, and sent to every player of the trial,
    /// dead or alive, one routed RPC each
    /// (<see cref="ArenaKeys.RewardRpc"/>); each player's own machine adds it (<see cref="RewardGrant"/>). A player who is
    /// offline at the end is sent nothing that reaches them, and gets nothing.
    /// </summary>
    internal static class RewardSend
    {
        public static void Pay(ZDO core, ArenaState state)
        {
            if (ZRoutedRpc.instance == null)
            {
                return;
            }
            RewardList shared = RewardPlan.For(state);
            List<long> players = TrialPlayers.All(core);
            for (int i = 0; i < players.Count; i++)
            {
                RewardList mine = RewardPlan.CoinsFor(shared, core, players[i]);
                if (!mine.Empty)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(players[i], ArenaKeys.RewardRpc, mine.Pack());
                }
            }
            Log.Info($"Hel's Trial rewards {players.Count} player(s): {shared.Entries.Count} kinds of item and their "
                + $"souls in coins, skill XP x{shared.XpFactor:0.##}");
        }

        /// <summary>The reward's handler, on every machine; it never throws into the RPC bus.</summary>
        public static void OnReward(long sender, ZPackage pkg) => Guard.Run("Hel's Trial reward", () => RewardGrant.Receive(pkg));
    }
}
