using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The quay's furniture along the docks' edges (DECORATE.md, "The quay"), laid out before the scenes, every piece set
    /// against its edge with its back to it, facing in; the ends where a dock is stepped onto are never used, and nothing
    /// stands in the water alongside where a ship moors. The pier heads first (<see cref="KwDecorHeads"/>: the harbour beacon,
    /// the signal bell, a lantern at a narrow pier's end, fishing gear near a wide pier's head), then along the water sides
    /// mooring posts every 7 m (density 1; on a dock section over its piling stations, <see cref="KwDecorMoorings"/>) and
    /// fenders hung between them (3), staggered so one side's post faces the other's fender, and dock lanterns every 11 m,
    /// alternating sides (2), each over one of the dock's pilings, its lamp in over the deck (<see cref="KwDecorLights"/>).
    /// A piece set against a dock's edge may reach
    /// into its lane as far as leaves the dock 1.6 m clear across with room for a post on the other side
    /// (<see cref="KwSlot.Reach"/>), so a narrow walkway takes only its posts. Each piece
    /// searches half a metre either way, then a metre, when its spot is taken. Plain arithmetic but the edge's line
    /// (<see cref="KwDecorWorld.Line"/>) and the world's checks.
    /// </summary>
    public static class KwDecorQuay
    {
        private const float PostFirst = 1.5f, PostEvery = 7f, LightFirst = 4.5f, LightEvery = 11f;
        private const float Corner = 0.8f, HeadWide = 2.4f, BothWide = 4.6f, Inside = 0.05f, Hang = 0.3f, Clear = 1.6f, Opposite = 0.5f;
        private static readonly float[] Nudges = { 0f, 0.5f, -0.5f, 1f, -1f };

        public static IEnumerable<IKwDecorTask> Tasks(KwDecorLayout layout)
        {
            yield return new KwOnceTask(() => KwDecorHeads.Beacon(layout));
            yield return new KwOnceTask(() => KwDecorHeads.Bell(layout));
            yield return new KwOnceTask(() => KwDecorHeads.EndLight(layout));
            yield return new KwOnceTask(() => KwDecorHeads.Fishing(layout));
            yield return new KwRunTask(layout, r => r.Water && !r.Tip, r => Along(layout, r, Side(r, PostFirst, PostEvery), PostEvery, Post));
            yield return new KwRunTask(layout, r => r.Water && !r.Tip, r => Along(layout, r, Side(r, LightFirst, LightEvery), LightEvery, Light));
            yield return new KwRunTask(layout, r => r.Water && !r.Tip, r => Fenders(layout, r));
            yield return new KwOnceTask(() => KwDecorHeads.Pilings(layout));
        }

        // One side of a dock (its edge facing +x or +z) starts at the rhythm's first step, the other half a stride on.
        private static float Side(KwEdgeRun run, float first, float every) => run.Out < 2 ? first : first + every / 2f;

        private static void Along(KwDecorLayout layout, KwEdgeRun run, float first, float every, Func<KwDecorLayout, KwEdgeRun, float, bool> put)
        {
            float start = run.Length < first + 0.4f ? run.Length / 2f : first;
            for (float t = start; t <= run.Length - 0.4f; t += every)
                Nudged(layout, run, t, put);
        }

        // Over a piling station on a dock section's side, else against the edge (KwDecorMoorings).
        private static bool Post(KwDecorLayout layout, KwEdgeRun run, float t) => KwDecorMoorings.Post(layout, run, t, 1);

        // A lantern pole over one of the dock's pilings, its lamp in over the deck (KwDecorLights).
        private static bool Light(KwDecorLayout layout, KwEdgeRun run, float t) => KwDecorLights.OnPiling(layout, run, t, 2);

        // Between each side's posts: a fender where the other side has its post.
        private static void Fenders(KwDecorLayout layout, KwEdgeRun run)
        {
            for (float t = Side(run, PostFirst + PostEvery / 2f, PostEvery) % PostEvery; t <= run.Length - 1f; t += PostEvery)
                Nudged(layout, run, t, Hung);
        }

        private static bool Nudged(KwDecorLayout layout, KwEdgeRun run, float t, Func<KwDecorLayout, KwEdgeRun, float, bool> put)
        {
            foreach (float nudge in Nudges)
            {
                if (put(layout, run, t + nudge))
                    return true;
            }
            return false;
        }

        /// <summary>A standing piece set against the run's edge at a distance along it, facing in, added when it fits.</summary>
        public static bool Against(KwDecorLayout layout, KwEdgeRun run, float t, string piece, KwDecorPlace place, int level, int group)
        {
            KwDecorPlan? plan = Plan(layout, run, t, piece, place, level, group);
            if (plan == null)
                return false;
            layout.Plans.Add(plan);
            return true;
        }

        /// <summary>That piece planned, when it fits there; not added.</summary>
        public static KwDecorPlan? Plan(KwDecorLayout layout, KwEdgeRun run, float t, string piece, KwDecorPlace place, int level, int group)
        {
            KwDecorKind? kind = KwDecorKind.Find(piece);
            if (kind == null || t < 0.2f || t > run.Length - 0.2f)
                return null;
            KwDecorGrid grid = layout.Area.Grid;
            int k = run.CellAt(t);
            Vector3 back = grid.Dir(run.Out), line = KwDecorWorld.Line(run.PointAt(grid, t), back, Inside);
            KwSlot slot = new KwSlot(place, k, line, back, true, grid.Cells[k].Region, Reach(run));
            (Vector3 at, Quaternion turn) = KwDecorPose.Pushed(slot, kind.Fit, layout.Dice);
            KwDecorPlan plan = new KwDecorPlan(kind, at, turn, slot, level, group);
            return KwDecorFitCheck.Fits(layout.Area, plan, layout.Plans) ? plan : null;
        }

        /// <summary>How far in from its edge a piece may reach and leave the dock 1.6 m clear with room for a post across.</summary>
        public static float Reach(KwEdgeRun run) => Mathf.Max(0f, run.Width - Clear - Opposite);

        // Fenders hung on the dock's side over the water, facing out.
        private static bool Hung(KwDecorLayout layout, KwEdgeRun run, float t)
        {
            KwDecorKind? kind = KwDecorKind.Find("fenders");
            if (kind == null || t < 1f || t > run.Length - 1f)
                return false;
            KwDecorGrid grid = layout.Area.Grid;
            int k = run.CellAt(t);
            Vector3 back = grid.Dir(run.Out), line = KwDecorWorld.Line(run.PointAt(grid, t), back, Hang);
            KwSlot slot = new KwSlot(KwDecorPlace.Edge, k, line, -back, true, grid.Cells[k].Region);
            (Vector3 at, Quaternion turn) = KwDecorPose.Hung(slot, kind.Fit);
            return layout.TryAdd(new KwDecorPlan(kind, at, turn, slot, 3, -1));
        }

        /// <summary>A pier head's corner spots: near its first end and near its last.</summary>
        public static (float Near, float Far) Corners(KwEdgeRun run) => (Corner, run.Length - Corner);

        /// <summary>Wide enough for one piece at a corner and still a way to its end; for two.</summary>
        public static bool Wide(KwEdgeRun run, bool both) => run.Length >= (both ? BothWide : HeadWide);
    }

    /// <summary>A task over the layout's edge runs a filter takes, one run a step.</summary>
    public sealed class KwRunTask : IKwDecorTask
    {
        private readonly KwDecorLayout layout;
        private readonly Func<KwEdgeRun, bool> takes;
        private readonly Action<KwEdgeRun> work;
        private int next;

        public KwRunTask(KwDecorLayout layout, Func<KwEdgeRun, bool> takes, Action<KwEdgeRun> work)
        {
            (this.layout, this.takes, this.work) = (layout, takes, work);
        }

        public bool Step()
        {
            if (next >= layout.Runs.Count)
                return true;
            KwEdgeRun run = layout.Runs[next++];
            if (takes(run))
                work(run);
            return next >= layout.Runs.Count;
        }
    }

    /// <summary>A task done in one step.</summary>
    public sealed class KwOnceTask : IKwDecorTask
    {
        private readonly Action work;

        public KwOnceTask(Action work)
        {
            this.work = work;
        }

        public bool Step()
        {
            work();
            return true;
        }
    }
}
