using System;
using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>When a plant is at its best, from the Forage file's <c>best</c>: every listed condition must hold.</summary>
    [Flags]
    public enum BestTime
    {
        None = 0,
        Day = 1,
        Night = 2,
        Wet = 4,
        Dry = 8,
    }

    /// <summary>
    /// Reading, testing and describing <see cref="BestTime"/>. The test uses the game's own environment state on the
    /// client that asks (EnvMan's day, night and wet flags, the weather where that player stands), so the picker's
    /// client decides whether a pick was at the plant's best, as it decides everything else about the pick.
    /// </summary>
    public static class BestTimes
    {
        private static readonly Dictionary<string, BestTime> Words = new Dictionary<string, BestTime>(StringComparer.OrdinalIgnoreCase)
        {
            { "day", BestTime.Day },
            { "night", BestTime.Night },
            { "wet", BestTime.Wet },
            { "rain", BestTime.Wet },
            { "dry", BestTime.Dry },
        };

        public const string Allowed = "day, night, wet or dry";

        public static bool TryParse(string word, out BestTime time) => Words.TryGetValue((word ?? "").Trim(), out time);

        /// <summary>Whether every condition holds right now; never for <see cref="BestTime.None"/> or without a world.</summary>
        public static bool IsNow(BestTime best)
        {
            if (best == BestTime.None || EnvMan.instance == null)
                return false;
            return Holds(best, BestTime.Day, EnvMan.IsDay()) && Holds(best, BestTime.Night, EnvMan.IsNight())
                && Holds(best, BestTime.Wet, EnvMan.IsWet()) && Holds(best, BestTime.Dry, !EnvMan.IsWet());
        }

        /// <summary>Words for a hover line, such as "at night" or "by day in dry weather".</summary>
        public static string Describe(BestTime best)
        {
            List<string> words = new List<string>();
            if ((best & BestTime.Day) != 0)
                words.Add("by day");
            if ((best & BestTime.Night) != 0)
                words.Add("at night");
            if ((best & BestTime.Wet) != 0)
                words.Add("in the rain");
            if ((best & BestTime.Dry) != 0)
                words.Add("in dry weather");
            return string.Join(" ", words);
        }

        private static bool Holds(BestTime best, BestTime condition, bool now) => (best & condition) == 0 || now;
    }
}
