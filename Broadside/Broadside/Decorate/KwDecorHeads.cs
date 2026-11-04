using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The quay's pieces at the pier heads (DECORATE.md, "The quay"): a pier head is a dock's end over the water, at least
    /// 2.4 m wide, ranked by how far it is from where its dock is stepped onto (the yard's heart when no entry was read).
    /// The harbour beacon at the outermost one's far corner, its back to the end; the signal bell at the next pier head's
    /// corner, or at the other corner of the same one when it is 4.6 m wide or more; a dock lantern on its post off a corner
    /// of the outermost end narrower than that (a walkway's end); on a dock 4 m wide or more, lobster pots and the stone anchor
    /// (the net rack beside them as a filler) along a water side nearest its outer end, placed together or not at all;
    /// pilings in the shallows 1.8 m off the outermost head's corners. The middle of every pier head stays clear.
    /// </summary>
    public static class KwDecorHeads
    {
        public const string PierFishing = "fishing gear on the pier";
        private const float Out = 1.8f, PotsFrom = 2.6f, AnchorOff = 1.55f, RackOff = -2.6f, Least = 0.85f, Slide = 1f, Wide = 4f, Narrow = 1f;

        public static void Beacon(KwDecorLayout layout)
        {
            List<KwEdgeRun> heads = Heads(layout);
            if (heads.Count == 0)
                return;
            (float near, float far) = KwDecorQuay.Corners(heads[0]);
            if (!KwDecorQuay.Against(layout, heads[0], far, "beacon", KwDecorPlace.Deck, 2, -1))
                KwDecorQuay.Against(layout, heads[0], near, "beacon", KwDecorPlace.Deck, 2, -1);
        }

        public static void Bell(KwDecorLayout layout)
        {
            List<KwEdgeRun> heads = Heads(layout);
            if (heads.Count > 1 || (heads.Count == 1 && KwDecorQuay.Wide(heads[0], true)))
            {
                KwEdgeRun head = heads[heads.Count > 1 ? 1 : 0];
                (float near, float far) = KwDecorQuay.Corners(head);
                if (!KwDecorQuay.Against(layout, head, near, "bell", KwDecorPlace.Deck, 3, -1))
                    KwDecorQuay.Against(layout, head, far, "bell", KwDecorPlace.Deck, 3, -1);
            }
        }

        /// <summary>
        /// A dock lantern off a corner of the outermost end too narrow for the beacon, when no pier head has one: on its post
        /// just past the end, its lamp in over the end (<see cref="KwDecorLights"/>).
        /// </summary>
        public static void EndLight(KwDecorLayout layout)
        {
            List<KwEdgeRun> ends = layout.Runs.FindAll(r => r.Tip && r.Water && r.Length >= Narrow && !KwDecorQuay.Wide(r, false));
            if (Heads(layout).Count > 0 || ends.Count == 0)
                return;
            ends.Sort((a, b) => Outer(layout, Middle(layout, b)).CompareTo(Outer(layout, Middle(layout, a))));
            if (!KwDecorLights.OnPiling(layout, ends[0], ends[0].Length - 0.5f, 2))
                KwDecorLights.OnPiling(layout, ends[0], 0.5f, 2);
        }

        public static void Pilings(KwDecorLayout layout)
        {
            List<KwEdgeRun> heads = Heads(layout);
            if (heads.Count == 0)
                return;
            (float near, float far) = KwDecorQuay.Corners(heads[0]);
            Piling(layout, heads[0], near - 0.5f);
            Piling(layout, heads[0], far + 0.5f);
        }

        private static void Piling(KwDecorLayout layout, KwEdgeRun head, float t)
        {
            KwDecorKind? kind = KwDecorKind.Find("pilings");
            KwDecorGrid grid = layout.Area.Grid;
            Vector3 at = head.PointAt(grid, t) + grid.Dir(head.Out) * Out;
            if (kind == null || !grid.CellOf(at, out int k) || grid.Cells[k].Kind != KwCellKind.Water)
                return;
            at.y = grid.Cells[k].Y;
            KwSlot slot = new KwSlot(KwDecorPlace.Shallows, k, at, Vector3.zero, false, -1);
            layout.TryAdd(new KwDecorPlan(kind, at, KwDecorPose.Free((float)layout.Dice.NextDouble() * 360f, layout.Dice), slot, 4, -1));
        }

        /// <summary>Lobster pots and the stone anchor along a water side, nearest its outer end, on the first side they fit.</summary>
        public static void Fishing(KwDecorLayout layout)
        {
            List<KwEdgeRun> sides = layout.Runs.FindAll(r => r.Water && !r.Tip && r.Length >= 4f && r.Width >= Wide);
            sides.Sort((a, b) => Outer(layout, Far(layout, b)).CompareTo(Outer(layout, Far(layout, a))));
            int group = layout.Group(PierFishing);
            foreach (KwEdgeRun side in sides)
            {
                if (Gear(layout, side, group))
                    return;
            }
        }

        // Sliding a metre at a time in from the outer end until the pots and the anchor fit together.
        private static bool Gear(KwDecorLayout layout, KwEdgeRun side, int group)
        {
            KwDecorGrid grid = layout.Area.Grid;
            bool up = Outer(layout, side.PointAt(grid, side.Length)) >= Outer(layout, side.PointAt(grid, 0f));
            float toward = up ? 1f : -1f;
            for (float s = 0f; s < side.Length; s += Slide)
            {
                float c = up ? side.Length - PotsFrom - s : PotsFrom + s;
                if (c < Least || c > side.Length - Least)
                    return false;
                KwDecorPlan? pots = KwDecorQuay.Plan(layout, side, c, "creels", KwDecorPlace.Deck, 3, group);
                KwDecorPlan? anchor = pots != null ? KwDecorQuay.Plan(layout, side, c + AnchorOff * toward, "anchor", KwDecorPlace.Shore, 3, group) : null;
                if (pots == null || anchor == null)
                    continue;
                layout.Plans.Add(pots);
                layout.Plans.Add(anchor);
                KwDecorQuay.Against(layout, side, c + RackOff * toward, "netrack", KwDecorPlace.Deck, 4, group);
                return true;
            }
            return false;
        }

        // Pier heads over the water wide enough, the outermost first.
        private static List<KwEdgeRun> Heads(KwDecorLayout layout)
        {
            List<KwEdgeRun> heads = layout.Runs.FindAll(r => r.Tip && r.Water && KwDecorQuay.Wide(r, false));
            heads.Sort((a, b) => Outer(layout, Middle(layout, b)).CompareTo(Outer(layout, Middle(layout, a))));
            return heads;
        }

        private static Vector3 Middle(KwDecorLayout layout, KwEdgeRun run) => run.PointAt(layout.Area.Grid, run.Length / 2f);

        private static Vector3 Far(KwDecorLayout layout, KwEdgeRun run)
        {
            KwDecorGrid grid = layout.Area.Grid;
            Vector3 a = run.PointAt(grid, 0f), b = run.PointAt(grid, run.Length);
            return Outer(layout, a) >= Outer(layout, b) ? a : b;
        }

        // How far a point is from where its dock is stepped onto (from the yard's heart with no entry read).
        private static float Outer(KwDecorLayout layout, Vector3 at) =>
            layout.Area.Entries.Count > 0 ? KwDecorLikes.Nearest(layout.Area.Entries, at) : KwDecorLikes.Flat(at - layout.Area.Fields!.Heart);
    }
}
