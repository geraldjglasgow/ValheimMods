using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>A whole-metre terrain vertex placed relative to a centre line.</summary>
    public struct VertexSample
    {
        /// <summary>World vertex coordinates, whole metres.</summary>
        public int X;
        public int Z;

        /// <summary>The planned surface height at the vertex's position along the line.</summary>
        public float Height;

        /// <summary>Signed horizontal distance from the line, positive to the left of the direction of travel.</summary>
        public float Lateral;

        /// <summary>Metres before the start or past the end of the line; 0 alongside it.</summary>
        public float Outside;
    }

    /// <summary>
    /// Finds the whole-metre terrain vertices near a centre line and places each relative to its nearest point on the
    /// line. Terrain vertices sit on whole world metres, so these are exactly the points an edit can change. Each
    /// segment offers itself only to the vertices in its own small box (<see cref="NearestGrid"/>). Pure functions.
    /// </summary>
    public static class LineProjector
    {
        /// <summary>
        /// Every vertex within <paramref name="reach"/> metres of the line, and near the ends within reach plus
        /// <paramref name="endReach"/>, each with its nearest point's height, its side distance and how far it lies past the ends.
        /// </summary>
        public static List<VertexSample> Sample(CentreLine line, float reach, float endReach)
        {
            List<VertexSample> samples = new List<VertexSample>();
            if (line.Count < 2)
                return samples;
            NearestGrid grid = new NearestGrid(line, reach + endReach);
            int last = line.Count - 2;
            for (int i = 0; i <= last; i++)
                grid.Scan(line.Points[i], line.Points[i + 1], i, i == 0 || i == last ? reach + endReach : reach);
            NearestGrid.Cell[] cells = grid.Cells;
            for (int index = 0; index < grid.Size; index++)
            {
                if (cells[index].Distance2 < float.MaxValue)
                    samples.Add(Describe(line, grid.MinX + index % grid.Width, grid.MinZ + index / grid.Width, cells[index]));
            }
            return samples;
        }

        /// <summary>Share (0..1) of the segment a to b at the point nearest to (x, z), horizontally.</summary>
        public static float ClosestShare(Vector3 a, Vector3 b, float x, float z)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            float length2 = dx * dx + dz * dz;
            if (length2 < 0.000001f)
                return 0f;
            return Mathf.Clamp01(((x - a.x) * dx + (z - a.z) * dz) / length2);
        }

        private static VertexSample Describe(CentreLine line, int x, int z, NearestGrid.Cell nearest)
        {
            Vector3 a = line.Points[nearest.Segment];
            Vector3 b = line.Points[nearest.Segment + 1];
            Vector3 direction = Direction(line, a, b);
            float vx = x - a.x;
            float vz = z - a.z;
            float along = vx * direction.x + vz * direction.z;
            float side = direction.x * vz - direction.z * vx;
            VertexSample sample = new VertexSample { X = x, Z = z, Height = Mathf.Lerp(a.y, b.y, nearest.U), Lateral = side };
            float segmentLength = CentreLine.Flat(a, b);
            if (nearest.Segment == 0 && along < 0f)
                sample.Outside = -along;
            else if (nearest.Segment == line.Count - 2 && along > segmentLength)
                sample.Outside = along - segmentLength;
            else if (nearest.U <= 0f || nearest.U >= 1f)
                sample.Lateral = Mathf.Sign(side) * Mathf.Sqrt(nearest.Distance2); // round the outer side of a bend
            return sample;
        }

        /// <summary>The segment's horizontal unit direction, or the whole line's when the segment has no length.</summary>
        private static Vector3 Direction(CentreLine line, Vector3 a, Vector3 b)
        {
            Vector3 direction = new Vector3(b.x - a.x, 0f, b.z - a.z);
            return direction.sqrMagnitude < 0.000001f ? line.Overall() : direction.normalized;
        }
    }
}
