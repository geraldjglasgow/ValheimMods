using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Convex pieces of a mesh less a hole (<see cref="KwHoleCut"/>): only what lies within the hole's depths
    /// (<see cref="KwHoleCut.Outside"/> out of its face, <see cref="KwHoleCut.Inside"/> in through it) is cut, so a big face
    /// that only passes through the hole's place keeps the rest of itself. Within them, what lies inside the hole's ragged
    /// outline seen along its normal is taken away: the outline is a star round its middle, so the plane outside it is the
    /// wedges between the rays from the middle through each edge's corners, beyond that edge, each convex; a piece is kept
    /// as its part in each wedge (three cuts a wedge, Sutherland-Hodgman), one piece a wedge at most. A piece clear of the
    /// outline's furthest corner is kept whole and one within its nearest edge dropped whole, uncut. The pieces' corners keep their places in 3D (a cut goes straight through the board along
    /// the normal), every channel blended along a cut edge; pieces under <see cref="Sliver"/> square metres are left out. Pure.
    /// </summary>
    public static class KwHoleClip
    {
        private const float Sliver = 1e-7f;
        private static readonly Vector2 Middle = Vector2.zero;

        public static List<List<KwHoleVertex>> Minus(List<List<KwHoleVertex>> pieces, KwHoleShape hole)
        {
            List<List<KwHoleVertex>> kept = new List<List<KwHoleVertex>>(), within = new List<List<KwHoleVertex>>();
            foreach (List<KwHoleVertex> piece in pieces)
                Depths(piece, hole, kept, within);
            foreach (List<KwHoleVertex> piece in within)
                Star(piece, hole, kept);
            return kept;
        }

        // A piece split at the hole's depths: what lies beyond them kept as it is, what lies within them to be cut.
        private static void Depths(List<KwHoleVertex> piece, KwHoleShape hole, List<List<KwHoleVertex>> kept, List<List<KwHoleVertex>> within)
        {
            (float lo, float hi) = (float.MaxValue, float.MinValue);
            foreach (KwHoleVertex v in piece)
                (lo, hi) = (Mathf.Min(lo, hole.Depth(v.P)), Mathf.Max(hi, hole.Depth(v.P)));
            if (hi <= -KwHoleCut.Inside || lo >= KwHoleCut.Outside || (lo >= -KwHoleCut.Inside && hi <= KwHoleCut.Outside))
            {
                (hi <= -KwHoleCut.Inside || lo >= KwHoleCut.Outside ? kept : within).Add(piece);
                return;
            }
            List<KwHoleVertex> rest = piece;
            foreach ((float level, float way) in new[] { (KwHoleCut.Outside, 1f), (-KwHoleCut.Inside, -1f) })
            {
                List<KwHoleVertex> beyond = Deeper(rest, hole, level, way);
                if (beyond.Count >= 3 && Area(beyond) > Sliver)
                    kept.Add(beyond);
                rest = Deeper(rest, hole, level, -way);
            }
            if (rest.Count >= 3 && Area(rest) > Sliver)
                within.Add(rest);
        }

        // A piece within the hole's depths less its star: whole when clear of its reach, gone within its inner circle, else
        // what lies in each wedge outside the star (Outside).
        private static void Star(List<KwHoleVertex> piece, KwHoleShape hole, List<List<KwHoleVertex>> into)
        {
            (float near, float far) = Reach(piece, hole);
            if (near >= hole.Reach || far <= hole.Inner)
            {
                if (near >= hole.Reach)
                    into.Add(piece);
                return;
            }
            List<Vector2> outline = hole.Outline;
            Vector2[] flat = piece.ConvertAll(v => hole.Flat(v.P)).ToArray();
            for (int k = 0; k < outline.Count; k++)
            {
                Vector2 a = outline[k], b = outline[(k + 1) % outline.Count];
                if (!Beside(flat, Middle, a, 1f) && !Beside(flat, Middle, b, -1f) && !Beside(flat, a, b, -1f))
                    Outside(piece, hole, a, b, into);
            }
        }

        // Whether a piece lies wholly on the other side of a line from the side a wedge keeps (way as Keep's).
        private static bool Beside(Vector2[] flat, Vector2 from, Vector2 to, float way)
        {
            Vector2 run = to - from;
            foreach (Vector2 f in flat)
            {
                if (way * (run.x * (f.y - from.y) - run.y * (f.x - from.x)) > 0f)
                    return false;
            }
            return true;
        }

        // The part of a piece in one wedge outside the star: between the rays from the middle through an edge's two
        // corners (anticlockwise of a, clockwise of b) and beyond the edge. The wedges are convex and together cover all
        // the plane outside the star, each once.
        private static void Outside(List<KwHoleVertex> piece, KwHoleShape hole, Vector2 a, Vector2 b, List<List<KwHoleVertex>> into)
        {
            List<KwHoleVertex> part = Keep(piece, hole, Middle, a, 1f);
            if (part.Count >= 3)
                part = Keep(part, hole, Middle, b, -1f);
            if (part.Count >= 3)
                part = Keep(part, hole, a, b, -1f);
            if (part.Count >= 3 && Area(part) > Sliver)
                into.Add(part);
        }

        // How near and how far a convex piece comes to the hole's middle, seen along its normal (near 0 when it covers it).
        private static (float Near, float Far) Reach(List<KwHoleVertex> piece, KwHoleShape hole)
        {
            (float near, float far, float turn) = (float.MaxValue, 0f, 0f);
            bool covers = true;
            for (int i = 0; i < piece.Count; i++)
            {
                Vector2 a = hole.Flat(piece[i].P), b = hole.Flat(piece[(i + 1) % piece.Count].P), run = b - a;
                float t = run.sqrMagnitude > 1e-12f ? Mathf.Clamp01(-Vector2.Dot(a, run) / run.sqrMagnitude) : 0f;
                (near, far) = (Mathf.Min(near, (a + run * t).magnitude), Mathf.Max(far, a.magnitude));
                float side = run.x * -a.y - run.y * -a.x;
                if (Mathf.Abs(side) > 1e-9f)
                    (covers, turn) = (covers && (turn == 0f || Mathf.Sign(side) == turn), turn == 0f ? Mathf.Sign(side) : turn);
            }
            return (covers && turn != 0f ? 0f : near, far);
        }

        // The part of a ring on one side of a line in the hole's plane (way 1: its left, inside an anticlockwise edge).
        private static List<KwHoleVertex> Keep(List<KwHoleVertex> ring, KwHoleShape hole, Vector2 from, Vector2 to, float way)
        {
            List<KwHoleVertex> kept = new List<KwHoleVertex>(ring.Count + 2);
            Vector2 run = to - from;
            Vector2 fa = hole.Flat(ring[ring.Count - 1].P) - from;
            float da = way * (run.x * fa.y - run.y * fa.x);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 fb = hole.Flat(ring[i].P) - from;
                float db = way * (run.x * fb.y - run.y * fb.x);
                if ((da > 0f && db < 0f) || (da < 0f && db > 0f))
                    kept.Add(KwHoleVertex.Lerp(ring[(i + ring.Count - 1) % ring.Count], ring[i], da / (da - db)));
                if (db >= 0f)
                    kept.Add(ring[i]);
                da = db;
            }
            return kept;
        }

        // The part of a ring at or past a depth (way 1: out from it; -1: in from it).
        private static List<KwHoleVertex> Deeper(List<KwHoleVertex> ring, KwHoleShape hole, float level, float way)
        {
            List<KwHoleVertex> kept = new List<KwHoleVertex>(ring.Count + 2);
            float da = way * (hole.Depth(ring[ring.Count - 1].P) - level);
            for (int i = 0; i < ring.Count; i++)
            {
                float db = way * (hole.Depth(ring[i].P) - level);
                if ((da > 0f && db < 0f) || (da < 0f && db > 0f))
                    kept.Add(KwHoleVertex.Lerp(ring[(i + ring.Count - 1) % ring.Count], ring[i], da / (da - db)));
                if (db >= 0f)
                    kept.Add(ring[i]);
                da = db;
            }
            return kept;
        }

        /// <summary>A flat convex ring's area in 3D, measured from its first corner.</summary>
        public static float Area(List<KwHoleVertex> ring)
        {
            Vector3 twice = Vector3.zero, o = ring[0].P;
            for (int i = 1; i + 1 < ring.Count; i++)
                twice += Vector3.Cross(ring[i].P - o, ring[i + 1].P - o);
            return twice.magnitude / 2f;
        }
    }
}
