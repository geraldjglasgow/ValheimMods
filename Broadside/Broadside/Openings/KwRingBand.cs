using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One band of an opening's ring (<see cref="KwOpeningRing"/>): two outlines, each edge in the steps the frame's lining
    /// takes along the cut's same edge (<see cref="KwFrameRing.Steps"/>, so the cut's corners are the lining's own), and on
    /// each the share of the tunnel's bend and of the move onto the wall's face line. The first band runs from the cut to
    /// the frame's outer edge (half the ring, if that is narrower) wholly on the face line, so the wall under a frame is
    /// where the frame is laid out from, the bend falling as it goes; the second on to the ring's outer edge, where both
    /// have fallen to nothing. Pure.
    /// </summary>
    public sealed class KwRingBand
    {
        private KwRingBand(List<Vector2> inner, List<Vector2> outer, Vector2 bend, Vector2 on) =>
            (Inner, Outer, Bend, On) = (inner, outer, bend, on);

        public List<Vector2> Inner { get; }
        public List<Vector2> Outer { get; }

        /// <summary>The share of the tunnel's bend on the inner outline (x) and the outer (y).</summary>
        public Vector2 Bend { get; }

        /// <summary>The share of the move onto the wall's face line on the inner outline (x) and the outer (y).</summary>
        public Vector2 On { get; }

        private static readonly ConditionalWeakTable<KwOpening, Plan> Plans = new ConditionalWeakTable<KwOpening, Plan>();

        /// <summary>
        /// An opening's ring worked out once (every strake piece of its part asks): its outer edge, the cut grown by its
        /// tunnel's ring (unclamped at a doorway's foot, so the ring goes round it; the cut itself where there is no ring),
        /// that edge's bounds, and its bands.
        /// </summary>
        public static (List<Vector2> Edge, Rect Bounds, List<KwRingBand> Bands) Ring(KwOpening o)
        {
            Plan plan = Plans.GetValue(o, made => new Plan(made));
            return (plan.Edge, plan.Bounds, plan.Bands);
        }

        private sealed class Plan
        {
            public Plan(KwOpening o)
            {
                Edge = Ringed(o) is KwOpeningTunnel t ? o.Outline(t.Ring) : o.Outline();
                Bounds = KwOpeningShape.Bounds(Edge);
                Bands = Of(o);
            }

            public List<Vector2> Edge { get; }
            public Rect Bounds { get; }
            public List<KwRingBand> Bands { get; }
        }

        // An opening's ring as its two bands; none where its tunnel has no ring.
        private static List<KwRingBand> Of(KwOpening o)
        {
            List<KwRingBand> bands = new List<KwRingBand>();
            if (!(Ringed(o) is KwOpeningTunnel t))
                return bands;
            float wide = KwOpeningFrames.Width(o.Frame).Wide, under = Mathf.Min(wide, t.Ring / 2f), mid = 1f - under / t.Ring;
            List<Vector2> cut = o.Outline(), frame = KwOpeningFrames.Grown(o, wide);
            List<Vector2> inner = Stepped(cut, cut, frame), middle = Stepped(o.Outline(under), cut, frame), outer = Stepped(o.Outline(t.Ring), cut, frame);
            bands.Add(new KwRingBand(inner, middle, new Vector2(1f, mid), new Vector2(1f, 1f)));
            bands.Add(new KwRingBand(middle, outer, new Vector2(mid, 0f), new Vector2(1f, 0f)));
            return bands;
        }

        // The opening's tunnel when it has a ring the strakes turn in; none otherwise.
        private static KwOpeningTunnel? Ringed(KwOpening o) => o.Surface is KwOpeningTunnel t && t.Ring > 0f && t.Strength > 0f ? t : null;

        // An outline with each edge in as many steps as the frame's lining takes along the cut's same edge.
        private static List<Vector2> Stepped(List<Vector2> ring, List<Vector2> cut, List<Vector2> frame)
        {
            List<Vector2> stepped = new List<Vector2>();
            for (int i = 0; i < cut.Count; i++)
            {
                int j = (i + 1) % cut.Count, n = KwFrameRing.Steps(cut[i], cut[j], frame[i], frame[j]);
                for (int k = 0; k < n; k++)
                    stepped.Add(Vector2.Lerp(ring[i], ring[j], (float)k / n));
            }
            return stepped;
        }
    }
}
