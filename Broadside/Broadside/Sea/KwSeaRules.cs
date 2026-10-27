using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's own building rules for a sea edit, on the editing player's game, at the part's place: within the build
    /// reach, outside a no-build location, the private area's access (<see cref="PrivateArea.CheckAccess"/>, the ward
    /// flashing as the game's), and a workbench in range, as building a ship needs (the karve's station, the game's own
    /// <see cref="CraftingStation.HaveBuildStationInRange"/>; one built aboard as a rider counts; none needed with the
    /// no-cost cheat, as the game's).
    /// </summary>
    public static class KwSeaRules
    {
        public static string? Why(Player player, KwSeaShip ship, Vector3 worldPoint)
        {
            if (Vector3.Distance(player.m_eye.position, worldPoint) > player.m_maxPlaceDistance + 1f)
                return "Too far away";
            if (Location.IsInsideNoBuildLocation(worldPoint))
                return "$msg_nobuildzone";
            if (!PrivateArea.CheckAccess(worldPoint))
                return "$msg_privatezone";
            return player.NoCostCheat() ? null : Station(ship, worldPoint);
        }

        // A workbench (the ship's own station) in range of the point; null when one is.
        private static string? Station(KwSeaShip ship, Vector3 worldPoint)
        {
            CraftingStation? station = ship.GetComponent<Piece>()?.m_craftingStation;
            if (station == null || CraftingStation.HaveBuildStationInRange(station.m_name, worldPoint) != null)
                return null;
            return "Changing a ship needs a workbench in range (one built aboard counts)";
        }
    }
}
