using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The torches' night: the game's own night widened by Torch Margin on both ends. The game's night
    /// (<c>EnvMan.IsNight()</c>) is its smoothed day fraction at or below 0.25 or at or above 0.75; the smoothed fraction
    /// follows <c>RescaleDayFraction</c> of the clock, which maps daybreak at 15 % and nightfall at 85 % of the clock's day
    /// onto 0.25 and 0.75. The margin moves those two clock points by hours / 24 (an in-game hour is a 24th of the day)
    /// and the same rescale gives the thresholds, so a margin of 0 is exactly <c>IsNight</c>. It depends on the clock
    /// alone, so every machine agrees and an owner change never flips a torch; <c>IsDaylight</c> would add dark weather,
    /// which differs per biome where each owner stands. The game's fraction trails the clock (1 % per update, from
    /// midnight after loading), so while it is more than a few degrees off (the first seconds in a world, a sleep's time
    /// skip) the answer is unknown and nothing switches.
    /// </summary>
    public static class TorchNight
    {
        /// <summary>
        /// How far, in degrees of the day, the game's fraction may trail the clock. Settled it trails under 1 at 50 updates a
        /// second and about 3 at 10 frames a second (EnvMan updates at most once per frame); after loading it starts 180 off at noon.
        /// </summary>
        private const float SettledDegrees = 10f;

        /// <summary>The game's daybreak and nightfall as fractions of the clock's day (<c>RescaleDayFraction</c>, <c>SkipToMorning</c>).</summary>
        private const float Daybreak = 0.15f;
        private const float Nightfall = 0.85f;
        private const float HoursPerDay = 24f;

        /// <summary>True from Torch Margin before nightfall to Torch Margin after daybreak, false in between, null while the
        /// game's day fraction is still catching up.</summary>
        public static bool? Now()
        {
            EnvMan env = EnvMan.instance;
            if (env == null || ZNet.instance == null || !Settled(env))
                return null;
            return Lit(env, env.GetDayFraction());
        }

        private static bool Lit(EnvMan env, float fraction)
        {
            float margin = Mathf.Clamp(TorchSettings.TorchMargin.Value, 0f, TorchSettings.MaxMargin) / HoursPerDay;
            float lightsOut = env.RescaleDayFraction(Daybreak + margin);
            float lightsOn = env.RescaleDayFraction(Nightfall - margin);
            return fraction <= lightsOut || fraction >= lightsOn;
        }

        private static bool Settled(EnvMan env)
        {
            if (env.m_debugTimeOfDay)
                return true;
            if (env.m_dayLengthSec <= 0)
                return false;
            double day = env.m_dayLengthSec;
            float clock = (float)(ZNet.instance.GetTimeSeconds() % day / day);
            float target = env.RescaleDayFraction(clock);
            return Mathf.Abs(Mathf.DeltaAngle(env.GetDayFraction() * 360f, target * 360f)) < SettledDegrees;
        }
    }
}
