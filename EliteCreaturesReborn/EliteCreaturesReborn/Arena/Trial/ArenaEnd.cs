using System.Globalization;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial ends (features/arena.md section 3, "End"): on the core's owner when no player of the trial is alive in
    /// the arena, when `elite arena stop` asks, or when Arena is turned off; on the server when nobody is at the arena any
    /// more to own the core (<see cref="ArenaOrphanWatch"/>: the last player jumped out or logged out). The core goes idle
    /// in its ZDO - which is what opens the doors and closes the portals on every client and tells every arena creature
    /// its trial is over - the creatures still waiting are dropped, those the owner holds vanish at once (the rest on their
    /// own owners within a second, or swept by the server), and every player of the trial reads at which wave it ended,
    /// with the kills and the souls they earned, and is paid the trial's reward (Arena/Rewards). The wave, kills and souls
    /// stay in the ZDO until the next trial begins.
    /// </summary>
    internal static class ArenaEnd
    {
        /// <summary>The owner ends its trial; <paramref name="won"/> names a biome's trial its players won.</summary>
        public static void End(ArenaCore core, bool stopped, string? won = null)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null || !core.IsOwner)
            {
                return;
            }
            long trial = core.State.TrialId;
            if (Close(zdo, stopped, won))
            {
                ArenaFighters.VanishOwned(trial);
            }
        }

        /// <summary>The owner: a biome's trial is over with its last wave, its boss's (<see cref="ArenaWaves.Rest"/>).
        /// With the boss beaten every player of the trial first reads that they won it; then it ends as any trial does,
        /// told and paid.</summary>
        public static void Finish(ArenaCore core)
        {
            ArenaState state = core.State;
            bool beaten = (state.BossesBeaten & ArenaState.BossBit(state.Wave)) != 0L;
            End(core, stopped: false, beaten ? ArenaModes.Token(ArenaModes.Of(state.Span)) : null);
        }

        /// <summary>
        /// The trial in the core's ZDO ends, written by whoever owns it now (the core's owner, or the server that has just
        /// taken a core nobody was at): idle, nothing waiting, no portal, the end told (as won, naming the trial, when
        /// <paramref name="won"/> is given) and the reward paid. False when no trial was running.
        /// </summary>
        public static bool Close(ZDO zdo, bool stopped, string? won = null)
        {
            ArenaState state = new ArenaState(zdo);
            if (!state.Running)
            {
                return false;
            }
            state.Phase = ArenaPhase.Idle;
            state.Left = 0;
            state.Next = 0L;
            state.RecallAt = 0L;
            state.Portals = 0;
            new ArenaQueue().Write(zdo);
            TellEach(zdo, state, won);
            Rewards.RewardSend.Pay(zdo, state); // to every player of the trial, dead or alive
            Log.Info($"Hel's Trial {(stopped ? "stopped" : "ended")} at wave {state.Wave}: {state.Kills} kills, "
                + $"{state.Souls} souls");
            return true;
        }

        // Each player of the trial reads the end with the souls they earned in it: "Hel's Trial ends at wave 12 ..." or
        // "You won Hel's trial of the Swamp ...".
        private static void TellEach(ZDO zdo, ArenaState state, string? won)
        {
            System.Collections.Generic.List<long> players = TrialPlayers.All(zdo);
            for (int i = 0; i < players.Count; i++)
            {
                string souls = Number(SoulWallets.Earned(zdo, players[i]));
                if (won != null)
                {
                    ArenaNet.TellOne(players[i], ArenaWords.Won, won, Number(state.Kills), souls);
                    continue;
                }
                ArenaNet.TellOne(players[i], ArenaWords.Ended, Number(state.Wave), Number(state.Kills), souls);
            }
        }

        /// <summary>A number as the end message shows it: "18,420".</summary>
        private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
