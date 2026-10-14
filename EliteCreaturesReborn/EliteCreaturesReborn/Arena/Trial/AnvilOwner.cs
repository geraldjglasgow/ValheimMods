using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Hel's Anvil on the core's owner (features/arena.md section 3, "Hel's Anvil"), which keeps the souls: a player asks
    /// to repair (<see cref="ArenaKeys.RepairRpc"/>); during the countdown or a break, from a player of the trial with the
    /// souls for it, the cost - 10 x the biome tier of the wave coming x 1.5 per repair they have made in the trial - is
    /// taken from their wallet, every player of the trial sees cold soul-fire flare at the anvil, and the asker is told
    /// yes with the cost (<see cref="ArenaKeys.RepairedRpc"/>); otherwise told no. The asker's own machine repairs.
    /// </summary>
    internal static class AnvilOwner
    {
        /// <summary>The size of the soul-fire flare at the anvil.</summary>
        private const float FlareSize = 0.35f;

        public static void Repair(ArenaCore core, long asker, Vector3 anvil)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null)
            {
                return;
            }
            int cost = Cost(zdo, asker, out bool open);
            bool paid = open && cost > 0 && SoulWallets.Souls(zdo, asker) >= cost;
            if (paid)
            {
                SoulWallets.Spend(zdo, asker, cost);
                ArenaNet.Flare(zdo, anvil, FlareSize);
            }
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(asker, ArenaKeys.RepairedRpc, paid, cost);
            }
        }

        /// <summary>What the anvil asks of the player now; <paramref name="open"/> is false outside the countdown and the
        /// breaks, or for no player of the trial. Any machine holding the core.</summary>
        public static int Cost(ZDO core, long peer, out bool open)
        {
            ArenaState state = new ArenaState(core);
            ArenaPhase phase = state.Phase;
            bool resting = phase == ArenaPhase.Countdown || phase == ArenaPhase.Break;
            bool member = SoulWallets.TryGet(core, peer, out SoulWallets.Wallet wallet);
            open = resting && member;
            return ArenaTable.AnvilCost(state.TierOf(state.JoiningWave), member ? wallet.Repairs : 0);
        }
    }
}
