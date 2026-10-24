using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Ending a raid, however it ends, on the host's owner: the end goes into the host's ZDO (which is what every raider
    /// and every HUD line reads), the raiders are dealt with - on a stop every one dies dropping nothing, on a loss they
    /// walk off and vanish with their coins (<see cref="RaiderOrders"/>), after a win there are none - and the
    /// players near read how it ended. No coin is paid at the end: what the raiders carried was paid as they fell. A won
    /// raid's staked trophies go back into the Raiders Chest, times the band's coins back (<see cref="ChestTrophies"/>);
    /// any other end loses them.
    /// </summary>
    internal static class RaidFinish
    {
        public static void End(RaidRunner runner, RaidEnd how)
        {
            if (!runner.IsOwner || how == RaidEnd.None)
            {
                return;
            }
            RaidState state = runner.State;
            if (!state.Running)
            {
                return;
            }
            state.Ended = how;
            state.EndedAt = NetTime.NowMs();
            state.Phase = RaidPhase.Ended;
            Dismiss(runner, how);
            int back = TrophiesBack(runner, state, how == RaidEnd.Won);
            RaidNet.Tell(runner, back > 0 ? RaidText.WonTrophies(state.Face, back) : RaidText.Ended(state.Face, how));
            Log.Info($"raid at {runner.Position:F0} ({state.DisplayName}, {RaidTable.Band(state.Band).Name}) ended: {how}");
        }

        // The staked trophies back into the chest on a win, how many; gone on any other end.
        private static int TrophiesBack(RaidRunner runner, RaidState state, bool won)
        {
            string staked = state.Trophies;
            state.Trophies = "";
            Container? chest = runner.GetComponent<Container>();
            if (!won || staked.Length == 0 || chest == null)
            {
                return 0;
            }
            return ChestTrophies.Return(chest, staked, RaidTable.Band(state.Band).CoinsBack);
        }

        private static void Dismiss(RaidRunner runner, RaidEnd how)
        {
            if (how == RaidEnd.Stopped)
            {
                RaiderOrders.Stop(runner);
            }
            else if (how != RaidEnd.Won)
            {
                RaiderOrders.Disband(runner, how);
            }
        }
    }
}
