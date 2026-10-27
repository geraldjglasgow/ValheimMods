using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A level frame in the world: an origin and a heading; points into its kit axes (x along it, y to its left, z up) and
    /// back. Plain arithmetic, the same on every peer and in an offline check.
    /// </summary>
    public readonly struct KwStackFrame
    {
        public KwStackFrame(Vector3 origin, Vector3 forward)
        {
            Origin = origin;
            Forward = forward;
        }

        public Vector3 Origin { get; }
        public Vector3 Forward { get; }

        /// <summary>The frame's left, its kit +y (a cabin's port side when its front is the bow).</summary>
        public Vector3 Left => new Vector3(-Forward.z, 0f, Forward.x);

        public Vector3 ToKit(Vector3 world)
        {
            Vector3 off = world - Origin;
            return new Vector3(Vector3.Dot(off, Forward), Vector3.Dot(off, Left), off.y);
        }

        public Vector3 ToWorld(Vector3 kit) => Origin + Forward * kit.x + Left * kit.y + Vector3.up * kit.z;

        /// <summary>How far along its heading a world point lies (frames on one keel line are compared by it).</summary>
        public float Along(Vector3 world) => Vector3.Dot(world, Forward);
    }

    /// <summary>
    /// A cabin where it stands (<see cref="KwCabinLift"/>): its spec, fitted as it builds, in its frame; its deck and its
    /// roof's top as world heights; whether a point is on its roof, and how far it is from the roof's edge. Pure numbers.
    /// </summary>
    public readonly struct KwCabinAt
    {
        public KwCabinAt(KwCabinSpec spec, KwStackFrame frame)
        {
            Spec = spec;
            Frame = frame;
        }

        public KwCabinSpec Spec { get; }
        public KwStackFrame Frame { get; }

        public float Floor => Frame.Origin.y + Spec.Floor;
        public float Top => Frame.Origin.y + Spec.Top;

        /// <summary>Its frame's place along its heading.</summary>
        public float Start => Frame.Along(Frame.Origin);

        /// <summary>The same cabin in the same place: its saved text and its frame's origin alike.</summary>
        public bool Same(KwCabinAt other) =>
            Spec.ToString() == other.Spec.ToString() && (Frame.Origin - other.Frame.Origin).sqrMagnitude < 1e-6f;

        /// <summary>Whether a world point is on its roof: within a height of its top, inside its outline grown by a margin.</summary>
        public bool OnRoof(Vector3 world, float margin, float height)
        {
            KwCabinSpec s = Spec;
            Vector3 kit = Frame.ToKit(world);
            if (s.Unbuilt != null || Mathf.Abs(kit.z - s.Top) > height || kit.x < s.X0 - margin || kit.x > s.X1 + margin)
                return false;
            float x = Mathf.Clamp(kit.x, s.X0, s.X1);
            float left = KwCabinOutline.Reach(s, s.Left, x, s.Top), right = KwCabinOutline.Reach(s, s.Right, x, s.Top);
            return kit.y < left + margin && kit.y > -right - margin;
        }

        /// <summary>How far in plan a world point is from its roof's outline, at its nearest edge (no roof: very far).</summary>
        public float FromEdge(Vector3 world)
        {
            if (Spec.Unbuilt != null)
                return float.MaxValue;
            Vector3 kit = Frame.ToKit(world);
            Vector2 p = new Vector2(kit.x, kit.y);
            float best = float.MaxValue;
            foreach ((Vector3 a, Vector3 b, string _) in KwCabinRoofs.KitEdges(Spec))
                best = Mathf.Min(best, Gap(p, new Vector2(a.x, a.y), new Vector2(b.x, b.y)));
            return best;
        }

        /// <summary>The roof's outline as a roof railing keeps it, in this frame at its top (<see cref="KwRoofSite"/>'s edges).</summary>
        public List<KwRoofSegment> Edges()
        {
            List<KwRoofSegment> edges = new List<KwRoofSegment>();
            if (Spec.Unbuilt != null)
                return edges;
            foreach ((Vector3 a, Vector3 b, string side) in KwCabinRoofs.KitEdges(Spec))
                edges.Add(new KwRoofSegment(KwRoofRail.Letter(side), new Vector2(a.x, a.y), new Vector2(b.x, b.y)));
            return edges;
        }

        private static float Gap(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 run = b - a;
            float t = run.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(p - a, run) / run.sqrMagnitude) : 0f;
            return (a + run * t - p).magnitude;
        }
    }
}
