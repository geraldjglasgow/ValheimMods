using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The vertices of a straight slope between two points: the height runs evenly from the ground at the start to the
    /// ground at the end along the line; every vertex within the half width of the line (rounded at both ends) gets
    /// weight 1, and the weight fades to 0 over the falloff beside it so the sides blend into the ground. The paint,
    /// if any, covers the full-weight surface.
    /// </summary>
    public static class SlopeBuilder
    {
        public static VertexSet Build(Vector3 from, Vector3 to, float halfWidth, float falloff, PaintOp paint)
        {
            float startHeight = TerrainRead.GroundHeight(from, from.y);
            float endHeight = TerrainRead.GroundHeight(to, to.y);
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z);
            float edge = halfWidth + falloff;
            VertexSet set = new VertexSet { Mode = VertexMode.Targets };
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - edge), maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + edge);
            int minZ = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - edge), maxZ = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + edge);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    float t = Project(a, b, new Vector2(x, z), out float distance);
                    float weight = distance <= halfWidth ? 1f : 1f - (distance - halfWidth) / falloff;
                    if (weight > 0f)
                        set.Targets.Add(VertexEdits.Target(x, z, Mathf.Lerp(startHeight, endHeight, t), weight, weight >= 1f ? paint : PaintOp.None));
                }
            }
            return set;
        }

        /// <summary>How far along the segment (0..1) the point's nearest spot lies, and the point's distance to it.</summary>
        private static float Project(Vector2 a, Vector2 b, Vector2 p, out float distance)
        {
            Vector2 ab = b - a;
            float length2 = ab.sqrMagnitude;
            float t = length2 > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / length2) : 0f;
            distance = Vector2.Distance(p, a + ab * t);
            return t;
        }
    }
}
