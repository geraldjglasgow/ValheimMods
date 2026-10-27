using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The water mask's shape (<see cref="KwShipWaterMask"/>), plain arithmetic in the ship's frame. Each of the hull's
    /// cross-sections is drawn in <see cref="Inset"/> all round, square to its edges, past the planking's inside face
    /// (<see cref="KwPlanks.Thick"/>; <see cref="Inner"/>, once; a section left too small, at the stems, is dropped), and
    /// its first and last kept in from the hull's ends as far, so the mask lies inside the hull and never over its outside.
    /// Each time the sea moves, every section is cut to a thin band round the sea's surface across it (<see cref="Band"/>:
    /// from <see cref="Below"/> under the sea's line to <see cref="Above"/> over it, never under the water in the hold, so
    /// it hides the sea only where the hold is dry), resampled to <see cref="Round"/> points from where a line out to
    /// starboard from its middle meets its edge, and lofted from each section to the next along a run of them
    /// (<see cref="Loft"/>), each run's ends capped, every face both ways round.
    /// </summary>
    public static class KwMaskShape
    {
        /// <summary>Metres: how far in from the hull's outside, how far under and over the sea's line, how far over the water inside.</summary>
        public const float Inset = 0.12f, Below = 0.2f, Above = 0.25f, OverWater = 0.03f;

        public const int Round = 32;
        private const float Least = 0.01f;
        private static readonly List<Vector2> Work = new List<Vector2>(), Cut = new List<Vector2>(), Path = new List<Vector2>();

        /// <summary>A section's outline drawn in by <see cref="Inset"/> from every edge (counter-clockwise); null when too little is left.</summary>
        public static Vector2[]? Inner(Vector2[] shape)
        {
            List<Vector2> ring = Ordered(shape);
            List<Vector2> inner = new List<Vector2>(ring);
            for (int i = 0; i < ring.Count && inner.Count >= 3; i++)
            {
                Vector2 p = ring[i], q = ring[(i + 1) % ring.Count], edge = q - p;
                if (edge.sqrMagnitude < 1e-8f)
                    continue;
                Vector2 n = new Vector2(-edge.y, edge.x).normalized; // inward: the inside lies to the left going round
                Clip(inner, n, Vector2.Dot(n, p) + Inset);
            }
            return inner.Count >= 3 && Area(inner) >= Least ? inner.ToArray() : null;
        }

        /// <summary>
        /// A section's inner outline cut to the band round the sea's line <paramref name="seaUp"/>·p = <paramref name="sea"/>
        /// (a unit normal pointing up), and, with water in the hold (<paramref name="wet"/>), over its surface
        /// <paramref name="waterUp"/>·p = <paramref name="water"/>; resampled into <paramref name="into"/>. False when nothing is left.
        /// </summary>
        public static bool Band(Vector2[] inner, Vector2 seaUp, float sea, bool wet, Vector2 waterUp, float water, Vector2[] into)
        {
            Work.Clear();
            Work.AddRange(inner);
            Clip(Work, seaUp, sea - Below);
            Clip(Work, -seaUp, -(sea + Above));
            if (wet)
                Clip(Work, waterUp, water + OverWater * waterUp.magnitude);
            if (Work.Count < 3 || Area(Work) < Least)
                return false;
            Resample(Work, into);
            return true;
        }

        /// <summary>
        /// A run of bands (each <see cref="Round"/> points) at their places along the ship, each joined to the next and both
        /// ends capped, into the lists; every face both ways round.
        /// </summary>
        public static void Loft(List<Vector2[]> bands, List<float> places, List<Vector3> points, List<int> triangles)
        {
            if (bands.Count < 2)
                return;
            int first = points.Count;
            for (int s = 0; s < bands.Count; s++)
            {
                foreach (Vector2 p in bands[s])
                    points.Add(new Vector3(p.x, p.y, places[s]));
            }
            for (int s = 0; s + 1 < bands.Count; s++)
            {
                for (int i = 0; i < Round; i++)
                {
                    int a = first + s * Round + i, b = first + s * Round + (i + 1) % Round;
                    Both(triangles, a, b, b + Round);
                    Both(triangles, a, b + Round, a + Round);
                }
            }
            Cap(points, triangles, first, places[0]);
            Cap(points, triangles, first + (bands.Count - 1) * Round, places[places.Count - 1]);
        }

        /// <summary>One pass of clipping a closed outline, in place: the part where n·p &gt;= c is kept.</summary>
        public static void Clip(List<Vector2> shape, Vector2 n, float c)
        {
            Cut.Clear();
            for (int i = 0; i < shape.Count; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Count];
                float dp = Vector2.Dot(n, p) - c, dq = Vector2.Dot(n, q) - c;
                if (dp >= 0f)
                    Cut.Add(p);
                if ((dp >= 0f) != (dq >= 0f))
                    Cut.Add(Vector2.Lerp(p, q, dp / (dp - dq)));
            }
            shape.Clear();
            shape.AddRange(Cut);
        }

        /// <summary>A closed outline's area, positive counter-clockwise (x across, y up).</summary>
        public static float Area(List<Vector2> shape)
        {
            float twice = 0f;
            for (int i = 0; i < shape.Count; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Count];
                twice += p.x * q.y - q.x * p.y;
            }
            return twice / 2f;
        }

        // The outline counter-clockwise, its repeated points dropped.
        private static List<Vector2> Ordered(Vector2[] shape)
        {
            List<Vector2> ring = new List<Vector2>();
            foreach (Vector2 p in shape)
            {
                if (ring.Count == 0 || (p - ring[ring.Count - 1]).sqrMagnitude > 1e-8f)
                    ring.Add(p);
            }
            if (ring.Count > 1 && (ring[0] - ring[ring.Count - 1]).sqrMagnitude <= 1e-8f)
                ring.RemoveAt(ring.Count - 1);
            if (Area(ring) < 0f)
                ring.Reverse();
            return ring;
        }

        /// <summary>
        /// A convex outline (counter-clockwise) as <see cref="Round"/> points evenly round its edge, from where a line out
        /// to starboard from its middle meets it, so the bands of neighbouring sections pair point for point.
        /// </summary>
        public static void Resample(List<Vector2> shape, Vector2[] into)
        {
            int start = Start(shape, out Vector2 from);
            Path.Clear();
            Path.Add(from);
            for (int k = 1; k <= shape.Count; k++)
                Path.Add(shape[(start + k) % shape.Count]);
            Path.Add(from);
            float total = 0f;
            for (int k = 0; k + 1 < Path.Count; k++)
                total += Vector2.Distance(Path[k], Path[k + 1]);
            float run = 0f;
            for (int i = 0, k = 0; i < Round; i++)
            {
                float want = total * i / Round;
                while (k + 2 < Path.Count && run + Vector2.Distance(Path[k], Path[k + 1]) < want)
                    run += Vector2.Distance(Path[k], Path[++k]);
                float span = Vector2.Distance(Path[k], Path[k + 1]);
                into[i] = span > 1e-6f ? Vector2.Lerp(Path[k], Path[k + 1], (want - run) / span) : Path[k];
            }
        }

        // Where a line from the outline's middle out to starboard (+x) meets its edge: the edge's first corner and the point.
        private static int Start(List<Vector2> shape, out Vector2 at)
        {
            Vector2 middle = Vector2.zero;
            foreach (Vector2 p in shape)
                middle += p / shape.Count;
            (int best, float far) = (0, float.MinValue);
            at = shape[0];
            for (int i = 0; i < shape.Count; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Count];
                if ((p.y > middle.y) == (q.y > middle.y))
                    continue;
                float x = p.x + (q.x - p.x) * (middle.y - p.y) / (q.y - p.y);
                if (x > far)
                    (best, far, at) = (i, x, new Vector2(x, middle.y));
            }
            return best;
        }

        // A band's end closed: a fan from its middle, both ways round.
        private static void Cap(List<Vector3> points, List<int> triangles, int first, float z)
        {
            Vector3 middle = Vector3.zero;
            for (int i = 0; i < Round; i++)
                middle += points[first + i] / Round;
            int centre = points.Count;
            points.Add(new Vector3(middle.x, middle.y, z));
            for (int i = 0; i < Round; i++)
                Both(triangles, centre, first + i, first + (i + 1) % Round);
        }

        private static void Both(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }
    }
}
