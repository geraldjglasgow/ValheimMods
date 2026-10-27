using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The way a hole opens through a launched ship's boxes (<see cref="KwHoleBoxes"/>), so shots, thrown things and small
    /// objects pass where the look shows a hole. A slab takes a hole when its thin side faces within 60 degrees of the hole's
    /// normal and its middle plane lies within <see cref="Near"/> of the hole's place: the ship's boxes lie on the faces the
    /// holes are put on (the hull's on its planking, <see cref="KwHullSkin"/>; decks', roofs' and walls' under their faces),
    /// so only the wall a hole is in takes it, and the wood between two holes keeps its box. The hole's ragged outline is
    /// seen square to the slab's face (a few centimetres from the hole's own plane), and the passage through it is a few
    /// rectangles in the slab's own axes round its middle: one wide, one square, one tall, each as large as fits inside the
    /// outline (<see cref="Passage"/>), so together they follow the hole without ever opening more than it shows, however
    /// the slab is turned (a hull box lies along its strakes, which climb at the ends). Each is carried along the hole's
    /// normal to the slab's outer and inner faces and opened over both, as the look's hole runs through the wall along its
    /// normal (a hole beside a fold in the hull meets the next box at a slant). The slab is cut round them as an opening's
    /// slab is (<see cref="KwOpeningBoxes.Apart"/>). Pure.
    /// </summary>
    public static class KwHoleGaps
    {
        private const float Facing = 0.5f, Near = 0.25f, Least = 0.02f;
        private static readonly float[] Widths = { 0.35f, 0.6f, 0.85f };

        /// <summary>The passage through an outline round its middle: rectangles in the outline's own axes from the middle.</summary>
        public static List<Rect> Passage(List<Vector2> outline, float reach)
        {
            List<Rect> rects = new List<Rect>();
            float wide = 0f;
            foreach (Vector2 p in outline)
                wide = Mathf.Max(wide, Mathf.Abs(p.x));
            foreach (float share in Widths)
            {
                float w = wide * share, h = Tallest(outline, w, reach);
                if (w >= Least && h >= Least)
                    rects.Add(Rect.MinMaxRect(-w, -h, w, h));
            }
            return rects;
        }

        /// <summary>The gaps a hole opens in a slab, in the slab's two broad axes from its middle; none when it misses.</summary>
        public static List<Rect> Of(KwHoleSlab slab, KwHoleShape hole)
        {
            List<Rect> gaps = new List<Rect>();
            Vector3 thin = slab.Axis(slab.Thin), off = slab.Centre - hole.Centre;
            float reach = slab.Half.magnitude + hole.Reach;
            if (off.sqrMagnitude > reach * reach || Mathf.Abs(Vector3.Dot(hole.Normal, thin)) < Facing || Mathf.Abs(Vector3.Dot(off, thin)) > Near)
                return gaps;
            Vector2 mid = Seen(slab, hole.Centre);
            Rect face = Rect.MinMaxRect(-slab.Half[slab.A], -slab.Half[slab.B], slab.Half[slab.A], slab.Half[slab.B]);
            foreach (Rect r in Passage(hole.Outline.ConvertAll(c => Seen(slab, hole.Point(c, 0f)) - mid), hole.Reach))
            {
                if (Carried(slab, hole, r, mid, out Rect gap) && gap.Overlaps(face))
                    gaps.Add(gap);
            }
            return gaps;
        }

        // A passage rectangle (round the hole's middle, seen on the slab at mid) carried along the hole's normal from where its
        // corners lie on the hole through the slab, no further than the hole's own wall (KwHoleCut's depths): the rectangle
        // over all of it; false where the slab lies wholly beyond the hole's wall.
        private static bool Carried(KwHoleSlab slab, KwHoleShape hole, Rect r, Vector2 mid, out Rect gap)
        {
            Vector3 a = slab.Axis(slab.A), b = slab.Axis(slab.B), thin = slab.Axis(slab.Thin);
            float cos = Vector3.Dot(hole.Normal, thin), half = slab.Half[slab.Thin];
            float m00 = Vector3.Dot(hole.Along, a), m01 = Vector3.Dot(hole.Up, a), m10 = Vector3.Dot(hole.Along, b), m11 = Vector3.Dot(hole.Up, b), det = m00 * m11 - m01 * m10;
            Vector2 drift = new Vector2(Vector3.Dot(hole.Normal, a), Vector3.Dot(hole.Normal, b)), lo = Vector2.positiveInfinity, hi = Vector2.negativeInfinity;
            bool meets = false;
            foreach (Vector2 q in new[] { r.min, r.max, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMin) })
            {
                Vector2 own = Mathf.Abs(det) > 1e-4f ? new Vector2(m11 * q.x - m01 * q.y, m00 * q.y - m10 * q.x) / det : Vector2.zero;
                float depth = Vector3.Dot(hole.Point(own, 0f) - slab.Centre, thin), s0 = (half - depth) / cos, s1 = (-half - depth) / cos;
                float near = Mathf.Clamp(Mathf.Min(s0, s1), -KwHoleCut.Inside, KwHoleCut.Outside), far = Mathf.Clamp(Mathf.Max(s0, s1), -KwHoleCut.Inside, KwHoleCut.Outside);
                meets |= Mathf.Min(s0, s1) < KwHoleCut.Outside && Mathf.Max(s0, s1) > -KwHoleCut.Inside;
                (lo, hi) = (Vector2.Min(lo, Vector2.Min(q + drift * near, q + drift * far)), Vector2.Max(hi, Vector2.Max(q + drift * near, q + drift * far)));
            }
            gap = Rect.MinMaxRect(lo.x + mid.x, lo.y + mid.y, hi.x + mid.x, hi.y + mid.y);
            return meets;
        }

        /// <summary>What is left of a slab's face with the gaps taken out, rectangles in its broad axes from its middle.</summary>
        public static List<Rect> Left(KwHoleSlab slab, List<Rect> gaps) =>
            KwOpeningBoxes.Apart(Rect.MinMaxRect(-slab.Half[slab.A], -slab.Half[slab.B], slab.Half[slab.A], slab.Half[slab.B]), gaps);

        // A point seen square to the slab's face, in its two broad axes from its middle.
        private static Vector2 Seen(KwHoleSlab slab, Vector3 p) =>
            new Vector2(Vector3.Dot(p - slab.Centre, slab.Axis(slab.A)), Vector3.Dot(p - slab.Centre, slab.Axis(slab.B)));

        // The tallest rectangle of half width w round the middle that lies inside an outline (a star round the middle).
        private static float Tallest(List<Vector2> outline, float w, float reach)
        {
            float lo = 0f, hi = reach;
            for (int i = 0; i < 12; i++)
            {
                float h = (lo + hi) / 2f;
                if (Inside(outline, w, h))
                    lo = h;
                else
                    hi = h;
            }
            return lo;
        }

        // Whether the rectangle (-w, -h) to (w, h) lies inside the outline: its corners and edges' middles in it, no corner of
        // the outline within it.
        private static bool Inside(List<Vector2> outline, float w, float h)
        {
            foreach (Vector2 p in outline)
            {
                if (Mathf.Abs(p.x) < w && Mathf.Abs(p.y) < h)
                    return false;
            }
            Vector2[] checks = { new Vector2(w, h), new Vector2(-w, h), new Vector2(-w, -h), new Vector2(w, -h), new Vector2(w, 0f), new Vector2(-w, 0f), new Vector2(0f, h), new Vector2(0f, -h) };
            foreach (Vector2 c in checks)
            {
                if (!Within(outline, c))
                    return false;
            }
            return true;
        }

        /// <summary>Whether a point lies inside a closed outline (crossings of a line to its right).</summary>
        public static bool Within(List<Vector2> outline, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            {
                Vector2 a = outline[i], b = outline[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }

    /// <summary>A ship's box as the holes see it: its middle and axes in the colliders' frame, its half sizes, which axis is thin.</summary>
    public readonly struct KwHoleSlab
    {
        private readonly Vector3 x, y, z;

        public KwHoleSlab(Vector3 centre, Vector3 x, Vector3 y, Vector3 z, Vector3 half)
        {
            (Centre, this.x, this.y, this.z, Half) = (centre, x, y, z, half);
            Thin = half.x <= half.y && half.x <= half.z ? 0 : half.y <= half.z ? 1 : 2;
            (A, B) = Thin == 0 ? (1, 2) : Thin == 1 ? (0, 2) : (0, 1);
        }

        public Vector3 Centre { get; }
        public Vector3 Half { get; }
        public int Thin { get; }
        public int A { get; }
        public int B { get; }

        public Vector3 Axis(int i) => i == 0 ? x : i == 1 ? y : z;
    }
}
