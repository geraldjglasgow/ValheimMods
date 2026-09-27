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
        private const float LogRange = 25f;

        private static readonly List<Pending> pending = new List<Pending>();

        internal static void Initialize() => Ticker.OnUpdate("EarthWright survival clearing", Update);

        /// <summary>Call before the first blow: remembers the logs already lying around, then schedules the passes.</summary>
        public static void Schedule(ClearPlan plan)
        {
            pending.Add(new Pending { Due = Time.time + Interval, Left = Passes, Plan = plan, OldLogs = LogsNear(plan.Area) });
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

        private static int RunPass(Pending next, Player player)
        {
            ClearPlan first = next.Plan;
            ClearPlan pass = first.Again();
            pass.Player = player;
            List<ZNetView> views = ObjectScan.Inside(first.Area);
            if ((first.Mask & ClearCategory.Logs) != 0)
                views.AddRange(NewLogsOutside(first.Area, next.OldLogs));
            ClearPlanner.AddAll(pass, views);
            return ClearExecutor.Run(pass, player);
        }

        private static HashSet<ZDOID> LogsNear(ClearArea area)
        {
            HashSet<ZDOID> ids = new HashSet<ZDOID>();
            foreach (ZNetView view in ObjectScan.Within(area.Center, area.Reach + LogRange, null))
            {
                if (view.GetComponent<TreeLog>() != null)
                    ids.Add(view.GetZDO().m_uid);
            }
            return ids;
        }

        private static List<ZNetView> NewLogsOutside(ClearArea area, HashSet<ZDOID> oldLogs)
        {
            List<ZNetView> logs = new List<ZNetView>();
            foreach (ZNetView view in ObjectScan.Within(area.Center, area.Reach + LogRange, p => !area.Contains(p)))
            {
                if (view.GetComponent<TreeLog>() != null && !oldLogs.Contains(view.GetZDO().m_uid))
                    logs.Add(view);
            }
            return logs;
        }
    }
}
