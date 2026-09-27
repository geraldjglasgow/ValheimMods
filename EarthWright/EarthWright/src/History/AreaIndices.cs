using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Which array indices of one heightmap an edit may touch, so the undo snapshot holds them. A brush stroke covers
    /// the square around its area (<see cref="TerrainEdit.GetArea"/>: the reach with the paint radius and the nearest
    /// vertex, plus the gentle-slopes ring); a ramp or road covers its target vertices (height and the paint cell of the
    /// same index) and the gentle-slopes ring around each (<see cref="Engine.ExtraReach"/>). One extra vertex all round
    /// covers the paint cells, which sit half a metre off the height vertices. The square is generous on purpose; the
    /// <see cref="Pruner"/> drops what the edit did not change.
    /// </summary>
    internal static class AreaIndices
    {
        public static List<int> For(TerrainEdit edit, Heightmap map)
        {
            if (edit.Kind == EditKind.Vertices && edit.Vertices != null && edit.Vertices.Mode == VertexMode.Targets)
            {
                float extra = Mathf.Max(0f, Safe.Call("EarthWright undo reach", () => Engine.ExtraReach(edit), 0f));
                return AroundTargets(edit.Vertices, map, extra);
            }
            edit.GetArea(out Vector3 center, out float radius);
            return Box(map, center.x - radius, center.z - radius, center.x + radius, center.z + radius);
        }

        /// <summary>Every index whose vertex lies in the world rectangle, one vertex wider all round.</summary>
        public static List<int> Box(Heightmap map, float minX, float minZ, float maxX, float maxZ)
        {
            List<int> indices = new List<int>();
            Vector3 origin = map.transform.position;
            Span(map, minX - origin.x, maxX - origin.x, out int x0, out int x1);
            Span(map, minZ - origin.z, maxZ - origin.z, out int y0, out int y1);
            int pitch = RawAccess.Pitch(map);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    indices.Add(y * pitch + x);
            }
            return indices;
        }

        /// <summary>Every index whose vertex lies within the radius of the centre (XZ).</summary>
        public static List<int> Circle(Heightmap map, Vector3 center, float radius)
        {
            List<int> indices = new List<int>();
            int pitch = RawAccess.Pitch(map);
            foreach (int index in Box(map, center.x - radius, center.z - radius, center.x + radius, center.z + radius))
            {
                Vector3 world = VertexWorld(map, index % pitch, index / pitch);
                float dx = world.x - center.x;
                float dz = world.z - center.z;
                if (dx * dx + dz * dz <= (radius + 0.5f) * (radius + 0.5f))
                    indices.Add(index);
            }
            return indices;
        }

        private static List<int> AroundTargets(VertexSet set, Heightmap map, float extra)
        {
            HashSet<int> indices = new HashSet<int>();
            int margin = 1 + Mathf.CeilToInt(extra / map.m_scale);
            int pitch = RawAccess.Pitch(map);
            foreach (TargetVertex target in set.Targets)
            {
                map.WorldToVertex(new Vector3(target.X, 0f, target.Z), out int x, out int y);
                for (int dy = -margin; dy <= margin; dy++)
                {
                    for (int dx = -margin; dx <= margin; dx++)
                    {
                        if (x + dx >= 0 && y + dy >= 0 && x + dx < pitch && y + dy < pitch)
                            indices.Add((y + dy) * pitch + x + dx);
                    }
                }
            }
            return new List<int>(indices);
        }

        /// <summary>The vertex columns (or rows) covering an offset range from the heightmap centre, clamped to the heightmap.</summary>
        private static void Span(Heightmap map, float from, float to, out int first, out int last)
        {
            int half = map.m_width / 2;
            first = Mathf.Clamp(Mathf.FloorToInt(from / map.m_scale) + half - 1, 0, map.m_width);
            last = Mathf.Clamp(Mathf.CeilToInt(to / map.m_scale) + half + 1, 0, map.m_width);
        }

        public static Vector3 VertexWorld(Heightmap map, int x, int y)
        {
            Vector3 origin = map.transform.position;
            int half = map.m_width / 2;
            return new Vector3(origin.x + (x - half) * map.m_scale, 0f, origin.z + (y - half) * map.m_scale);
        }
    }
}
