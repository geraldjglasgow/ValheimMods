using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Survival clearing with drops leaves new objects behind, as chopping does in the game: a felled tree drops a log
    /// and leaves a stump, a log breaks into halves, a boulder breaks into a mineable rock. A few passes after the
    /// first blow clear what appeared, in the same area and (for logs, which roll) up to <see cref="LogRange"/> metres
    /// beyond it, but only logs that were not lying there before the first blow. A pass that finds nothing ends it.
    /// </summary>
    public static class SurvivalFollowUp
    {
        private sealed class Pending
        {
            public float Due;
            public int Left;
            public ClearPlan Plan;
            public HashSet<ZDOID> OldLogs;
        }

        private const float Interval = 1.5f;
        private const int Passes = 4;

        /// <summary>How far beyond the area new logs are looked for (they roll).</summary>
        public const float LogRange = 25f;

        private static readonly List<Pending> pending = new List<Pending>();
        private static readonly List<ZNetView> around = new List<ZNetView>();
        private static readonly List<ZNetView> candidates = new List<ZNetView>();

        internal static void Initialize() => Ticker.OnUpdate("EarthWright survival clearing", Update);

        /// <summary>Call before the first blow: remembers the logs already lying around, then schedules the passes.</summary>
        public static void Schedule(ClearPlan plan)
        {
            List<ZNetView> nearby = plan.Nearby ?? ObjectScan.Within(plan.Area.Center, plan.Area.Reach + LogRange, null);
            pending.Add(new Pending { Due = Time.time + Interval, Left = Passes, Plan = plan, OldLogs = Logs(nearby) });
        }

        private static void Update()
        {
            if (pending.Count == 0)
                return;
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null)
            {
                pending.Clear();
                return;
            }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Pending next = pending[i];
                if (Time.time < next.Due)
                    continue;
                int struck = RunPass(next, player);
                next.Left--;
                next.Due = Time.time + Interval;
                if (struck == 0 || next.Left <= 0)
                    pending.RemoveAt(i);
            }
        }

        /// <summary>One walk over the loaded objects per pass: what lies inside the area, and new logs that rolled out of it.</summary>
        private static int RunPass(Pending next, Player player)
        {
            ClearPlan first = next.Plan;
            ClearPlan pass = first.Again();
            pass.Player = player;
            bool logs = (first.Mask & ClearCategory.Logs) != 0;
            ObjectScan.Collect(first.Area.Center, first.Area.Reach + (logs ? LogRange : 0f), null, around);
            candidates.Clear();
            foreach (ZNetView view in around)
            {
                if (first.Area.Contains(view.transform.position) || (logs && IsNewLog(view, next.OldLogs)))
                    candidates.Add(view);
            }
            ClearPlanner.AddAll(pass, candidates);
            return ClearExecutor.Run(pass, player);
        }

        /// <summary>The logs among the objects (lying there before the first blow).</summary>
        private static HashSet<ZDOID> Logs(List<ZNetView> views)
        {
            HashSet<ZDOID> ids = new HashSet<ZDOID>();
            foreach (ZNetView view in views)
            {
                if (view != null && view.IsValid() && view.GetComponent<TreeLog>() != null)
                    ids.Add(view.GetZDO().m_uid);
            }
            return ids;
        }

        private static bool IsNewLog(ZNetView view, HashSet<ZDOID> oldLogs)
        {
            return view.GetComponent<TreeLog>() != null && !oldLogs.Contains(view.GetZDO().m_uid);
        }
    }
}
