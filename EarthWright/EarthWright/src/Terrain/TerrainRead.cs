using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>One height vertex as this machine sees it.</summary>
    public struct VertexInfo
    {
        public Heightmap Map;

        /// <summary>Null when nobody has edited this heightmap yet.</summary>
        public TerrainComp Comp;

        /// <summary>Vertex coordinates in the heightmap (0..width) and the array index shared by height and paint.</summary>
        public int X;
        public int Y;
        public int Index;

        /// <summary>World position of the vertex, Y = current height.</summary>
        public Vector3 World;

        /// <summary>The world's generated height (absolute).</summary>
        public float Base;

        /// <summary>The height shown now (absolute).</summary>
        public float Current;

        public float LevelDelta;
        public float SmoothDelta;
        public bool HeightModified;
        public bool PaintModified;
        public Color Paint;
    }

    /// <summary>
    /// Read-only access to the terrain as this machine holds it: heightmaps with their generated heights, and the
    /// compilers' edit arrays (replicated from their ZDOs, so every client reads the same values once synced).
    /// </summary>
    public static class TerrainRead
    {
        /// <summary>Heightmap vertices sit on whole world metres (scale 1); this rounds a position to its vertex.</summary>
        public static Vector3Int VertexOf(Vector3 world) => new Vector3Int(Mathf.RoundToInt(world.x), 0, Mathf.RoundToInt(world.z));

        /// <summary>The vertex nearest to the position, false when no loaded heightmap covers it.</summary>
        public static bool TryVertex(Vector3 world, out VertexInfo info)
        {
            info = default;
            Heightmap map = Heightmap.FindHeightmap(world);
            if (map == null || map.m_buildData == null)
                return false;
            map.WorldToVertex(world, out int x, out int y);
            int pitch = map.m_width + 1;
            if (x < 0 || y < 0 || x >= pitch || y >= pitch)
                return false;
            info.Map = map;
            info.X = x;
            info.Y = y;
            info.Index = y * pitch + x;
            FillHeights(ref info);
            return true;
        }

        private static void FillHeights(ref VertexInfo info)
        {
            Heightmap map = info.Map;
            Vector3 origin = map.transform.position;
            // The height before any compiler edit (includes location flattening), the same base the limits measure from.
            info.Base = EngineBaseHeights.TryWorld(map, info.Index, out float preEdit) ? preEdit : map.m_buildData.m_baseHeights[info.Index] + origin.y;
            info.Current = map.GetHeight(info.X, info.Y) + origin.y;
            int half = map.m_width / 2;
            info.World = new Vector3(origin.x + (info.X - half) * map.m_scale, info.Current, origin.z + (info.Y - half) * map.m_scale);
            info.Paint = map.GetPaintMask(info.X, info.Y);
            TerrainComp comp = TerrainComp.FindTerrainCompiler(origin);
            if (comp == null || !comp.m_initialized)
                return;
            info.Comp = comp;
            info.LevelDelta = comp.m_levelDelta[info.Index];
            info.SmoothDelta = comp.m_smoothDelta[info.Index];
            info.HeightModified = comp.m_modifiedHeight[info.Index];
            info.PaintModified = comp.m_modifiedPaint[info.Index];
            info.Paint = comp.m_paintMask[info.Index];
        }

        /// <summary>The ground height shown now at a world position (terrain only, no buildings), or the fallback.</summary>
        public static float GroundHeight(Vector3 world, float fallback)
        {
            return Heightmap.GetHeight(world, out float height) ? height : fallback;
        }

        /// <summary>The world's generated height at a world position (nearest vertex), or the fallback.</summary>
        public static float BaseHeight(Vector3 world, float fallback)
        {
            return TryVertex(world, out VertexInfo info) ? info.Base : fallback;
        }
    }
}
