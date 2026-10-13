using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// One step of the trial on the core's owner, twice a second (features/arena.md section 3), as
    /// <see cref="Raids.RaidPhases"/> steps a raid: first who is in the hall (they join the trial), then the ends that can
    /// come at any time, then the clock. In the countdown the first wave waits for its moment; in a wave the creatures
    /// still to come are sent while fewer than 20 live (<see cref="ArenaSpawn"/>), the creatures left are counted for the
    /// HUD line, and the next wave comes when two or fewer are left or the wave's timer runs out - or, in a boss wave,
    /// once its boss and everything its death brings in its place is gone. Everything read and written is in the core's
    /// ZDO, so a machine that has just taken the core over picks up where the last one stopped.
    /// </summary>
    internal static class ArenaRun
    {
        /// <summary>A wave lasts at least this long, so one that brought nobody (a biome whose creatures this game does
        /// not know) never sends the next ones in a blink.</summary>
        private const float ShortestWaveSeconds = 5f;

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
                Fight(core, state, inHall.Count, now);
            }
            else if (now >= state.Next)
            {
                ArenaWaves.Begin(core, 1, inHall.Count, now); // the countdown is over
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

        private static void Fight(ArenaCore core, ArenaState state, int players, long now)
        {
            ZDO zdo = core.Zdo!;
            long trial = state.TrialId;
            int alive = ArenaFighters.Alive(trial);
            ArenaQueue queue = ArenaQueue.Read(zdo);
            if (queue.Count > 0 && alive < ArenaTable.MaxAlive)
            {
                alive += ArenaSpawn.Send(core, queue, ArenaTable.MaxAlive - alive);
            }
            state.Left = alive + queue.Count;
            if (WaveDone(core, state, queue, now))
            {
                ArenaWaves.Begin(core, state.Wave + 1, players, now);
            }
        }

        private static bool WaveDone(ArenaCore core, ArenaState state, ArenaQueue queue, long now)
        {
            if (!ArenaTable.IsBossWave(state.Wave))
            {
                long began = state.Next - ArenaState.Ms(ArenaTable.WaveSeconds(state.Wave));
                bool cleared = state.Left <= ArenaTable.NextWaveAtLeft;
                return now - began >= ArenaState.Ms(ShortestWaveSeconds) && (cleared || now >= state.Next);
            }
            if (queue.BossDue || ArenaFighters.Bosses(state.TrialId) > 0)
            {
                core.BossSeenAt = Time.time;
                return false;
            }
            return Time.time - core.BossSeenAt >= ArenaTable.BossGoneSeconds;
        }
    }
}
