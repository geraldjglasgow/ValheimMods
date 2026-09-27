using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Reads the raw edit values of a heightmap's terrain compiler as this machine holds them (replicated from the
    /// compiler's ZDO). A heightmap nobody has edited has no compiler: its values are the untouched state (no height
    /// change, no painted texture), with the paint the game would start a new compiler from.
    /// </summary>
    internal static class RawAccess
    {
        /// <summary>The loaded (full detail) heightmap at a position, or null when that ground is not loaded here.</summary>
        public static Heightmap MapAt(Vector3 position)
        {
            Heightmap map = Heightmap.FindHeightmap(position);
            return map != null && map.m_buildData != null ? map : null;
        }

        /// <summary>The heightmap's compiler if it exists and is ready, without creating one.</summary>
        public static TerrainComp CompOf(Heightmap map)
        {
            TerrainComp comp = TerrainComp.FindTerrainCompiler(map.transform.position);
            if (comp == null || !comp.m_initialized || comp.m_nview == null || !comp.m_nview.IsValid())
                return null;
            return comp;
        }

        public static int Pitch(Heightmap map) => map.m_width + 1;

        /// <summary>The raw values of one array index (height and paint share the index).</summary>
        public static RawVertex Read(Heightmap map, TerrainComp comp, int index)
        {
            if (comp == null)
                return Untouched(map, index);
            return new RawVertex
            {
                Index = index,
                HeightModified = comp.m_modifiedHeight[index],
                LevelDelta = comp.m_levelDelta[index],
                SmoothDelta = comp.m_smoothDelta[index],
                PaintModified = comp.m_modifiedPaint[index],
                Paint = comp.m_paintMask[index],
            };
        }

        private static RawVertex Untouched(Heightmap map, int index)
        {
            int pitch = Pitch(map);
            Color paint = map.m_paintMask != null ? map.GetPaintMask(index % pitch, index / pitch) : Color.clear;
            return new RawVertex { Index = index, Paint = paint };
        }

        /// <summary>The two values would show the same ground (unmodified paint values are not shown, so they do not count).</summary>
        public static bool Same(RawVertex a, RawVertex b)
        {
            if (a.HeightModified != b.HeightModified || a.PaintModified != b.PaintModified)
                return false;
            if (a.LevelDelta != b.LevelDelta || a.SmoothDelta != b.SmoothDelta)
                return false;
            return !a.PaintModified || a.Paint == b.Paint;
        }

        /// <summary>A stable key for a heightmap (and its compiler) from its centre position.</summary>
        public static Vector2Int KeyOf(Vector3 position) => new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
    }
}
