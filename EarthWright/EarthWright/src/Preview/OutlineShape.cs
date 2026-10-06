using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The outline loops of a <see cref="FootprintSpec"/> in the horizontal plane: the rim, and for a ring or frame with
    /// a hole the inner edge, both with the same number of points (the volume's lid is a band between them). Points are
    /// placed with the engine's own axes (<see cref="Terrain.Footprint.LocalToWorld"/>), so the outline turns exactly as
    /// the edit does. The loops are written into the caller's arrays, which are kept while the point count stays.
    /// </summary>
    internal static class OutlineShape
    {
        private const float Spacing = 0.5f;
        private const float MinHole = 0.05f;

        private static readonly Vector2[] corners = new Vector2[4];

        /// <summary>Fills <paramref name="loops"/> with the loops (outer first), reusing its arrays where they fit.</summary>
        public static void Loops(FootprintSpec spec, List<Vector2[]> loops)
        {
            float hole = spec.Hole;
            int count = 0;
            if (spec.Cornered)
            {
                int perSide = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.Max(spec.HalfU, spec.HalfV) / Spacing), 4, 64);
                Box(spec, spec.HalfU, spec.HalfV, perSide, Slot(loops, count++, perSide * 4));
                if (hole > MinHole)
                    Box(spec, hole, hole, perSide, Slot(loops, count++, perSide * 4));
            }
            else
            {
                int points = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * spec.Print.Outer / Spacing), 32, 256);
                Circle(spec, spec.Print.Outer, Slot(loops, count++, points));
                if (hole > MinHole)
                    Circle(spec, hole, Slot(loops, count++, points));
            }
            loops.RemoveRange(count, loops.Count - count);
        }

        /// <summary>The array for loop <paramref name="index"/>: the one already there when it has this length, else a new one.</summary>
        public static T[] Slot<T>(List<T[]> loops, int index, int length)
        {
            if (index < loops.Count)
            {
                if (loops[index].Length != length)
                    loops[index] = new T[length];
                return loops[index];
            }
            T[] array = new T[length];
            loops.Add(array);
            return array;
        }

        private static void Circle(FootprintSpec spec, float radius, Vector2[] points)
        {
            int count = points.Length;
            for (int i = 0; i < count; i++)
            {
                float a = 2f * Mathf.PI * i / count;
                points[i] = World(spec, Mathf.Sin(a) * radius, Mathf.Cos(a) * radius);
            }
        }

        /// <summary>A turned box sampled every side into <paramref name="perSide"/> steps, starting at a corner.</summary>
        private static void Box(FootprintSpec spec, float halfU, float halfV, int perSide, Vector2[] points)
        {
            corners[0] = new Vector2(-halfU, -halfV);
            corners[1] = new Vector2(halfU, -halfV);
            corners[2] = new Vector2(halfU, halfV);
            corners[3] = new Vector2(-halfU, halfV);
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < perSide; i++)
                {
                    Vector2 local = Vector2.Lerp(corners[side], corners[(side + 1) % 4], (float)i / perSide);
                    points[side * perSide + i] = World(spec, local.x, local.y);
                }
            }
        }

        private static Vector2 World(FootprintSpec spec, float u, float v)
        {
            Vector3 world = Terrain.Footprint.LocalToWorld(spec.Center, spec.Rotation, u, v);
            return new Vector2(world.x, world.z);
        }

        /// <summary>
        /// A local offset (x along the turned X axis, y along the turned Z axis) as a world offset, for a turn of
        /// <paramref name="degrees"/> about the vertical axis; the same convention as the engine's LocalToWorld.
        /// </summary>
        public static Vector2 Turn(Vector2 local, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r);
            float sin = Mathf.Sin(r);
            return new Vector2(local.x * cos + local.y * sin, -local.x * sin + local.y * cos);
        }
    }
}
