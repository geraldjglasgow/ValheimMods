using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The planking cut into strakes (<see cref="KwGirth"/>: level boards on the sides, one width round on the bottom),
    /// each a clinker board (<see cref="KwPlanks"/>). Between two stations each quad of neighbouring section points is cut
    /// where the strakes' edges cross it, and each piece is drawn as its strake's board there: the outside face, standing
    /// Lap proud at the strake's lower edge and flush at its upper, the inside face, the lip under the lower edge (and
    /// along the keel under a strake the keel cuts) and, on the sheer, the gunwale's cap. A part's ends are closed through
    /// the thickness strake by strake. The texture goes on by real measure (<see cref="KwUv.Strake"/>): across, the
    /// strake's own row from its lower edge, a strake the keel or the sheer cuts cut off with it; along, the strake's own
    /// length from the part's first station, counted the way it runs at each station (<see cref="KwGirth"/>), so a curving
    /// stem stretches nothing. Every face carries its strake and side (<see cref="MeshBuilder.Row"/>), which
    /// <see cref="KwPlankShift"/> starts the strake into the texture by.
    /// </summary>
    public static class KwStrakes
    {
        private const float W = KwUv.StrakeWidth, OnEdge = 1e-4f;

        /// <summary>The planking between station s and the next, on one side (1 port, -1 starboard).</summary>
        public static void Gap(MeshBuilder b, KwGirth girth, int s, float side)
        {
            float[] ga = girth.Of(s), gc = girth.Of(s + 1);
            for (int j = 0; j + 1 < ga.Length; j++)
            {
                int lo = Mathf.Max(Mathf.FloorToInt(Mathf.Min(ga[j], gc[j]) / W), girth.Lowest);
                int hi = Mathf.Min(Mathf.FloorToInt((Mathf.Max(ga[j + 1], gc[j + 1]) - OnEdge) / W), girth.Highest);
                for (int k = lo; k <= hi; k++)
                {
                    b.Row = new Vector2(k, side);
                    List<KwGirthPoint> quad = new List<KwGirthPoint>
                        { Corner(girth, s, j, side, k), Corner(girth, s, j + 1, side, k), Corner(girth, s + 1, j + 1, side, k), Corner(girth, s + 1, j, side, k) };
                    foreach (List<KwGirthPoint> piece in KwOpeningSkin.Apart(girth, side, KwClip.Between(quad, p => p.Girth, KwGirthPoint.Lerp, k * W, (k + 1) * W)))
                        Board(b, k, piece);
                }
            }
        }

        /// <summary>A part's end at station s closed through the planking's thickness, strake by strake, on one side.</summary>
        public static void EndCap(MeshBuilder b, KwGirth girth, int s, float side)
        {
            float keel = girth.Keel(s), sheer = girth.Sheer(s);
            for (int k = Mathf.Max(Mathf.FloorToInt(keel / W + OnEdge), girth.Lowest); k * W < sheer - OnEdge && k <= girth.Highest; k++)
            {
                float lo = Mathf.Max(k * W, keel), hi = Mathf.Min((k + 1) * W, sheer);
                if (hi - lo < OnEdge)
                    continue;
                b.Row = new Vector2(k, side);
                List<float> marks = Marks(girth.Of(s), lo, hi);
                for (int m = 0; m + 1 < marks.Count; m++)
                    EndSlice(b, On(girth, s, marks[m], side, k), On(girth, s, marks[m + 1], side, k), k);
            }
        }

        /// <summary>How far out of the planking's line strake k stands at a girth: Lap at its lower edge, flush at its upper.</summary>
        public static float Proud(float girth, int k) => KwPlanks.Lap * (1f - Mathf.Clamp01((girth - k * W) / W));

        /// <summary>
        /// How far out strake k stands at a point of it: as <see cref="Proud(float, int)"/>, but Lap along the keel however
        /// the keel cuts the strake, so the lowest strake's lap closes on the keel as a strake's lower edge does.
        /// </summary>
        public static float Proud(KwGirthPoint p, int k) => p.Keel >= 1f ? KwPlanks.Lap : Proud(p.Girth, k);

        /// <summary>Point j of station s's section as strake k's cut starts from it, with how far along that strake it lies.</summary>
        internal static KwGirthPoint Corner(KwGirth girth, int s, int j, float side, int k)
        {
            Station st = girth.Station(s);
            float along = girth.Along(s, k, KwPlanks.Base(st, j, 1f));
            return new KwGirthPoint(KwPlanks.Base(st, j, side), KwPlanks.Normal(st, j, side), girth.Of(s)[j], along,
                j == st.Half.Length - 1 ? 1f : 0f, j == 0 ? 1f : 0f);
        }

        // Strake k's piece of one quad: its outside and inside faces, then its lip (on its lower edge or along the keel) and
        // its cap where it has those edges.
        private static void Board(MeshBuilder b, int k, List<KwGirthPoint> piece)
        {
            if (piece.Count < 3)
                return;
            b.Polygon(piece.ConvertAll(p => p.At + p.Out * Proud(p, k)), piece.ConvertAll(p => KwUv.Strake(true, p.Along, k, p.Girth - k * W)));
            b.Polygon(piece.ConvertAll(p => p.At - p.Out * KwPlanks.Thick), piece.ConvertAll(p => KwUv.Strake(false, p.Along, k, p.Girth - k * W)));
            for (int i = 0; i < piece.Count; i++)
            {
                KwGirthPoint p = piece[i], q = piece[(i + 1) % piece.Count];
                if ((Mathf.Abs(p.Girth - k * W) < OnEdge && Mathf.Abs(q.Girth - k * W) < OnEdge) || (p.Keel >= 1f && q.Keel >= 1f))
                    Lip(b, p, q, k);
                else if (p.Sheer >= 1f && q.Sheer >= 1f)
                    Cap(b, p, q, k);
            }
        }

        // The face under strake k's proud lower edge (or its edge along the keel), from p to q, through the board's
        // thickness and as far out as the board stands there.
        private static void Lip(MeshBuilder b, KwGirthPoint p, KwGirthPoint q, int k) =>
            b.Polygon(new List<Vector3> { p.At - p.Out * KwPlanks.Thick, p.At + p.Out * Proud(p, k), q.At + q.Out * Proud(q, k), q.At - q.Out * KwPlanks.Thick },
                new List<Vector2> { KwUv.Lip(p.Along, k, false), KwUv.Lip(p.Along, k, true), KwUv.Lip(q.Along, k, true), KwUv.Lip(q.Along, k, false) });

        // The gunwale's cap on the sheer, from p to q, over strake k's upper edge where the sheer cuts it.
        private static void Cap(MeshBuilder b, KwGirthPoint p, KwGirthPoint q, int k) =>
            b.Polygon(new List<Vector3> { p.At + p.Out * Proud(p, k), p.At - p.Out * KwPlanks.Thick, q.At - q.Out * KwPlanks.Thick, q.At + q.Out * Proud(q, k) },
                new List<Vector2> { KwUv.Cap(p.Along, k, true), KwUv.Cap(p.Along, k, false), KwUv.Cap(q.Along, k, false), KwUv.Cap(q.Along, k, true) });

        // The girths from lo to hi with the section's own points between, so an end follows the section's corners.
        private static List<float> Marks(float[] round, float lo, float hi)
        {
            List<float> marks = new List<float> { lo };
            foreach (float g in round)
            {
                if (g > lo + OnEdge && g < hi - OnEdge)
                    marks.Add(g);
            }
            marks.Add(hi);
            return marks;
        }

        // Station s's point at a girth on strake k, its way out blended from the section points either side.
        private static KwGirthPoint On(KwGirth girth, int s, float g, float side, int k)
        {
            float[] round = girth.Of(s);
            int j = KwGirth.Segment(round, g);
            float span = round[j + 1] - round[j];
            return KwGirthPoint.Lerp(Corner(girth, s, j, side, k), Corner(girth, s, j + 1, side, k), span < 1e-6f ? 0f : Mathf.Clamp01((g - round[j]) / span));
        }

        // One slice of an end between girths p and q of strake k, from its outside face in through the board, its texture
        // the strake's row across and the board's thickness along.
        private static void EndSlice(MeshBuilder b, KwGirthPoint p, KwGirthPoint q, int k) =>
            b.Polygon(new List<Vector3> { p.At + p.Out * Proud(p, k), q.At + q.Out * Proud(q, k), q.At - q.Out * KwPlanks.Thick, p.At - p.Out * KwPlanks.Thick },
                new List<Vector2>
                {
                    KwUv.Strake(true, p.Along, k, p.Girth - k * W), KwUv.Strake(true, q.Along, k, q.Girth - k * W),
                    KwUv.Strake(true, q.Along + KwPlanks.Thick, k, q.Girth - k * W), KwUv.Strake(true, p.Along + KwPlanks.Thick, k, p.Girth - k * W),
                });
    }

    /// <summary>
    /// A point on the planking's outside face between two stations as a strake is cut (<see cref="KwStrakes"/>): where it is
    /// (kit axes, one side), the way out of the hull there, its measure round the hull (<see cref="KwGirth.Measure"/>), how
    /// far along the strake it lies (<see cref="KwGirth.Along(int, int, Vector3)"/>), 1 on the sheer and 1 on the keel.
    /// </summary>
    public readonly struct KwGirthPoint
    {
        public KwGirthPoint(Vector3 at, Vector3 outward, float girth, float along, float sheer, float keel)
        {
            At = at;
            Out = outward;
            Girth = girth;
            Along = along;
            Sheer = sheer;
            Keel = keel;
        }

        public Vector3 At { get; }
        public Vector3 Out { get; }
        public float Girth { get; }
        public float Along { get; }
        public float Sheer { get; }
        public float Keel { get; }

        /// <summary>The point a share f of the way from a to b, every measure blended, its way out kept a unit.</summary>
        public static KwGirthPoint Lerp(KwGirthPoint a, KwGirthPoint b, float f) => new KwGirthPoint(
            Vector3.LerpUnclamped(a.At, b.At, f), Vector3.LerpUnclamped(a.Out, b.Out, f).normalized, a.Girth + (b.Girth - a.Girth) * f,
            a.Along + (b.Along - a.Along) * f, a.Sheer + (b.Sheer - a.Sheer) * f, a.Keel + (b.Keel - a.Keel) * f);
    }
}
