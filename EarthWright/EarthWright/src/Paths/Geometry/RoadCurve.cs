using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The smooth curve a road follows through its waypoints: a centripetal Catmull-Rom spline, which passes through
    /// every waypoint and makes no loops or cusps however unevenly they are spaced. Heights ride along the same
    /// curve but stay between the heights of the two waypoints of each span, so a road never climbs above or dips
    /// below its waypoints. Pure functions.
    /// </summary>
    public static class RoadCurve
    {
        /// <summary>Samples per metre of chord are raised by this share, since a bending span is longer than its chord.</summary>
        private const float BendAllowance = 1.25f;

        /// <summary>Points along the curve, about <paramref name="step"/> metres apart, from the first waypoint to the last.</summary>
        public static List<Vector3> Sample(IList<Vector3> waypoints, float step)
        {
            List<Vector3> result = new List<Vector3>();
            if (waypoints.Count < 2)
            {
                result.AddRange(waypoints);
                return result;
            }
            for (int i = 0; i + 1 < waypoints.Count; i++)
                SampleSpan(waypoints, i, step, result);
            return result;
        }

        private static void SampleSpan(IList<Vector3> w, int span, float step, List<Vector3> result)
        {
            Vector3 p1 = w[span];
            Vector3 p2 = w[span + 1];
            Vector3 p0 = span > 0 ? w[span - 1] : 2f * p1 - p2;
            Vector3 p3 = span + 2 < w.Count ? w[span + 2] : 2f * p2 - p1;
            int steps = Mathf.Max(1, Mathf.CeilToInt(CentreLine.Flat(p1, p2) * BendAllowance / Mathf.Max(0.05f, step)));
            float low = Mathf.Min(p1.y, p2.y);
            float high = Mathf.Max(p1.y, p2.y);
            for (int k = span == 0 ? 0 : 1; k <= steps; k++)
            {
                Vector3 point = Point(p0, p1, p2, p3, (float)k / steps);
                point.y = Mathf.Clamp(point.y, low, high);
                result.Add(point);
            }
        }

        /// <summary>The point at share <paramref name="u"/> of the span from p1 to p2 (Barry and Goldman's pyramid form).</summary>
        public static Vector3 Point(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            float t1 = Knot(p0, p1);
            float t2 = t1 + Knot(p1, p2);
            float t3 = t2 + Knot(p2, p3);
            float t = Mathf.Lerp(t1, t2, u);
            Vector3 a1 = Blend(p0, p1, 0f, t1, t);
            Vector3 a2 = Blend(p1, p2, t1, t2, t);
            Vector3 a3 = Blend(p2, p3, t2, t3, t);
            Vector3 b1 = Blend(a1, a2, 0f, t2, t);
            Vector3 b2 = Blend(a2, a3, t1, t3, t);
            return Blend(b1, b2, t1, t2, t);
        }

        /// <summary>Centripetal knot spacing: the square root of the horizontal distance, never zero.</summary>
        private static float Knot(Vector3 a, Vector3 b) => Mathf.Max(Mathf.Sqrt(CentreLine.Flat(a, b)), 0.001f);

        private static Vector3 Blend(Vector3 a, Vector3 b, float ta, float tb, float t)
        {
            float span = tb - ta;
            if (span < 0.000001f)
                return a;
            return (tb - t) / span * a + (t - ta) / span * b;
        }
    }
}
