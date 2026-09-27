using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Night as the game sees it: <c>EnvMan.IsNight()</c>, the day fraction outside 0.25 to 0.75 (daybreak and nightfall
    /// at 15 % and 85 % of the clock's day). It depends on the clock alone, so every machine agrees and an owner change
    /// never flips a torch; <c>IsDaylight</c> would add dark weather, which differs per biome where each owner stands. The
    /// game's fraction trails the clock (1 % per update, from midnight after loading), so while it is more than a few
    /// degrees off (the first seconds in a world, a sleep's time skip) the answer is unknown and nothing switches.
    /// </summary>
    public static class TorchNight
    {
        /// <summary>
        /// How far, in degrees of the day, the game's fraction may trail the clock. Settled it trails under 1 at 50 updates a
        /// second and about 3 at 10 frames a second (EnvMan updates at most once per frame); after loading it starts 180 off at noon.
        /// </summary>
        private const float SettledDegrees = 10f;

        /// <summary>True at night, false by day, null while the game's day fraction is still catching up.</summary>
        public static bool? Now()
        {
            EnvMan env = EnvMan.instance;
            if (env == null || ZNet.instance == null || !Settled(env))
                return null;
            return EnvMan.IsNight();
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
