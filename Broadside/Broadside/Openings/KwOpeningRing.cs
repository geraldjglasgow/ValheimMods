using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A strake's piece, or a painted band's over it, cut round the openings on its side so each cut runs straight through
    /// the board along its opening's tunnel (<see cref="KwOpeningTunnel"/>), as <see cref="KwOpeningSkin.Apart"/> hands them
    /// to <see cref="KwStrakes"/> and <see cref="KwBandMesh"/>. A board's outside and inside faces are each corner's way out
    /// times a depth (<see cref="KwGirthPoint.Out"/>), so a corner on the cut whose way is the tunnel's, set on the wall's
    /// face line, lies on the tunnel's line through it at each face: the very line the frame's lining runs along, the cut's
    /// corners being the lining's own (each edge in its steps, <see cref="KwFrameRing.Steps"/>). Round each cut a ring as wide
    /// as its tunnel's <see cref="KwOpeningTunnel.Ring"/> turns back from the tunnel to the wall's own depth and fan on its
    /// outer edge, in bands of quads from each step of the cut out to the same step of the ring (<see cref="KwRingBand"/>);
    /// outside the ring the piece is cut as before (<see cref="KwOpeningCut"/>). Every point is placed in the piece's (along, up) first and given the piece's
    /// measures there as its fan draws it (<see cref="KwPieceFan"/>), and every corner put on its neighbours' edges
    /// (<see cref="KwRingSeal"/>), so a point two pieces share comes out the same in both and nothing opens between them.
    /// Pure.
    /// </summary>
    public static class KwOpeningRing
    {
        private const float Sliver = 1e-10f;

        /// <summary>A piece less the openings on its side, as convex pieces turned as it is; the piece itself when none reaches it.</summary>
        public static List<List<KwGirthPoint>> Apart(List<KwGirthPoint> piece, List<KwOpening>? holes)
        {
            List<List<KwGirthPoint>> whole = new List<List<KwGirthPoint>> { piece };
            if (holes == null || holes.Count == 0 || piece.Count < 3)
                return whole;
            List<KwRingPoint> corners = new List<KwRingPoint>();
            for (int i = 0; i < piece.Count; i++)
                corners.Add(new KwRingPoint(new Vector2(piece[i].At.x, piece[i].At.z), 0f, -1, i));
            List<List<KwRingPoint>> parts = new List<List<KwRingPoint>> { corners };
            bool touched = false;
            for (int h = 0; h < holes.Count; h++)
                parts = Cut(parts, holes[h], h, ref touched);
            if (!touched)
                return whole;
            KwPieceFan fan = new KwPieceFan(piece, Strake(piece));
            return KwRingSeal.Seal(parts).ConvertAll(part => part.ConvertAll(p => Lift(fan, p, holes)));
        }

        // The strake a piece lies on: where the middle of its measures round the hull falls.
        private static int Strake(List<KwGirthPoint> piece)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (KwGirthPoint p in piece)
                (lo, hi) = (Mathf.Min(lo, p.Girth), Mathf.Max(hi, p.Girth));
            return Mathf.FloorToInt((lo + hi) / 2f / KwUv.StrakeWidth);
        }

        // Every part a hole's ring reaches cut: what lies outside the ring as before (cut by the ring's own edges, not its
        // steps, which would only add slivers along them), and the ring's bands of quads within it (KwRingBand).
        private static List<List<KwRingPoint>> Cut(List<List<KwRingPoint>> parts, KwOpening o, int h, ref bool touched)
        {
            (List<Vector2> edge, Rect bounds, List<KwRingBand> bands) = KwRingBand.Ring(o);
            List<List<KwRingPoint>> next = new List<List<KwRingPoint>>();
            foreach (List<KwRingPoint> part in parts)
            {
                if (!KwOpeningCut.Touches(part, p => p.At, bounds))
                {
                    next.Add(part);
                    continue;
                }
                touched = true;
                next.AddRange(KwOpeningCut.Apart(part, p => p.At, KwRingPoint.Lerp, new[] { edge }));
                foreach (KwRingBand band in bands)
                {
                    for (int i = 0; i < band.Inner.Count; i++)
                        Quad(next, part, band, i, h);
                }
            }
            return next;
        }

        // A band's quad from step i of its inner outline out to step i of its outer (anticlockwise as the cut is), within a
        // part and turned as the part runs, each corner with its band's shares.
        private static void Quad(List<List<KwRingPoint>> into, List<KwRingPoint> part, KwRingBand band, int i, int h)
        {
            int j = (i + 1) % band.Inner.Count;
            List<KwRingPoint> kept = new List<KwRingPoint>
            {
                new KwRingPoint(band.Inner[i], band.Bend.x, h, -1, band.On.x), new KwRingPoint(band.Outer[i], band.Bend.y, h, -1, band.On.y),
                new KwRingPoint(band.Outer[j], band.Bend.y, h, -1, band.On.y), new KwRingPoint(band.Inner[j], band.Bend.x, h, -1, band.On.x),
            };
            float turn = Signed(part) < 0f ? -1f : 1f;
            if (turn < 0f)
                kept.Reverse();
            for (int e = 0; e < part.Count && kept.Count >= 3; e++)
            {
                Vector2 a = part[e].At, b = part[(e + 1) % part.Count].At;
                kept = Keep(kept, p => turn * Cross(b - a, p.At - a));
            }
            if (kept.Count >= 3 && Mathf.Abs(Signed(kept)) > Sliver)
                into.Add(kept);
        }

        // A point of the cut piece as the strake draws it: one of the piece's own corners, or the piece's measures where its
        // fan lies there; by its share, moved onto the wall's face line there and its way out bent to the tunnel's, so the
        // cut itself lies on the very lines the lining runs along (the board's fan strays a few millimetres from the face
        // line where the sections change fast, at a stem).
        private static KwGirthPoint Lift(KwPieceFan fan, KwRingPoint p, List<KwOpening> holes)
        {
            KwGirthPoint g = p.Corner >= 0 ? fan.Corner(p.Corner) : fan.At(p.At);
            if ((p.W <= 0f && p.On <= 0f) || p.Hole < 0 || !(holes[p.Hole].Surface is KwOpeningTunnel t))
                return g;
            float on = p.On * t.Strength;
            Vector3 at = Vector3.Lerp(g.At, t.Face.Point(p.At.x, p.At.y, 0f), on), fibre = Vector3.Lerp(g.Out, t.Face.Fibre(p.At.x, p.At.y), on);
            return new KwGirthPoint(at, t.Bend(fibre, t.Face.Out(p.At.x, p.At.y), p.W), g.Girth, g.Along, g.Sheer, g.Keel);
        }

        // The part of a ring where over is not below 0 (one cut, Sutherland-Hodgman).
        private static List<KwRingPoint> Keep(List<KwRingPoint> ring, System.Func<KwRingPoint, float> over)
        {
            List<KwRingPoint> kept = new List<KwRingPoint>();
            for (int i = 0; i < ring.Count; i++)
            {
                KwRingPoint a = ring[i], b = ring[(i + 1) % ring.Count];
                float da = over(a), db = over(b);
                if (da >= 0f)
                    kept.Add(a);
                if ((da > 0f && db < 0f) || (da < 0f && db > 0f))
                    kept.Add(KwRingPoint.Lerp(a, b, da / (da - db)));
            }
            return kept;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // A ring's signed area, measured from its first corner so a small piece far from the origin keeps its area.
        private static float Signed(List<KwRingPoint> ring)
        {
            float twice = 0f;
            for (int i = 1; i + 1 < ring.Count; i++)
                twice += Cross(ring[i].At - ring[0].At, ring[i + 1].At - ring[0].At);
            return twice / 2f;
        }
    }

    /// <summary>
    /// A point of a piece being cut round openings (<see cref="KwOpeningRing"/>): where it lies in the piece's (along, up),
    /// what share of its way out bends to its opening's tunnel, which opening (-1 none), which of the piece's own corners it
    /// is (-1 none) and what share of the way it moves from the board's fan onto the wall's face line.
    /// </summary>
    public readonly struct KwRingPoint
    {
        public KwRingPoint(Vector2 at, float w, int hole, int corner = -1, float on = 0f) => (At, W, Hole, Corner, On) = (at, w, hole, corner, on);

        public Vector2 At { get; }
        public float W { get; }
        public int Hole { get; }
        public int Corner { get; }
        public float On { get; }

        /// <summary>The point a share f of the way from a to b: no longer a corner, its shares blended.</summary>
        public static KwRingPoint Lerp(KwRingPoint a, KwRingPoint b, float f) => new KwRingPoint(Vector2.LerpUnclamped(a.At, b.At, f),
            a.W + (b.W - a.W) * f, a.W > 0f || a.On > 0f ? a.Hole : b.Hole, -1, a.On + (b.On - a.On) * f);
    }
}
