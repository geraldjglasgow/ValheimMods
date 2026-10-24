using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Which trial Hel holds (features/arena.md section 3, "Trials"; the user, 2026-10-23): a biome's - Meadows, Black
    /// Forest, Swamp ... the Deep North - or Infinite. The world opens a biome's trial by killing its boss (the boss's own
    /// defeat key, a global key, read from the boss prefab), so the highest is the latest boss's biome; the Meadows is
    /// always open, so new players can fight too. A biome's trial is its five waves, the fifth its boss, and ends there;
    /// Infinite is the highest open biome round after round, a level harder each, up to <see cref="ArenaTable.Levels"/>.
    /// Chosen at Helgrind (E), carried into the hall by the player who walks through, and checked again by the core's
    /// owner as the trial begins. Any machine (the world's global keys reach every client).
    /// </summary>
    internal static class ArenaModes
    {
        /// <summary>The Infinite trial's number; 0 to 7 are the biomes' trials.</summary>
        public const int Infinite = ArenaTable.Biomes;

        /// <summary>No choice made: the highest open biome's trial.</summary>
        public const int Unset = -1;

        /// <summary>The word key of the Infinite trial's name.</summary>
        public const string InfiniteWord = "ecr_arena_mode_infinite";

        /// <summary>The highest biome whose trial the world has opened, 0 (Meadows) to 7: its latest boss's.</summary>
        public static int Highest()
        {
            ZoneSystem zones = ZoneSystem.instance;
            int highest = 0;
            for (int i = 1; zones != null && i < ArenaTable.Biomes; i++)
            {
                string key = ArenaBosses.DefeatKey(i);
                highest = key.Length > 0 && zones.GetGlobalKey(key) ? i : highest;
            }
            return highest;
        }

        /// <summary>The trial asked for, made one the world has open: an unset or unknown one is the highest biome's, a
        /// biome past it is the highest.</summary>
        public static int Resolve(int mode, int highest) =>
            mode == Infinite ? Infinite : mode < 0 || mode > Infinite ? highest : Mathf.Min(mode, highest);

        /// <summary>The trial after <paramref name="mode"/> at the gate: the next open biome, then Infinite, then the
        /// Meadows again.</summary>
        public static int Next(int mode, int highest)
        {
            int now = Resolve(mode, highest);
            return now == Infinite ? 0 : now >= highest ? Infinite : now + 1;
        }

        /// <summary>The biomes a trial fights.</summary>
        public static TrialSpan Span(int mode, int highest)
        {
            int chosen = Resolve(mode, highest);
            return chosen == Infinite ? new TrialSpan(highest, highest, endless: true) : new TrialSpan(chosen, chosen, false);
        }

        /// <summary>A trial's name as a word token, which every client reads in its own language: the game's biome name
        /// ("$biome_swamp") or "$ecr_arena_mode_infinite".</summary>
        public static string Token(int mode) => mode == Infinite ? Lang.Token(InfiniteWord)
            : "$biome_" + ArenaCreatures.Biome(Mathf.Clamp(mode, 0, ArenaTable.Biomes - 1)).ToString().ToLowerInvariant();

        /// <summary>A trial's name in this player's language.</summary>
        public static string Name(int mode) =>
            mode == Infinite ? Lang.Get(InfiniteWord) : ArenaWords.BiomeName(ArenaCreatures.Biome(Mathf.Max(0, mode)));

        /// <summary>The trial a running one is, from its span.</summary>
        public static int Of(TrialSpan span) => span.Endless ? Infinite : span.First;
    }
}
