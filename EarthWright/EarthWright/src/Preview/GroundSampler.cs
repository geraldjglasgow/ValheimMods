using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// Terrain height lookups for the visuals, the same value as <c>TerrainRead.GroundHeight</c> (the heightmap's own
    /// height, buildings ignored) but remembering the heightmap of the previous lookup: outlines and grids ask for
    /// hundreds of neighbouring points per frame, and the game's search walks every loaded heightmap each time.
    /// </summary>
    internal static class GroundSampler
    {
        private static Heightmap last;

        public static float Height(Vector3 world, float fallback)
        {
            Heightmap map = last != null && last.IsPointInside(world) ? last : Heightmap.FindHeightmap(world);
            if (map == null)
                return fallback;
            last = map;
            return map.GetWorldHeight(world, out float height) ? height : fallback;
        }
    }
}
