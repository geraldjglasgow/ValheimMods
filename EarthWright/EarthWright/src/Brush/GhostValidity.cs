using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The placement status of the moved ghost. When the game aimed at terrain its own checks stand; the moved centre
    /// is only checked again for no-build locations and wards (never made more permissive). When the brush recovered
    /// the aim from a rock, tree or cliff ("no silent blocks"), the game's refusal was about that object, so the status
    /// is worked out afresh at the ground point with the game's own ground, dungeon, no-build and ward checks. Wards over
    /// the whole footprint are the Protection module's job. Runs on the player's machine, as the game's checks do.
    /// </summary>
    public static class GhostValidity
    {
        public static void Apply(Player player, GameObject ghost, Vector3 center, bool direct, bool flashGuardStone)
        {
            Player.PlacementStatus status = player.m_placementStatus;
            if (!direct)
            {
                ghost.SetActive(true);
                status = GroundStatus(player, ghost.GetComponent<Piece>(), center);
            }
            status = AreaStatus(status, center, flashGuardStone);
            player.m_placementStatus = status;
            player.SetPlacementGhostValid(status == Player.PlacementStatus.Valid);
        }

        /// <summary>The game's checks for a ground piece at a ground point (UpdatePlacementGhost, heightmap branch).</summary>
        private static Player.PlacementStatus GroundStatus(Player player, Piece piece, Vector3 center)
        {
            Heightmap map = Heightmap.FindHeightmap(center);
            if (map == null || piece == null)
                return Player.PlacementStatus.Invalid;
            if (piece.m_cultivatedGroundOnly && !map.IsCultivated(center))
                return Player.PlacementStatus.NeedCultivated;
            if (piece.m_vegetationGroundOnly && NoVegetation(map, center))
                return Player.PlacementStatus.NeedDirt;
            if (!piece.m_allowedInDungeons && player.InInterior() && !DungeonBuildAllowed())
                return Player.PlacementStatus.NotInDungeon;
            return Player.PlacementStatus.Valid;
        }

        private static bool NoVegetation(Heightmap map, Vector3 point)
        {
            float mask = map.GetVegetationMask(point);
            return map.GetBiome(point) == Heightmap.Biome.AshLands ? mask > 0.1f : mask < 0.25f;
        }

        private static bool DungeonBuildAllowed()
        {
            bool interiorOverride = EnvMan.instance != null && EnvMan.instance.CheckInteriorBuildingOverride();
            bool globalKey = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild);
            return interiorOverride || globalKey;
        }

        /// <summary>No-build locations and wards at the moved centre; only ever turns a Valid status into a refusal.</summary>
        private static Player.PlacementStatus AreaStatus(Player.PlacementStatus status, Vector3 center, bool flash)
        {
            if (status != Player.PlacementStatus.Valid)
                return status;
            if (Location.IsInsideNoBuildLocation(center))
                return Player.PlacementStatus.NoBuildZone;
            if (!PrivateArea.CheckAccess(center, 0f, flash, false))
                return Player.PlacementStatus.PrivateZone;
            return status;
        }
    }
}
