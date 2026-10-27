using System;
using System.Collections;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Runs the blueprint work that is spread over frames (building a ship, removing one)
    /// on one hidden object kept across scenes. Each job works until <see cref="Budget"/> of the frame is spent, then waits
    /// for the next frame. A job that throws is logged (<see cref="Guard"/>) and stopped; its <c>done</c> always runs.
    /// </summary>
    public sealed class KwBpRunner : MonoBehaviour
    {
        private const double Budget = 0.004;
        private static KwBpRunner? instance;

        /// <summary>When this frame's share of the work runs out.</summary>
        public static double Until() => Time.realtimeSinceStartupAsDouble + Budget;

        public static bool Over(double until) => Time.realtimeSinceStartupAsDouble > until;

        public static void Run(string what, IEnumerator job, Action done)
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("KwBpRunner");
                DontDestroyOnLoad(holder);
                instance = holder.AddComponent<KwBpRunner>();
            }
            instance.StartCoroutine(Guarded(what, job, done));
        }

        private static IEnumerator Guarded(string what, IEnumerator job, Action done)
        {
            while (Step(what, job))
                yield return job.Current;
            done();
        }

        private static bool Step(string what, IEnumerator job)
        {
            try
            {
                return Guard.Run<bool>(what, job.MoveNext);
            }
            catch (Exception)
            {
                // Guard has logged it under the mod's name; the job stops here.
                return false;
            }
        }
    }

    /// <summary>
    /// A job's share of a frame: at most <c>most</c> items and <see cref="KwBpRunner.Until"/>'s time. After each item the
    /// job asks <see cref="Spent"/>; when it is, the job yields and calls <see cref="Restart"/> on the next frame.
    /// </summary>
    public sealed class KwBpPace
    {
        private readonly int most;
        private double until = KwBpRunner.Until();
        private int count;

        public KwBpPace(int most) => this.most = most;

        public bool Spent() => ++count >= most || KwBpRunner.Over(until);

        public void Restart()
        {
            count = 0;
            until = KwBpRunner.Until();
        }
    }
}
