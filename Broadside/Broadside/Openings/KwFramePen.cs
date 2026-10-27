using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Draws an opening's frame (<see cref="KwOpeningFrames"/>) step by step round its rings (<see cref="KwFrameRing"/>) on
    /// its wall, kept between the wall's foot and top there (<c>lo</c>, <c>hi</c>): a frame's face at a depth with its rim
    /// back into the wall along its outer edge, the lining boards round the cut, and the iron band. Every point is the
    /// wall's own (<see cref="KwOpeningSurface.Point"/>), so the frame bends with the wall. Each step's texture runs along
    /// its board (<see cref="KwFrameGrain"/>): the plank wood's grain for the wood, the old density for the iron.
    /// </summary>
    public sealed class KwFramePen
    {
        private readonly MeshBuilder b;
        private readonly KwOpeningSurface s;
        private readonly float lo, hi;

        public KwFramePen(MeshBuilder b, KwOpeningSurface s, float lo, float hi) => (this.b, this.s, this.lo, this.hi) = (b, s, lo, hi);

        /// <summary>A frame's face between the rings at a depth, its outer edge's rim back to another depth; a kind of board.</summary>
        public void Face(KwFrameRing r, float depth, float from, int kind, float perMetre = KwPlankGrain.PerMetre)
        {
            float along = 0f;
            int board = -1;
            foreach (int i in r.Steps())
            {
                int j = (i + 1) % r.Count;
                if (r.Board[i] != board)
                    (board, along) = (r.Board[i], 0f);
                KwFrameGrain g = Grain(r.Inner[i], r.Inner[j], depth, along, Shift(board, kind, perMetre), perMetre);
                along += g.Length;
                if (r.Floor[i])
                    continue;
                Polygon(new List<Vector2> { r.Inner[i], r.Inner[j], r.Outer[j], r.Outer[i] }, depth, g);
                Rim(r.Outer[i], r.Outer[j], depth, from, g);
            }
        }

        /// <summary>The lining boards round the cut, from the outside frame's face to the inside frame's (none on a doorway's floor).</summary>
        public void Lining(KwFrameRing r, float front, float back)
        {
            float along = 0f;
            int board = -1;
            foreach (int i in r.Steps())
            {
                int j = (i + 1) % r.Count;
                if (r.Board[i] != board)
                    (board, along) = (r.Board[i], 0f);
                KwFrameGrain g = Grain(r.Inner[i], r.Inner[j], front, along, KwPlankGrain.Shift(board, KwPlankGrain.Lining), KwPlankGrain.PerMetre);
                along += g.Length;
                (Vector2 p, Vector2 q, bool kept) = Within(r.Inner[i], r.Inner[j]);
                if (kept && !r.Floor[i])
                    Quad(new[] { s.Point(p.x, p.y, front), s.Point(q.x, q.y, front), s.Point(q.x, q.y, back), s.Point(p.x, p.y, back) }, g);
            }
        }

        // A step's grain: along its inner edge at a depth, from where its board has come to.
        private KwFrameGrain Grain(Vector2 p, Vector2 q, float depth, float along, Vector2 shift, float perMetre)
        {
            Vector3 a = s.Point(p.x, p.y, depth), c = s.Point(q.x, q.y, depth);
            return new KwFrameGrain(a, c, along, shift, perMetre);
        }

        private static Vector2 Shift(int board, int kind, float perMetre) => perMetre == KwPlankGrain.PerMetre ? KwPlankGrain.Shift(board, kind) : Vector2.zero;

        // A face's polygon in (u, z) kept between the heights, at a depth.
        private void Polygon(List<Vector2> face, float depth, KwFrameGrain g)
        {
            List<Vector2> kept = KwClip.Between(face, p => p.y, Vector2.LerpUnclamped, lo, hi);
            if (kept.Count < 3)
                return;
            List<Vector3> points = kept.ConvertAll(p => s.Point(p.x, p.y, depth));
            b.Polygon(points, points.ConvertAll(g.Uv));
        }

        // The rim under a face's outer edge from p to q, from its depth back to another.
        private void Rim(Vector2 p0, Vector2 q0, float depth, float from, KwFrameGrain g)
        {
            (Vector2 p, Vector2 q, bool kept) = Within(p0, q0);
            if (kept && Vector2.Distance(p, q) > 1e-4f)
                Quad(new[] { s.Point(p.x, p.y, depth), s.Point(q.x, q.y, depth), s.Point(q.x, q.y, from), s.Point(p.x, p.y, from) }, g);
        }

        private void Quad(Vector3[] corners, KwFrameGrain g)
        {
            List<Vector3> points = new List<Vector3>(corners);
            b.Polygon(points, points.ConvertAll(g.Uv));
        }

        // An edge cut to the heights lo to hi; false when none of it is within them.
        private (Vector2 A, Vector2 C, bool Kept) Within(Vector2 p, Vector2 q)
        {
            float rise = q.y - p.y;
            if (Mathf.Abs(rise) < 1e-6f)
                return (p, q, p.y >= lo - 1e-4f && p.y <= hi + 1e-4f);
            float a = (lo - p.y) / rise, c = (hi - p.y) / rise;
            float t0 = Mathf.Max(0f, Mathf.Min(a, c)), t1 = Mathf.Min(1f, Mathf.Max(a, c));
            return (Vector2.Lerp(p, q, t0), Vector2.Lerp(p, q, t1), t1 - t0 > 1e-4f);
        }
    }

    /// <summary>
    /// A step of a frame's board on the wood: a point's texture point from how far along the board it lies (from the board's
    /// start, along the step's inner edge) and how far off that edge (across the face, down the rim, through the lining).
    /// </summary>
    public readonly struct KwFrameGrain
    {
        private readonly Vector3 a, way;
        private readonly float along, perMetre;
        private readonly Vector2 shift;

        public KwFrameGrain(Vector3 a, Vector3 c, float along, Vector2 shift, float perMetre)
        {
            (this.a, this.along, this.shift, this.perMetre) = (a, along, shift, perMetre);
            Length = Vector3.Distance(a, c);
            way = Length > 1e-6f ? (c - a) / Length : Vector3.right;
        }

        /// <summary>The step's length along its inner edge.</summary>
        public float Length { get; }

        public Vector2 Uv(Vector3 p)
        {
            Vector3 d = p - a;
            float on = Vector3.Dot(d, way);
            return new Vector2((along + on) * perMetre, (d - way * on).magnitude * perMetre) + shift;
        }
    }
}
