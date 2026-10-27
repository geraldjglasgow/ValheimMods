using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin as a mast's ropes see it (<see cref="KwRigShip"/>), from its saved text, in its kit frame: its whole room from
    /// under its floor up to its roof's top, out to its walls' outer faces (a side at the hull: the hull's outside, carried
    /// up to the roof), which no rope may pass through; its roof's outline at its top (<see cref="KwCabinOutline.Roof"/>),
    /// where a rope may tie on; and whether a point stands on its roof.
    /// </summary>
    public sealed class KwRigCabin : KwAnchorSolid
    {
        // A wall's thickness past its outline, the roof's planks over its top, a margin under the floor.
        private const float Wall = 0.1f, Slab = 0.06f, Floor = 0.15f;

        public KwRigCabin(KwAnchorPose pose, KwCabinSpec spec, KwBiomeTier tier) : base("a cabin")
        {
            (Pose, Spec, Tier) = (pose, spec, tier);
            Roof = KwCabinOutline.Roof(spec);
            float wide = KwAnchorHull.Widest(spec.Stations) + Wall;
            Round(pose, new Vector3(spec.X0 - Wall, -wide, spec.Floor - Floor), new Vector3(spec.X1 + Wall, wide, spec.Top + Slab));
        }

        public KwAnchorPose Pose { get; }
        public KwCabinSpec Spec { get; }
        public KwBiomeTier Tier { get; }

        /// <summary>The roof's outline at its top: how far it reaches left and right at each place along it.</summary>
        public List<(float X, float Left, float Right)> Roof { get; }

        /// <summary>The roof's reach to the left and right at x along the cabin; null past its ends.</summary>
        public (float Left, float Right)? Reach(float x)
        {
            for (int i = 0; i + 1 < Roof.Count; i++)
            {
                (float x0, float l0, float r0) = Roof[i];
                (float x1, float l1, float r1) = Roof[i + 1];
                if (x < x0 - 1e-4f || x > x1 + 1e-4f)
                    continue;
                float t = x1 - x0 < 1e-4f ? 0f : Mathf.Clamp01((x - x0) / (x1 - x0));
                return (Mathf.Lerp(l0, l1, t), Mathf.Lerp(r0, r1, t));
            }
            return null;
        }

        /// <summary>Whether a point (the piece's kit) stands on the roof's top: at its height and inside its outline.</summary>
        public bool Carries(Vector3 p)
        {
            Vector3 k = Pose.Local(p);
            return Mathf.Abs(k.z - Spec.Top) < 0.12f && Over(k, 0.05f);
        }

        /// <summary>The roof's top over a point (the piece's kit x, y) as a height there; null off the roof.</summary>
        public float? RoofTop(Vector3 p)
        {
            Vector3 k = Pose.Local(new Vector3(p.x, p.y, 0f));
            return Over(k, 0f) ? Pose.Out(new Vector3(k.x, k.y, Spec.Top)).z : (float?)null;
        }

        // Inside the roof's outline, widened by a margin.
        private bool Over(Vector3 k, float margin)
        {
            (float Left, float Right)? reach = Reach(k.x);
            return reach.HasValue && k.y < reach.Value.Left + margin && k.y > -reach.Value.Right - margin;
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = Pose.Local(p);
            if (k.x < Spec.X0 - Wall - r || k.x > Spec.X1 + Wall + r || k.z < Spec.Floor - Floor - r || k.z > Spec.Top + Slab + r)
                return false;
            KwCabinSide side = k.y >= 0f ? Spec.Left : Spec.Right;
            float x = Mathf.Clamp(k.x, Spec.X0, Spec.X1);
            float reach = side.AtHull ? KwShapes.WidthAt(KwCabinOutline.Up(Spec, x).Half, k.z) + KwPlanks.Lap : KwCabinOutline.Reach(Spec, side, x, k.z);
            return Mathf.Abs(k.y) < reach + r;
        }
    }

    /// <summary>
    /// A railing on a cabin's roof as a mast's ropes see it (<see cref="KwRoofRail"/>, its kit frame on the roof's top): along
    /// each railed edge of the roof's outline its cap and its top rail, each a rail's width in from the edge; where a ladder
    /// comes up, a gap. A rope ties on to it there (<see cref="Covers"/>) and passes over it elsewhere.
    /// </summary>
    public sealed class KwRigRoofRail : KwAnchorSolid
    {
        private readonly KwAnchorPose pose;
        private readonly KwRoofRail rail;
        private readonly List<KwRoofSegment> railed;

        public KwRigRoofRail(KwAnchorPose pose, KwRoofRail rail) : base("a roof railing")
        {
            (this.pose, this.rail, railed) = (pose, rail, rail.Railed);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
            foreach (KwRoofSegment edge in rail.Edges)
                (min, max) = (Vector2.Min(min, Vector2.Min(edge.A, edge.B)), Vector2.Max(max, Vector2.Max(edge.A, edge.B)));
            Round(pose, new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, KwRailing.RailHeight + KwRailing.TopRail));
        }

        /// <summary>Whether its rail stands at a point on its roof's outline (the piece's kit): a railed edge there, no gap.</summary>
        public bool Covers(Vector3 p)
        {
            Vector3 k = pose.Local(p);
            Vector2 at = new Vector2(k.x, k.y);
            if (Mathf.Abs(k.z) > 0.1f || rail.Gaps.Exists(gap => (gap - at).magnitude < KwRoofRail.Gap / 2f + 0.05f))
                return false;
            return railed.Exists(edge => (edge.Nearest(at) - at).magnitude < 0.08f);
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = pose.Local(p);
            bool cap = k.z > -r && k.z < KwRailing.CapTop + r, top = k.z > KwRailing.RailHeight - r && k.z < KwRailing.RailHeight + KwRailing.TopRail + r;
            if (!cap && !top)
                return false;
            Vector2 at = new Vector2(k.x, k.y);
            return railed.Exists(edge => (edge.Nearest(at) - at).magnitude < 2f * KwRailing.Half + r);
        }
    }

    /// <summary>
    /// A mast's sails and spars as its own and its neighbours' ropes see them (<see cref="KwRigShip"/>), fully set, in the
    /// mast's kit frame: a square sail's yard and its cloth bellied forward of the mast; a sail along the ship's boom and its
    /// cloth, a thin wall aft of the mast (a triangle in steps up its luff).
    /// </summary>
    public static class KwRigSails
    {
        public static void Add(List<KwAnchorSolid> into, KwAnchorPose pose, List<KwSailLayout> sails)
        {
            foreach (KwSailLayout s in sails)
            {
                if (s.Square)
                    Square(into, pose, s);
                else
                    Along(into, pose, s);
            }
        }

        private static void Square(List<KwAnchorSolid> into, KwAnchorPose pose, KwSailLayout s)
        {
            float half = s.Width / 2f;
            into.Add(new KwAnchorRod("a yard", pose, new Vector3(s.YardX, -half - 0.15f, s.At), new Vector3(s.YardX, half + 0.15f, s.At), s.Spar + 0.02f));
            Vector3 min = new Vector3(s.YardX - 0.06f, -half, s.At - s.Height), max = new Vector3(s.YardX + s.BellyAt(s.Height) + 0.06f, half, s.At);
            into.Add(new KwAnchorBox("a sail", pose, min, max));
        }

        // A boom and its cloth: a gaff sail's whole, a triangle's in four steps narrowing up the mast.
        private static void Along(List<KwAnchorSolid> into, KwAnchorPose pose, KwSailLayout s)
        {
            float aft = -s.Width - 0.1f, high = s.Spec.Top - s.At;
            into.Add(new KwAnchorRod("a boom", pose, new Vector3(-s.Radius, 0f, s.At), new Vector3(aft, 0f, s.At), s.Spar + 0.02f));
            int steps = s.Spec.Shape == 1 ? 4 : 1;
            for (int i = 0; i < steps; i++)
            {
                float reach = s.Spec.Shape == 1 ? (aft + s.Radius) * (1f - (float)i / steps) - s.Radius : aft;
                Vector3 min = new Vector3(reach, -0.25f, s.At + high * i / steps), max = new Vector3(-s.Radius, 0.25f, s.At + high * (i + 1) / steps);
                into.Add(new KwAnchorBox("a sail", pose, min, max));
            }
        }
    }
}
