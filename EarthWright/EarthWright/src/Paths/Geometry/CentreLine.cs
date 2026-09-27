using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The planned centre line of a ramp or road: points closely spaced along its course, each carrying the planned
    /// surface height in Y, with the horizontal distance of each from the start. Pure data with the slope figures the
    /// checks, the preview and the HUD need; it reads no game state.
    /// </summary>
    public sealed class CentreLine
    {
        public readonly List<Vector3> Points = new List<Vector3>();

        /// <summary>Horizontal distance of each point from the first, along the line.</summary>
        public readonly List<float> Distances = new List<float>();

        public int Count => Points.Count;

        /// <summary>Horizontal length in metres.</summary>
        public float Length => Distances.Count > 0 ? Distances[Distances.Count - 1] : 0f;

        /// <summary>End height minus start height.</summary>
        public float Rise => Count > 1 ? Points[Count - 1].y - Points[0].y : 0f;

        public void Add(Vector3 point)
        {
            float distance = 0f;
            if (Points.Count > 0)
                distance = Distances[Distances.Count - 1] + Flat(Points[Points.Count - 1], point);
            Points.Add(point);
            Distances.Add(distance);
        }

        /// <summary>Slope of segment <paramref name="segment"/> (point i to i + 1) in degrees; 0 for a segment without length.</summary>
        public float SlopeDegrees(int segment)
        {
            float run = Distances[segment + 1] - Distances[segment];
            if (run < 0.0001f)
                return 0f;
            return Mathf.Atan(Mathf.Abs(Points[segment + 1].y - Points[segment].y) / run) * Mathf.Rad2Deg;
        }

        /// <summary>The steepest segment in degrees.</summary>
        public float MaxSlope()
        {
            float max = 0f;
            for (int i = 0; i + 1 < Count; i++)
                max = Mathf.Max(max, SlopeDegrees(i));
            return max;
        }

        /// <summary>The slope in degrees averaged over the length (each segment weighted by its length).</summary>
        public float MeanSlope()
        {
            if (Length < 0.0001f)
                return 0f;
            float sum = 0f;
            for (int i = 0; i + 1 < Count; i++)
                sum += SlopeDegrees(i) * (Distances[i + 1] - Distances[i]);
            return sum / Length;
        }

        /// <summary>The horizontal unit vector pointing left of the direction of travel at a point.</summary>
        public Vector3 LeftNormal(int index)
        {
            Vector3 from = Points[Mathf.Max(0, index - 1)];
            Vector3 to = Points[Mathf.Min(Count - 1, index + 1)];
            Vector3 direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
            if (direction.sqrMagnitude < 0.000001f)
                direction = Overall();
            direction.Normalize();
            return new Vector3(-direction.z, 0f, direction.x);
        }

        /// <summary>The horizontal direction from the first point to the last, north when they coincide.</summary>
        public Vector3 Overall()
        {
            if (Count < 2)
                return Vector3.forward;
            Vector3 direction = new Vector3(Points[Count - 1].x - Points[0].x, 0f, Points[Count - 1].z - Points[0].z);
            return direction.sqrMagnitude < 0.000001f ? Vector3.forward : direction.normalized;
        }

        /// <summary>Horizontal distance between two points.</summary>
        public static float Flat(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Signed horizontal distance of <paramref name="point"/> from the line a to b, positive to its left.</summary>
        public static float SideOf(Vector3 a, Vector3 b, Vector3 point)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            float length = Mathf.Sqrt(dx * dx + dz * dz);
            if (length < 0.0001f)
                return 0f;
            return (dx * (point.z - a.z) - dz * (point.x - a.x)) / length;
        }
    }
}
