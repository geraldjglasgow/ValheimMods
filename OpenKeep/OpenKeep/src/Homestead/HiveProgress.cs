using System;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// A hive's progress toward its next honey is the game's <c>product</c> ZDO float: seconds of game time counted at
    /// the rate of the moment. This class keeps it honest when this mod sets the rate: a rate change keeps the fraction
    /// of a honey already made, and the seconds past a finished honey are kept instead of dropped. All of it runs on
    /// the hive's ZDO owner and lives in the ZDO, so it survives an owner change and a restart.
    /// </summary>
    public static class HiveProgress
    {
        /// <summary>ZDO float: the seconds per honey the stored progress was counted at. Written once this mod's rate has run.</summary>
        public const string RateKey = "OpenKeep.hiveSecPerUnit";

        private static readonly int RateHash = RateKey.GetStableHashCode();

        /// <summary>What the game's tick starts from: the rate, the stored progress and the time stamp.</summary>
        public sealed class Tick
        {
            public float Seconds;
            public float Product;
            public long LastTime;
        }

        /// <summary>
        /// Converts the stored progress to a new rate so the fraction of a honey is kept (half a honey at 2 per day is
        /// half a honey at 10 per day). A hive this mod never ran counted at the game's rate; with the settings off
        /// again, one conversion back to the game's rate follows and the hive is left alone after that.
        /// </summary>
        public static void Rescale(ZDO zdo, float seconds, float vanilla, bool active)
        {
            float counted = zdo.GetFloat(RateHash, 0f);
            if (counted <= 0f)
            {
                if (!active)
                    return;
                counted = vanilla;
            }
            if (Mathf.Approximately(counted, seconds))
                return;
            float product = zdo.GetFloat(ZDOVars.s_product);
            if (product > 0f)
                zdo.Set(ZDOVars.s_product, product * seconds / counted);
            zdo.Set(RateHash, seconds);
        }

        public static Tick Before(ZDO zdo, float seconds)
        {
            return new Tick
            {
                Seconds = seconds,
                Product = zdo.GetFloat(ZDOVars.s_product),
                LastTime = zdo.GetLong(ZDOVars.s_lastTime, 0L),
            };
        }

        /// <summary>
        /// The game adds (int)(num / m_secPerUnit) honey, num being the stored progress plus the seconds since the time
        /// stamp, then sets the progress to 0 and drops the rest of num. The hive ticks every 10 s, so at 60 a day
        /// (30 s per honey) that would give 45. Recomputes num as the game did and stores the rest instead.
        /// </summary>
        public static void KeepRemainder(ZDO zdo, Tick before)
        {
            if (zdo.GetFloat(ZDOVars.s_product) != 0f)
                return;
            float num = before.Product + Elapsed(before.LastTime, zdo.GetLong(ZDOVars.s_lastTime, 0L));
            if (!(num > before.Seconds))
                return;
            float rest = num - (int)(num / before.Seconds) * before.Seconds;
            if (rest > 0f && rest < before.Seconds)
                zdo.Set(ZDOVars.s_product, rest);
        }

        /// <summary>The seconds the game's own tick added: 0 without a time stamp or when the clock did not move.</summary>
        private static float Elapsed(long before, long after)
        {
            if (before == 0L || after <= before)
                return 0f;
            return (float)new TimeSpan(after - before).TotalSeconds;
        }
    }
}
