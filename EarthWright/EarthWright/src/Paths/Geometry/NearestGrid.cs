using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Scratch space for <see cref="LineProjector"/>: for every whole-metre vertex in the box around a centre line, the
    /// nearest segment found so far, as the share along it and the squared distance. One array, reused from plan to
    /// plan (planning runs on the main thread only), so re-planning several times a second makes no garbage.
    /// </summary>
    public sealed class NearestGrid
    {
        /// <summary>The nearest segment found for one vertex; <see cref="Distance2"/> is float.MaxValue while none is.</summary>
        public struct Cell
        {
            public int Segment;
            public float U;
            public float Distance2;
        }

        private static Cell[] shared = new Cell[0];

        public readonly int MinX;
        public readonly int MinZ;
        public readonly int Width;
        public readonly int Depth;

        /// <summary>A grid covering the line's points and <paramref name="margin"/> metres around them, every cell empty.</summary>
        public NearestGrid(CentreLine line, float margin)
        {
            Bounds(line, out float minX, out float maxX, out float minZ, out float maxZ);
            MinX = Mathf.FloorToInt(minX - margin);
            MinZ = Mathf.FloorToInt(minZ - margin);
            Width = Mathf.CeilToInt(maxX + margin) - MinX + 1;
            Depth = Mathf.CeilToInt(maxZ + margin) - MinZ + 1;
            int size = Width * Depth;
            if (shared.Length < size)
                shared = new Cell[size];
            for (int i = 0; i < size; i++)
                shared[i].Distance2 = float.MaxValue;
        }

        public Cell[] Cells => shared;

        public int Size => Width * Depth;

        /// <summary>Offers segment a to b to every vertex within <paramref name="reach"/> metres of it.</summary>
        public void Scan(Vector3 a, Vector3 b, int segment, float reach)
        {
            int x0 = Mathf.Max(MinX, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - reach));
            int x1 = Mathf.Min(MinX + Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + reach));
            int z0 = Mathf.Max(MinZ, Mathf.FloorToInt(Mathf.Min(a.z, b.z) - reach));
            int z1 = Mathf.Min(MinZ + Depth - 1, Mathf.CeilToInt(Mathf.Max(a.z, b.z) + reach));
            float reach2 = reach * reach;
            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                    Offer(x, z, segment, a, b, reach2);
            }
        }

        /// <summary>Keeps the segment as the vertex's nearest when it is closer than any offered before.</summary>
        private void Offer(int x, int z, int segment, Vector3 a, Vector3 b, float reach2)
        {
            float u = LineProjector.ClosestShare(a, b, x, z);
            float dx = a.x + (b.x - a.x) * u - x;
            float dz = a.z + (b.z - a.z) * u - z;
            float distance2 = dx * dx + dz * dz;
            int index = (z - MinZ) * Width + (x - MinX);
            if (distance2 > reach2 || shared[index].Distance2 <= distance2)
                return;
            shared[index] = new Cell { Segment = segment, U = u, Distance2 = distance2 };
        }

        private static void Bounds(CentreLine line, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = maxX = line.Points[0].x;
            minZ = maxZ = line.Points[0].z;
            foreach (Vector3 point in line.Points)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }
        }
    }
}
