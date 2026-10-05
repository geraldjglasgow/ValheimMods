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

        /// <summary>
        /// The paint: the compiler's when the heightmap has one, else the heightmap's own texture. Read when asked, since
        /// a texture read is the dearest part of a vertex and the loops over many vertices never need it.
        /// </summary>
        public Color Paint
        {
            get
            {
                if (Comp != null)
                    return Comp.m_paintMask[Index];
                return Map != null ? Map.GetPaintMask(X, Y) : default;
            }
        }
    }

    /// <summary>
    /// Read-only access to the terrain as this machine holds it: heightmaps with their generated heights, and the
    /// compilers' edit arrays (replicated from their ZDOs, so every client reads the same values once synced). Loops over
    /// many vertices read through a <see cref="VertexSampler"/> instead, which finds each heightmap and compiler once.
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
            return TryRead(map, CompilerOf(map), map.transform.position, world, out info);
        }

        /// <summary>The heightmap's compiler as the game finds it (null when nobody has edited it), initialized or not.</summary>
        internal static TerrainComp CompilerOf(Heightmap map) => TerrainComp.FindTerrainCompiler(map.transform.position);

        /// <summary>
        /// The vertex of <paramref name="map"/> nearest to the position, given the heightmap's compiler
        /// (<see cref="CompilerOf"/>) and position; false when the heightmap is not built or does not hold the vertex.
        /// </summary>
        internal static bool TryRead(Heightmap map, TerrainComp comp, Vector3 origin, Vector3 world, out VertexInfo info)
        {
            info = default;
            if (map.m_buildData == null)
                return false;
            map.WorldToVertex(world, out int x, out int y);
            int pitch = map.m_width + 1;
            if (x < 0 || y < 0 || x >= pitch || y >= pitch)
                return false;
            info.Map = map;
            info.X = x;
            info.Y = y;
            info.Index = y * pitch + x;
            FillHeights(ref info, comp, origin);
            if (comp != null && comp.m_initialized)
                FillEdits(ref info, comp);
            return true;
        }

        private static void FillHeights(ref VertexInfo info, TerrainComp comp, Vector3 origin)
        {
            Heightmap map = info.Map;
            // The height before any compiler edit (includes location flattening), the same base the limits measure from.
            info.Base = EngineBaseHeights.TryLocal(map, comp, info.Index, out float preEdit) ? preEdit + origin.y : map.m_buildData.m_baseHeights[info.Index] + origin.y;
            info.Current = map.GetHeight(info.X, info.Y) + origin.y;
            int half = map.m_width / 2;
            info.World = new Vector3(origin.x + (info.X - half) * map.m_scale, info.Current, origin.z + (info.Y - half) * map.m_scale);
        }

        private static void FillEdits(ref VertexInfo info, TerrainComp comp)
        {
            info.Comp = comp;
            info.LevelDelta = comp.m_levelDelta[info.Index];
            info.SmoothDelta = comp.m_smoothDelta[info.Index];
            info.HeightModified = comp.m_modifiedHeight[info.Index];
            info.PaintModified = comp.m_modifiedPaint[info.Index];
        }

        /// <summary>The ground height shown now at a world position (terrain only, no buildings), or the fallback.</summary>
        public static float GroundHeight(Vector3 world, float fallback)
        {
            return Heightmap.GetHeight(world, out float height) ? height : fallback;
        }
    }
}
