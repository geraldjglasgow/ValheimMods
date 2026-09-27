using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// A brush stroke's footprint as a plain inside test, built on the Engine's own <see cref="Terrain.Footprint"/>
    /// (same shapes, same rotation convention, same size rules, full weight everywhere inside), so clearing and the
    /// admin terrain command pick exactly the objects and vertices the Engine would change.
    /// </summary>
    public static class AreaShape
    {
        /// <summary>The stroke's footprint with a flat inside (weight 1 up to the rim).</summary>
        public static Terrain.Footprint For(BrushStroke stroke)
        {
            return Terrain.Footprint.Create(stroke.Shape, stroke.Center, stroke.Radius, stroke.Radius2, stroke.Rotation, 1f, true);
        }

        public static bool Contains(Terrain.Footprint print, Vector3 world) => print.Weight(world.x, world.z) > 0f;

        /// <summary>Every whole-metre vertex (world X, Z) inside the stroke's footprint.</summary>
        public static List<Vector2Int> Vertices(BrushStroke stroke)
        {
            Terrain.Footprint print = For(stroke);
            print.Extent(out float halfX, out float halfZ);
            List<Vector2Int> vertices = new List<Vector2Int>();
            int minX = Mathf.FloorToInt(print.CenterX - halfX), maxX = Mathf.CeilToInt(print.CenterX + halfX);
            int minZ = Mathf.FloorToInt(print.CenterZ - halfZ), maxZ = Mathf.CeilToInt(print.CenterZ + halfZ);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (print.Weight(x, z) > 0f)
                        vertices.Add(new Vector2Int(x, z));
                }
            }
            return vertices;
        }
    }

    /// <summary>
    /// The ground a clearing works on: a brush footprint (the Clear and Groundbreaker entries) or a circle (the
    /// Clearing Radius setting and the console commands). <see cref="Reach"/> is a circle around the centre that holds
    /// the whole area, for a quick first test.
    /// </summary>
    public sealed class ClearArea
    {
        private Terrain.Footprint print;
        private bool shaped;
        private float radius;

        public Vector3 Center { get; private set; }

        public float Reach { get; private set; }

        public static ClearArea Circle(Vector3 center, float radius)
        {
            return new ClearArea { Center = center, radius = radius, Reach = radius };
        }

        public static ClearArea Of(BrushStroke stroke)
        {
            Terrain.Footprint print = AreaShape.For(stroke);
            print.Extent(out float halfX, out float halfZ);
            return new ClearArea { Center = stroke.Center, print = print, shaped = true, Reach = Mathf.Sqrt(halfX * halfX + halfZ * halfZ) };
        }

        /// <summary>The area of the Clear and Groundbreaker entries: the Clearing Radius circle when set, else the stroke's footprint.</summary>
        public static ClearArea ForEntry(BrushStroke stroke)
        {
            float setting = ClearingSettings.ClearingRadius.Value;
            return setting > 0f ? Circle(stroke.Center, setting) : Of(stroke);
        }

        public bool Contains(Vector3 world)
        {
            if (shaped)
                return AreaShape.Contains(print, world);
            float dx = world.x - Center.x;
            float dz = world.z - Center.z;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
