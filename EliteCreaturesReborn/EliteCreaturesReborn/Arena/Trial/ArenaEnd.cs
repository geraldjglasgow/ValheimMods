using System.Globalization;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial ends (features/arena.md section 3, "End"), on the core's owner: when no player of the trial is alive in
    /// the hall, when `elite arena stop` asks, or when Arena is turned off. The core goes idle in its ZDO - which is what
    /// opens the doors on every client and tells every arena creature its trial is over - the creatures still waiting are
    /// dropped, those this machine owns vanish at once (the rest on their own owners within a second), and every player
    /// of the trial reads at which wave it ended, with the kills and the score. The wave, kills and score stay in the ZDO
    /// until the next trial begins.
    /// </summary>
    internal static class ArenaEnd
    {
        public static void End(ArenaCore core, bool stopped)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null || !core.IsOwner)
            {
                return;
            }
            ArenaState state = core.State;
            if (!state.Running)
            {
                return;
            }
            long trial = state.TrialId;
            state.Phase = ArenaPhase.Idle;
            state.Left = 0;
            state.Next = 0L;
            new ArenaQueue().Write(zdo);
            ArenaFighters.VanishOwned(trial);
            ArenaNet.Tell(zdo, ArenaWords.Ended, Number(state.Wave), Number(state.Kills), Number(state.Score));
            Log.Info($"Hel's Trial {(stopped ? "stopped" : "ended")} at wave {state.Wave}: {state.Kills} kills, "
                + $"score {state.Score}");
        }

        /// <summary>A number as the end message shows it: "18,420".</summary>
        private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
