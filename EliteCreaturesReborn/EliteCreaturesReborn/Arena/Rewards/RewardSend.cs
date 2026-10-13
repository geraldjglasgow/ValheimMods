using System.Collections.Generic;
using EliteCreaturesReborn.Arena.Trial;
using EliteCreaturesReborn.Util;
using PatchGuard;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// Paying a trial's reward as it ends (features/arena.md section 3, "Rewards"), on the core's owner: the reward is
    /// planned once (<see cref="RewardPlan"/>) and sent to every player of the trial, dead or alive, one routed RPC each
    /// (<see cref="ArenaKeys.RewardRpc"/>); each player's own machine adds it (<see cref="RewardGrant"/>). A player who is
    /// offline at the end is sent nothing that reaches them, and gets nothing.
    /// </summary>
    internal static class RewardSend
    {
        public static void Pay(ZDO core, ArenaState state)
        {
            RewardList list = RewardPlan.For(state);
            if (list.Empty || ZRoutedRpc.instance == null)
            {
                return;
            }
            ZPackage pkg = list.Pack();
            List<long> players = TrialPlayers.All(core);
            for (int i = 0; i < players.Count; i++)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(players[i], ArenaKeys.RewardRpc, pkg);
            }
            Log.Info($"Hel's Trial rewards {players.Count} player(s): {list.Entries.Count} kinds of item, skill XP x"
                + $"{list.XpFactor:0.##}");
        }

        /// <summary>The reward's handler, on every machine; it never throws into the RPC bus.</summary>
        public static void OnReward(long sender, ZPackage pkg) => Guard.Run("Hel's Trial reward", () => RewardGrant.Receive(pkg));
    }
}
