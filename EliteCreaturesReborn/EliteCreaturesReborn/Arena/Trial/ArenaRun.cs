using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// One step of the trial on the core's owner, four times a second (features/arena.md section 3), as
    /// <see cref="Raids.RaidPhases"/> steps a raid: first who is in the hall (they join the trial), then the ends that can
    /// come at any time, then the clock. The countdown and each break wait for their moment and then the next wave
    /// begins (<see cref="ArenaWaves"/>). In a wave, each archway sends its next creature at its own pace while fewer
    /// than 20 live (<see cref="ArenaSpawn"/>), the creatures left and the open portals are written for every client, and
    /// the wave ends once nothing of it is alive or still to come (held for a moment, for a boss's next phase or a
    /// Splintering creature's copies), or when its three minutes are up. Everything read and written is in the core's
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
            else if (now >= state.Next)
            {
                ArenaWaves.Begin(core, state.Wave + 1, inHall.Count, now); // the countdown or the break is over
            }
        }

        // Arena turned off (or the hall gone from this machine) stops the trial; an empty hall ends it.
        private static bool EndsNow(ArenaCore core, ArenaState state, long now)
        {
            bool off = !Config.ArenaSettings.Enabled || !ArenaHall.Built;
            if (!off && !Deserted(state, now))
            {
                return false;
            }
            ArenaEnd.End(core, stopped: off);
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
            if (state.Left > 0)
            {
                core.BusyAt = Time.time;
            }
            bool cleared = state.Left == 0 && Time.time - core.BusyAt >= ArenaTable.ClearedSeconds;
            if (cleared || now >= state.Next)
            {
                ArenaWaves.Rest(core, now, swept: !cleared);
            }
        }
    }
}
