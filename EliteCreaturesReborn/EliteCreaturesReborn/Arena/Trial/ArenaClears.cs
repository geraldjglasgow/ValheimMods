using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Which waves are over, on the core's owner at every step of a wave (features/arena.md section 3, "A wave is a round"
    /// and "Calling the next wave"). Usually one wave fights; after a player calls the next one early
    /// (<see cref="ArenaCalls"/>) several do, each with its own creatures (their tags carry their wave). A wave is cleared
    /// once none of its creatures is alive or still to come, held for <see cref="ArenaTable.ClearedSeconds"/> (a boss's
    /// next phase, a Splintering creature's copies): a boss wave's idol is earned then, and the players read "Wave 6
    /// cleared.". The trial's <see cref="ArenaState.Swept"/> mark moves up over the cleared waves at the bottom; a wave
    /// cleared above one still fighting waits in <see cref="ArenaState.Done"/>. Once every wave fighting is cleared, the
    /// break begins (<see cref="ArenaWaves.Rest"/>), so no break comes between waves called early.
    /// </summary>
    internal static class ArenaClears
    {
        public static void Step(ArenaCore core, ArenaState state, ArenaQueue queue)
        {
            int before = state.Done;
            int done = before;
            for (int wave = state.Swept + 1; wave <= state.Wave; wave++)
            {
                int bit = state.DoneBit(wave);
                if (bit != 0 && (done & bit) == 0 && Quiet(core, state, queue, wave))
                {
                    done |= bit;
                    Earned(state, wave);
                }
            }
            if (done == before)
            {
                return;
            }
            if (done == state.DoneBit(state.Wave + 1) - 1)
            {
                ArenaWaves.Rest(core, NetTimeNow(), swept: false, Highest(state, done & ~before));
                return;
            }
            Tell(state, done & ~before);
            Advance(core, state, done);
        }

        // Nothing of the wave alive or still to come, for long enough.
        private static bool Quiet(ArenaCore core, ArenaState state, ArenaQueue queue, int wave)
        {
            Dictionary<int, float> busyAt = core.WaveBusyAt;
            float now = Time.time;
            if (queue.Of(wave) > 0 || ArenaFighters.Alive(state.TrialId, wave) > 0 || !busyAt.ContainsKey(wave))
            {
                busyAt[wave] = now;
                return false;
            }
            return now - busyAt[wave] >= ArenaTable.ClearedSeconds;
        }

        // A boss wave cleared: its boss fell, an idol at the end.
        private static void Earned(ArenaState state, int wave)
        {
            if (ArenaTable.IsBossWave(wave))
            {
                state.BossesBeaten |= ArenaState.BossBit(wave);
            }
        }

        // The highest wave among the bits of a Done mask.
        private static int Highest(ArenaState state, int bits)
        {
            int at = 0;
            for (int i = 0; i <= ArenaTable.MaxWavesAtOnce; i++)
            {
                at = (bits & 1 << i) != 0 ? i : at;
            }
            return state.Swept + 1 + at;
        }

        // Each wave just cleared while another still fights: "Wave 6 cleared."
        private static void Tell(ArenaState state, int bits)
        {
            for (int i = 0; i <= ArenaTable.MaxWavesAtOnce; i++)
            {
                if ((bits & 1 << i) != 0)
                {
                    ArenaNet.Tell(state.Zdo, ArenaWords.WaveDone, (state.Swept + 1 + i).ToString());
                }
            }
        }

        // The Swept mark up over the cleared waves at the bottom; never to the wave fighting, since one still fights.
        private static void Advance(ArenaCore core, ArenaState state, int done)
        {
            int swept = state.Swept;
            while ((done & 1) != 0 && swept + 1 < state.Wave)
            {
                done >>= 1;
                core.WaveBusyAt.Remove(++swept);
            }
            state.Swept = swept;
            state.Done = done;
        }

        private static long NetTimeNow() => Util.NetTime.NowMs();
    }
}
