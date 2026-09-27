using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// The game's placement checks that must still hold after the Extras move a ghost (seed grid) or lift a refusal
    /// (cultivate anywhere): dungeons, no-build locations and wards at the ghost's final point. Only ever turns a valid
    /// status into a refusal, never the other way round.
    /// </summary>
    public static class GhostStatus
    {
        public static void Set(Player player, Player.PlacementStatus status)
        {
            player.m_placementStatus = status;
            player.SetPlacementGhostValid(status == Player.PlacementStatus.Valid);
        }

        /// <summary>The status after the area checks at <paramref name="point"/>; <paramref name="flash"/> flashes a refusing ward.</summary>
        public static Player.PlacementStatus Checked(Player player, Piece piece, Vector3 point, bool flash)
        {
            if (!piece.m_allowedInDungeons && player.InInterior() && !DungeonBuildAllowed())
                return Player.PlacementStatus.NotInDungeon;
            if (Location.IsInsideNoBuildLocation(point))
                return Player.PlacementStatus.NoBuildZone;
            if (!PrivateArea.CheckAccess(point, 0f, flash, false))
                return Player.PlacementStatus.PrivateZone;
            return Player.PlacementStatus.Valid;
        }

        private static bool DungeonBuildAllowed()
        {
            bool interiorOverride = EnvMan.instance != null && EnvMan.instance.CheckInteriorBuildingOverride();
            bool globalKey = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild);
            return interiorOverride || globalKey;
        }
    }
}
