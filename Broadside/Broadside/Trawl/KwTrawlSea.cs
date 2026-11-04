using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The sea the net is in: the water's level (the game's sea level; the net is under the waves, so they are left out),
    /// the sea floor's height (the terrain where it is loaded, else the world's generated height, so a dedicated server or
    /// a peer at the edge of what it has loaded still finds it) and the biome of the water.
    /// </summary>
    public static class KwTrawlSea
    {
        public static float Water => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        /// <summary>The sea floor's height under a point.</summary>
        public static float Floor(Vector3 at)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float height))
                return height;
            return WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(at.x, at.z) : Water - 100f;
        }

        /// <summary>The biome of the water at a point (None before the world is made).</summary>
        public static Heightmap.Biome Biome(Vector3 at) => WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(at) : Heightmap.Biome.None;
    }
}
