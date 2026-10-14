using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// One step of the trial on the core's owner, four times a second (features/arena.md section 3), as
    /// <see cref="Raids.RaidPhases"/> steps a raid: first who is in the hall (they join the trial), then the ends that can
    /// come at any time, then the clock. The countdown and each break wait for their moment, or until every player alive
    /// in the arena has pressed F (<see cref="ArenaReady"/>), and then the next wave begins (<see cref="ArenaWaves"/>). In a wave, each archway sends its next creature at its own pace while fewer
    /// than 24 live (<see cref="ArenaSpawn"/>), the creatures left and the open portals are written for every client, and
    /// each wave fighting ends once nothing of it is alive or still to come (<see cref="ArenaClears"/>), or all of them
    /// a minute after Hel marked their last few (<see cref="ArenaRecall"/>) or when the five minutes of the latest are up. Everything read and written is in the core's
    /// ZDO, so a machine that has just taken the core over picks up where the last one stopped.
    /// </summary>
    internal static class ArenaRun
    {
        public static void Step(ArenaCore core, long now)
        {
            ArenaState state = core.State;
            if (!state.Running)
            {
                return;
            }
            List<long> inHall = HallPlayers.PeersNow();
            Presence(core, state, inHall, now);
            if (EndsNow(core, state, now))
            {
                return;
            }
            if (state.Phase == ArenaPhase.Fighting)
            {
                Fight(core, state, now);
            }
            else if (now >= state.Next || ArenaReady.GoIfAll(core, inHall))
            {
                ArenaWaves.Begin(core, state.Wave + 1, inHall.Count, now); // the countdown or the break is over
            }
        }

        // Arena turned off stops the trial and an empty arena ends it. With the hall not built here (this machine's player
        // has just left it) nothing is done: the core passes to a player who is there, or the server ends the trial.
        private static bool EndsNow(ArenaCore core, ArenaState state, long now)
        {
            if (!Config.ArenaSettings.Enabled)
            {
                ArenaEnd.End(core, stopped: true);
                return true;
            }
            if (!ArenaHall.Built)
            {
                return true;
            }
            if (!Deserted(state, now))
            {
                return false;
            }
            ArenaEnd.End(core, stopped: false);
            return true;
        }

        // The players in the hall join the trial; while any is there, the time is stamped every second or so.
        private static void Presence(ArenaCore core, ArenaState state, List<long> inHall, long now)
        {
            if (inHall.Count == 0)
            {
                return;
            }
            TrialPlayers.Join(core.Zdo!, inHall);
            SoulWallets.Join(core.Zdo!, inHall, state.Souls);
            if (now - state.SeenAt >= ArenaState.Ms(ArenaTable.SeenStampSeconds))
            {
                state.SeenAt = now;
            }
        }

        // No living player in the hall for a few seconds, or a dedicated server running the trial with nobody connected
        // (its clock stands still then, so the seconds would never pass).
        private static bool Deserted(ArenaState state, long now)
        {
            ZNet net = ZNet.instance;
            bool empty = net != null && net.IsDedicated() && net.GetNrOfPlayers() == 0;
            return empty || now - state.SeenAt > ArenaState.Ms(ArenaTable.EmptySeconds);
        }

        private static void Fight(ArenaCore core, ArenaState state, long now)
        {
            int alive = ArenaFighters.Alive(state.TrialId);
            ArenaQueue queue = ArenaQueue.Read(core.Zdo!);
            if (queue.Count > 0)
            {
                alive += ArenaSpawn.Send(core, queue, alive);
            }
            state.Left = alive + queue.Count;
            state.Portals = queue.LaneMask();
            ArenaClears.Step(core, state, queue); // each wave fighting cleared by its own creatures; the break after the last
            if (state.Phase == ArenaPhase.Fighting)
            {
                ArenaRecall.Step(core, state, queue, alive, now); // the last few marked, then recalled
            }
            if (state.Phase == ArenaPhase.Fighting && now >= state.Next)
            {
                ArenaWaves.Rest(core, now, swept: true, state.Wave);
            }
        }
    }
}
