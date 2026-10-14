using System.Collections.Generic;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Hel wakes (features/arena.md section 3, "Start"), on the core's owner, asked by a player at her throne through
    /// <see cref="ArenaTrial.RequestStart"/>: when Arena is on, the hall is built here, no trial runs and a living player
    /// is in the hall, the trial begins with its ten-second countdown, every player in the hall now is in it, and they
    /// read that it has begun. Anything else is ignored: the asker's own checks already stopped a mistaken ask.
    /// </summary>
    internal static class ArenaStart
    {
        public static void Begin(ArenaCore core, long asker)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null || !core.IsOwner || !Config.ArenaSettings.Enabled || !ArenaHall.Built)
            {
                return;
            }
            ArenaState state = core.State;
            List<long> inHall = HallPlayers.PeersNow();
            if (state.Running || inHall.Count == 0)
            {
                return;
            }
            state.Begin(NetTime.NowMs(), ArenaPower.StartBiome());
            new ArenaQueue().Write(zdo);
            TrialPlayers.Reset(zdo, inHall);
            SoulWallets.Reset(zdo, inHall);
            ArenaReady.Clear(zdo);
            core.WaveBusyAt.Clear();
            ArenaNet.Tell(zdo, ArenaWords.Started);
            Log.Info($"Hel's Trial begins for {inHall.Count} player(s), asked by peer {asker}");
        }
    }
}
