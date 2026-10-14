using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Hel's recall of a wave's stragglers (features/arena.md section 3, "Stragglers"; a player's report 2026-10-14: a
    /// first-wave Ghost, slow and hard to see in the dark, took a long walk to find), on the core's owner at every step of
    /// a wave. Once every creature of the waves fighting has come out and no more than a quarter of them are left
    /// (<see cref="ArenaTable.Stragglers"/>), the recall starts: its moment goes into the core's ZDO, so every client marks
    /// the last ones (<see cref="StragglerMark"/>) and counts it down on the HUD line, and the players read that Hel
    /// will recall them. A minute later the waves fighting end as when their time runs out: the stragglers vanish, scoring
    /// nothing, and the break begins. Never while a boss lives or is still to come; a wave called early calls the recall
    /// off. Creatures that join after it started (a Splintering creature's copies) do not.
    /// </summary>
    internal static class ArenaRecall
    {
        /// <summary>One step of the waves fighting, after their clears: <paramref name="alive"/> of the trial's creatures
        /// live.</summary>
        public static void Step(ArenaCore core, ArenaState state, ArenaQueue queue, int alive, long now)
        {
            if (queue.Count > 0 || ArenaFighters.Bosses(state.TrialId) > 0)
            {
                state.RecallAt = 0L;
                return;
            }
            long at = state.RecallAt;
            if (at == 0L && alive > 0 && alive <= ArenaTable.Stragglers(state.WaveSize))
            {
                Begin(state, alive, now);
            }
            else if (at > 0L && now >= at)
            {
                ArenaWaves.Rest(core, now, swept: true, state.Wave);
            }
        }

        private static void Begin(ArenaState state, int alive, long now)
        {
            state.RecallAt = now + ArenaState.Ms(ArenaTable.RecallSeconds);
            ArenaNet.Tell(state.Zdo, ArenaWords.Recall, Mathf.RoundToInt(ArenaTable.RecallSeconds).ToString());
            Log.Info($"Hel's Trial: wave {state.Wave} down to {alive} of {state.WaveSize}; Hel recalls them in "
                + $"{ArenaTable.RecallSeconds:0} s");
        }
    }
}
