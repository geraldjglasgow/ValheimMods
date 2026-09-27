using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Turns the points of a ramp or road into its planned centre line: a ramp is straight from its start to its end
    /// with the height following the profile; a road follows the curve through its waypoints. Pure functions.
    /// </summary>
    public static class LineBuilder
    {
        /// <summary>Spacing of the centre line's points in metres; half the terrain's vertex spacing.</summary>
        public const float Step = 0.5f;

        /// <summary>A straight ramp from <paramref name="start"/> to <paramref name="end"/> (heights in Y).</summary>
        public static CentreLine Ramp(Vector3 start, Vector3 end, RampProfile profile, float joinLength)
        {
            CentreLine line = new CentreLine();
            float length = CentreLine.Flat(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / Step));
            float joinShare = length > 0.0001f ? joinLength / length : 0f;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y = start.y + (end.y - start.y) * ProfileCurve.Fraction(profile, t, joinShare);
                line.Add(point);
            }
            return line;
        }

        /// <summary>A road through its waypoints (heights in Y).</summary>
        public static CentreLine Road(IList<Vector3> waypoints)
        {
            CentreLine line = new CentreLine();
            foreach (Vector3 point in RoadCurve.Sample(waypoints, Step))
                line.Add(point);
            return line;
        }
    }
}
