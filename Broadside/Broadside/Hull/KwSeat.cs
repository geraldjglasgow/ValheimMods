using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a hull part sits on its ship's keels (<see cref="KwKeelLines"/>), worked out each time it builds and never
    /// saved: how far under its start the level keel line lies in its frame (0, or less where its start rib stands a little
    /// up a keel end's curve), and for a part between two ribs the keel's top under it. A rib's height is over the level
    /// keel line wherever it stands (<see cref="KwRibs.Custom"/>), so a part's top edge at a rib is the level keel line plus
    /// that rib's height, whichever part meets it: parts between ribs of one height lie level at one height, and a part
    /// from a rib to a keel end starts where its neighbour ends. A part between two ribs follows the keel under it, bends
    /// included, so its planking closes on the keel; its strakes are counted from the level keel line, as every part's.
    /// </summary>
    public sealed class KwSeat
    {
        // How far the keel may stand off a straight line between the points kept before another is kept: only points on a
        // straight stretch go.
        private const float Bend = 0.0001f;

        public KwSeat(float level, List<Vector2>? keel)
        {
            Level = level;
            Keel = keel;
        }

        /// <summary>The level keel line's height in the part's frame.</summary>
        public float Level { get; }

        /// <summary>A part between two ribs: the keel's top under it, (along, height) in its frame, from end to end; null for any other part.</summary>
        public List<Vector2>? Keel { get; }

        /// <summary>Whether it changes nothing: the part starts on the level keel line and the keel under it is level.</summary>
        public bool Plain => Mathf.Abs(Level) < 1e-4f && (Keel == null || Keel.TrueForAll(p => Mathf.Abs(p.y) < 1e-4f));

        /// <summary>The keel's top at x along the part, straight between the points kept (0 with none).</summary>
        public float KeelAt(float x)
        {
            if (Keel == null || Keel.Count == 0)
                return 0f;
            if (x <= Keel[0].x)
                return Keel[0].y;
            for (int i = 0; i + 1 < Keel.Count; i++)
            {
                if (x <= Keel[i + 1].x)
                    return Mathf.Lerp(Keel[i].y, Keel[i + 1].y, (x - Keel[i].x) / Mathf.Max(Keel[i + 1].x - Keel[i].x, 1e-5f));
            }
            return Keel[Keel.Count - 1].y;
        }

        /// <summary>
        /// A part between two ribs on this seat: its own stations (a run's two, a taper's seven) and, with bends, one where
        /// the keel under it bends; each section's keel on the keel there and its top edge the level keel line plus the
        /// height its frames give there, every point between moved in proportion. Railings leave the bends out (their posts
        /// stand at stations).
        /// </summary>
        public List<Station> Between(string family, KwFrame a, KwFrame b, float length, bool bends)
        {
            Vector2[] from = KwJoints.Section(family, a), to = KwJoints.Section(family, b);
            bool same = a.Same(b);
            List<Station> stations = new List<Station>();
            foreach (float x in Places(same ? 1 : 6, length, bends))
            {
                Vector2[] half = same ? from : KwJoints.Blend(from, to, KwJoints.Smoothstep(x / length));
                stations.Add(new Station(x, Reseat(half, KeelAt(x), half[half.Length - 1].y + Level)));
            }
            return stations;
        }

        // A run's or taper's even places along it, with the keel's bends between them.
        private List<float> Places(int steps, float length, bool bends)
        {
            List<float> places = new List<float>();
            for (int i = 0; i <= steps; i++)
                places.Add(length * i / steps);
            if (bends && Keel != null)
            {
                foreach (Vector2 p in Keel)
                {
                    if (places.TrueForAll(x => Mathf.Abs(x - p.x) > 0.01f))
                        places.Add(p.x);
                }
            }
            places.Sort();
            return places;
        }

        /// <summary>A section standing on 0 moved to stand on keel with its top edge at sheer, every point in proportion.</summary>
        public static Vector2[] Reseat(Vector2[] half, float keel, float sheer)
        {
            float top = half[half.Length - 1].y, k = half[0].y;
            Vector2[] seated = new Vector2[half.Length];
            for (int i = 0; i < half.Length; i++)
                seated[i] = new Vector2(half[i].x, keel + (half[i].y - k) * (sheer - keel) / Mathf.Max(top - k, 1e-4f));
            return seated;
        }

        /// <summary>Points along a keel thinned to its ends and where it bends (the points on its straight stretches go).</summary>
        public static List<Vector2> Thin(List<Vector2> points)
        {
            if (points.Count <= 2)
                return points;
            List<Vector2> kept = new List<Vector2> { points[0] };
            Keep(points, 0, points.Count - 1, kept);
            kept.Add(points[points.Count - 1]);
            return kept;
        }

        // The points between first and last that stand further than Bend off the line between them, the furthest first.
        private static void Keep(List<Vector2> points, int first, int last, List<Vector2> kept)
        {
            int far = -1;
            float most = Bend;
            for (int i = first + 1; i < last; i++)
            {
                float t = (points[i].x - points[first].x) / Mathf.Max(points[last].x - points[first].x, 1e-5f);
                float off = Mathf.Abs(points[i].y - Mathf.Lerp(points[first].y, points[last].y, t));
                if (off > most)
                    (far, most) = (i, off);
            }
            if (far < 0)
                return;
            Keep(points, first, far, kept);
            kept.Add(points[far]);
            Keep(points, far, last, kept);
        }
    }

    /// <summary>Where a part stands among its ship's keels: the keels, and its start and the way it runs, in their frame.</summary>
    public readonly struct KwSeatAt
    {
        public KwSeatAt(KwKeelLines keels, Vector3 origin, Vector3 forward)
        {
            Keels = keels;
            Origin = origin;
            Forward = forward;
        }

        public KwKeelLines Keels { get; }
        public Vector3 Origin { get; }
        public Vector3 Forward { get; }

        /// <summary>Where a part standing in the world stands among the keels loaded.</summary>
        public static KwSeatAt World(Transform part) => new KwSeatAt(KwKeelLines.World(), part.position, part.forward);

        /// <summary>A part's span (just read from its text) seated here.</summary>
        public KwSpan Seated(KwSpan span)
        {
            span.Seat = Keels.Seat(Origin, Forward, span);
            return span;
        }

        /// <summary>How far up its keel a rib standing here stands (<see cref="KwKeelLines.Climb"/>).</summary>
        public float Climb => Keels.Climb(Origin);
    }
}
