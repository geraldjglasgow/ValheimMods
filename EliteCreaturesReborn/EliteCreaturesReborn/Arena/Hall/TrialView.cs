using System;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// What the hall shows of the trial (the doors, the runes, Hel's hover), read from <see cref="ArenaTrial"/> at most
    /// four times a second and shared by all three, so the trial's ZDO is not read by each of them every frame. A read
    /// that throws is logged once and the hall shows no trial until it reads again.
    /// </summary>
    internal static class TrialView
    {
        private const float Every = 0.25f;

        private static float _next;
        private static bool _reported;

        /// <summary>A trial runs, from its countdown to its end.</summary>
        public static bool Running { get; private set; }

        /// <summary>The wave running; 0 in the countdown or with no trial.</summary>
        public static int Wave { get; private set; }

        /// <summary>The running wave is a boss wave.</summary>
        public static bool BossWave { get; private set; }

        /// <summary>A trial runs and is not in a break between waves (<see cref="ArenaTrial.Break"/>): a wave is being
        /// fought, or the countdown runs. The runes are calm otherwise.</summary>
        public static bool Fighting { get; private set; }

        public static void Refresh()
        {
            if (Time.time < _next)
            {
                return;
            }
            _next = Time.time + Every;
            try
            {
                Running = ArenaTrial.Running;
                Wave = Running ? ArenaTrial.Wave : 0;
                BossWave = Running && ArenaTrial.BossWave;
                Fighting = Running && !ArenaTrial.Break;
            }
            catch (Exception e)
            {
                Running = BossWave = Fighting = false;
                Wave = 0;
                Report(e);
            }
        }

        private static void Report(Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                Log.Error($"Hel's hall could not read the trial: {e}");
            }
        }
    }
}
