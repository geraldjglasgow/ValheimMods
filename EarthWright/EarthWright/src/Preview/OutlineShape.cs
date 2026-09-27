using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The outline loops of a <see cref="FootprintSpec"/> in the horizontal plane: the rim, and for a ring or frame with
    /// a hole the inner edge, both with the same number of points (the volume's lid is a band between them). Points are
    /// placed with the engine's own axes (<see cref="Terrain.Footprint.LocalToWorld"/>), so the outline turns exactly as
    /// the edit does.
    /// </summary>
    internal static class OutlineShape
    {
        private const float Spacing = 0.5f;
        private const float MinHole = 0.05f;

        public static List<Vector2[]> Loops(FootprintSpec spec)
        {
            List<Vector2[]> loops = new List<Vector2[]>();
            float hole = spec.Hole;
            if (spec.Cornered)
            {
                int perSide = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.Max(spec.HalfU, spec.HalfV) / Spacing), 4, 64);
                loops.Add(Box(spec, spec.HalfU, spec.HalfV, perSide));
                if (hole > MinHole)
                    loops.Add(Box(spec, hole, hole, perSide));
                return loops;
            }
            int count = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * spec.Print.Outer / Spacing), 32, 256);
            loops.Add(Circle(spec, spec.Print.Outer, count));
            if (hole > MinHole)
                loops.Add(Circle(spec, hole, count));
            return loops;
        }

        private static Vector2[] Circle(FootprintSpec spec, float radius, int count)
        {
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float a = 2f * Mathf.PI * i / count;
                points[i] = World(spec, Mathf.Sin(a) * radius, Mathf.Cos(a) * radius);
            }
            return points;
        }

        /// <summary>A turned box sampled every side into <paramref name="perSide"/> steps, starting at a corner.</summary>
        private static Vector2[] Box(FootprintSpec spec, float halfU, float halfV, int perSide)
        {
            Vector2[] corners = { new Vector2(-halfU, -halfV), new Vector2(halfU, -halfV), new Vector2(halfU, halfV), new Vector2(-halfU, halfV) };
            Vector2[] points = new Vector2[perSide * 4];
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < perSide; i++)
                {
                    Vector2 local = Vector2.Lerp(corners[side], corners[(side + 1) % 4], (float)i / perSide);
                    points[side * perSide + i] = World(spec, local.x, local.y);
                }
            }
            return points;
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
