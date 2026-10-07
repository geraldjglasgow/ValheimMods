using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The day's fortune: one of five, the same for everyone in the world, from the world's seed and the day number alone,
    /// so every machine works it out without a message. It adds to the effective level of every star roll that day
    /// (<see cref="StarOdds"/>): Ill-omened -10, Poor -5, Fair 0, Good +5, Blessed +10. Odds of each: 10%, 20%, 40%, 20%,
    /// 10%. Fair (no change) while the "Daily Fortune" switch is off or before a world is loaded.
    /// </summary>
    public static class Fortune
    {
        public enum Kind
        {
            IllOmened,
            Poor,
            Fair,
            Good,
            Blessed,
        }

        private static readonly float[] levels = { -10f, -5f, 0f, 5f, 10f };
        private static readonly string[] names = { "Ill-omened", "Poor", "Fair", "Good", "Blessed" };

        private static int cachedDay = -1;
        private static int cachedSeed;
        private static Kind cachedKind = Kind.Fair;

        public static bool On => Settings.DailyFortune != null && Settings.DailyFortune.Value;

        /// <summary>Levels today's fortune adds to every star roll: -10 to +10, 0 while off.</summary>
        public static float Levels() => On ? levels[(int)Today()] : 0f;

        public static Kind Today()
        {
            if (EnvMan.instance == null || WorldGenerator.instance == null)
                return Kind.Fair;
            return Of(EnvMan.instance.GetDay(), WorldGenerator.instance.GetSeed());
        }

        /// <summary>The fortune of a day in a world; worked out once per day.</summary>
        public static Kind Of(int day, int seed)
        {
            if (day == cachedDay && seed == cachedSeed)
                return cachedKind;
            cachedDay = day;
            cachedSeed = seed;
            int roll = ($"hearthhold_fortune:{seed}:{day}".GetStableHashCode() & 0x7FFFFFFF) % 100;
            cachedKind = roll < 10 ? Kind.IllOmened : roll < 30 ? Kind.Poor : roll < 70 ? Kind.Fair : roll < 90 ? Kind.Good : Kind.Blessed;
            return cachedKind;
        }

        public static string Name(Kind kind) => names[(int)kind];

        public static float LevelsOf(Kind kind) => levels[(int)kind];

        /// <summary>The fortune as one line, e.g. "Good fortune today: star rolls +5".</summary>
        public static string Line(Kind kind)
        {
            float value = LevelsOf(kind);
            string effect = value == 0f ? "star rolls as usual" : $"star rolls {(value > 0f ? "+" : "")}{value:0}";
            return $"{Name(kind)} fortune today: {effect}";
        }
    }
}
