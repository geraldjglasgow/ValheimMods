using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Whether a crop can be sown at a spot without the player placing it by hand (row planting, auto-replant), so no
    /// seed is spent on a plant that would die: cultivated ground where the piece needs it, a biome the crop grows in at
    /// the planter's level, outside no-build locations and wards the player may not use, and room to grow (nothing
    /// within its grow radius on the layers the game's own room check reads, except the object being replaced).
    /// </summary>
    public static class PlantSpot
    {
        private static readonly Collider[] Hits = new Collider[32];
        private static int spaceMask;

        /// <summary>The layers Plant.HaveGrowSpace checks.</summary>
        public static int SpaceMask => spaceMask != 0 ? spaceMask
            : spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");

        public static bool CanSow(CropPlant crop, Vector3 position, float level, GameObject replacing)
        {
            Heightmap map = Heightmap.FindHeightmap(position);
            if (map == null || (crop.Piece.m_cultivatedGroundOnly && !map.IsCultivated(position)))
                return false;
            if ((map.GetBiome(position) & PlantTraits.Biomes(crop, level)) == 0)
                return false;
            if (Location.IsInsideNoBuildLocation(position) || !PrivateArea.CheckAccess(position, 0f, false))
                return false;
            return HasRoom(position, PlantTraits.Radius(crop, level), replacing);
        }

        /// <summary>A spot on the ground: the terrain height under the position.</summary>
        public static Vector3 Ground(Vector3 position)
        {
            if (ZoneSystem.instance != null)
                position.y = ZoneSystem.instance.GetGroundHeight(position);
            return position;
        }

        private static bool HasRoom(Vector3 position, float radius, GameObject replacing)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, Hits, SpaceMask);
            for (int i = 0; i < count; i++)
            {
                ZNetView owner = Hits[i] != null ? Hits[i].GetComponentInParent<ZNetView>() : null;
                if (replacing == null || owner == null || owner.gameObject != replacing)
                    return false;
            }
            return true;
        }
    }
}
