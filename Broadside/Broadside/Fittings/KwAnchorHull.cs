using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planking part as an anchor's cable sees it (<see cref="KwAnchorShip"/>): its stations' outer outline (the planks'
    /// outside, their laps <see cref="KwPlanks.Lap"/> proud of it) and inner one (<see cref="KwPlanks.Inside"/>) along it, its
    /// sheer (the top edge), its flat ends (a transom), its openings and its tier, in its own kit frame (x along its
    /// stations, y across, its left +, z up from the keel line). On deck a cable must stay inside its inner face below the
    /// sheer; overboard, outside its outer face (a flat end's transom board too). Over the sheer it is free of it.
    /// </summary>
    public sealed class KwAnchorHull : KwAnchorSolid
    {
        // How far under the keel line the hull is counted (its keel and garboard), and the transom board's thickness.
        private const float Under = 0.4f, Board = KwPlanks.Thick;

        private readonly List<Station> stations;
        private readonly Vector2[][] inside;
        private readonly bool flatFore, flatAft;

        public KwAnchorHull(KwBpPiece piece, KwAnchorPose pose, KwSpan span) : base("the hull's side")
        {
            (Piece, Pose, stations) = (piece, pose, span.Stations());
            inside = stations.ConvertAll(s => KwPlanks.Inside(s.Half)).ToArray();
            flatFore = KwSpan.IsTransom(span.End) || KwSpanKeels.Flat(span.End, true);
            flatAft = KwSpanKeels.Flat(span.End, false);
            Openings = KwOpeningText.Read(span.Openings);
            Round(pose, new Vector3(From, -Widest(stations), Lowest(stations) - Under), new Vector3(To, Widest(stations) + KwPlanks.Lap, Highest(stations)));
        }

        /// <summary>The stations' widest half width, lowest point and highest sheer.</summary>
        public static float Widest(List<Station> stations) => stations.Max(s => s.Half.Max(p => p.x));

        public static float Lowest(List<Station> stations) => stations.Min(s => s.Half.Min(p => p.y));

        public static float Highest(List<Station> stations) => stations.Max(s => s.Half.Max(p => p.y));

        public KwBpPiece Piece { get; }

        /// <summary>Its stations' lowest point (its kit z): the keel line's deepest.</summary>
        public float Bottom => Lowest(stations);
        public KwAnchorPose Pose { get; }
        public List<KwOpening> Openings { get; }
        public float From => stations[0].X;
        public float To => stations[stations.Count - 1].X;

        /// <summary>Whether its end at x (the start, or the far end) is flat: a transom board closes it.</summary>
        public bool Flat(bool far) => far ? flatFore : flatAft;

        /// <summary>The end station at its start or far end.</summary>
        public Station End(bool far) => stations[far ? stations.Count - 1 : 0];

        /// <summary>The sheer at x along it: its half width and its height.</summary>
        public Vector2 Sheer(float x) => KwRailing.EdgeAt(stations, x);

        /// <summary>How fast the sheer's half width changes along it at x (per metre).</summary>
        public float Slope(float x)
        {
            float a = Mathf.Max(From, x - 0.05f), c = Mathf.Min(To, x + 0.05f);
            return c - a < 1e-4f ? 0f : (Sheer(c).x - Sheer(a).x) / (c - a);
        }

        /// <summary>How far out its outside is at x along and height z (the laps included); 0 under its keel line.</summary>
        public float Outside(float x, float z) => Width(x, z, false) + KwPlanks.Lap;

        /// <summary>How far out its inside face is at x along and height z.</summary>
        public float Inside(float x, float z) => Width(x, z, true);

        // The outline's half width at x and z, between the stations either side.
        private float Width(float x, float z, bool inner)
        {
            int i = 0;
            while (i + 2 < stations.Count && x > stations[i + 1].X)
                i++;
            if (stations.Count == 1)
                return KwShapes.WidthAt(inner ? inside[0] : stations[0].Half, z);
            float run = stations[i + 1].X - stations[i].X, t = run < 1e-4f ? 0f : Mathf.Clamp01((x - stations[i].X) / run);
            float a = KwShapes.WidthAt(inner ? inside[i] : stations[i].Half, z), c = KwShapes.WidthAt(inner ? inside[i + 1] : stations[i + 1].Half, z);
            return Mathf.Lerp(a, c, t);
        }

        // The keel line's height under x: the stations' lowest points, between the two either side.
        private float Keel(float x)
        {
            int i = 0;
            while (i + 2 < stations.Count && x > stations[i + 1].X)
                i++;
            float run = stations.Count > 1 ? stations[i + 1].X - stations[i].X : 0f;
            float t = run < 1e-4f ? 0f : Mathf.Clamp01((x - stations[i].X) / run);
            return stations.Count > 1 ? Mathf.Lerp(stations[i].Half[0].y, stations[i + 1].Half[0].y, t) : stations[0].Half[0].y;
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = Pose.Local(p);
            bool past = k.x < From || k.x > To;
            if ((k.x < From && (!flatAft || k.x < From - r)) || (k.x > To && (!flatFore || k.x > To + r)))
                return false;
            float x = Mathf.Clamp(k.x, From, To);
            if (k.z > Sheer(x).y + r || k.z < Keel(x) - Under)
                return false;
            float across = Mathf.Abs(k.y);
            if (outboard || past)
                return across < Outside(x, k.z) + r;
            return across > Inside(x, k.z) - r || InBoard(k.x, r);
        }

        // Inboard, within a flat end's transom board.
        private bool InBoard(float x, float r) => (flatFore && x > To - Board - r) || (flatAft && x < From + Board + r);
    }
}
