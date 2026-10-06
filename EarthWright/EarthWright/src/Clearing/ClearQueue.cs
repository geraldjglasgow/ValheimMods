using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Clearing targets wait here and are struck a few per frame (<see cref="TargetsPerFrame"/> objects, and no more once
    /// <see cref="StrikesPerFrame"/> hits went out, a rock taking one per hit area), so felling a forest or clearing a
    /// 128 m area spreads its falling trees, drops and object changes over a few frames instead of one long hitch and one
    /// burst to the server. An object already waiting is not queued again (the follow-up passes find it too), and one
    /// that is gone by its turn is skipped. The queue empties when the player is gone.
    /// </summary>
    internal static class ClearQueue
    {
        private const int TargetsPerFrame = 10;
        private const int StrikesPerFrame = 150;

        internal sealed class Job
        {
            public ClearPlan Plan;
            public ClearTarget Target;
            public Player Player;
        }

        private static readonly Queue<Job> jobs = new Queue<Job>();
        private static readonly HashSet<ZNetView> queued = new HashSet<ZNetView>();

        internal static void Initialize() => Ticker.OnUpdate("EarthWright clearing queue", Tick);

        /// <summary>Queues the target; false when it is gone or already waiting.</summary>
        public static bool Add(ClearPlan plan, ClearTarget target, Player player)
        {
            ZNetView view = target.View;
            if (view == null || !view.IsValid() || !queued.Add(view))
                return false;
            jobs.Enqueue(new Job { Plan = plan, Target = target, Player = player });
            return true;
        }

        private static void Tick()
        {
            if (jobs.Count == 0)
                return;
            if (Player.m_localPlayer == null || ZNetScene.instance == null)
            {
                jobs.Clear();
                queued.Clear();
                return;
            }
            int strikes = 0;
            for (int done = 0; done < TargetsPerFrame && strikes < StrikesPerFrame && jobs.Count > 0; done++)
            {
                Job job = jobs.Dequeue();
                queued.Remove(job.Target.View);
                strikes += Safe.Call("EarthWright clearing", ClearExecutor.ClearOne, job, 0);
            }
        }
    }
}
