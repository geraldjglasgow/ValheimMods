using BepInEx.Configuration;
using SyncedConfig;

namespace Broadside
{
    /// <summary>
    /// Broadside's .cfg (<c>BepInEx/config/milkyteam.broadside.cfg</c>), written at startup, hot reloaded on edit and bound
    /// to the server's values through Charter: the lock, and whether any piece is built on a launched ship
    /// (<see cref="KwShipRiderRules"/>; 2026-11-01, from a player's wish to "build on these boats like any other structure.
    /// I want to live on board one of these beauties!").
    /// </summary>
    public static class KwSettings
    {
        private static ConfigEntry<bool>? buildingOnShips;

        /// <summary>Whether players build any piece on a launched ship, not only ship cannons (the server's value while it binds).</summary>
        public static bool BuildingOnShips => buildingOnShips?.Value ?? false;

        public static void Bind(SyncedConfiguration config)
        {
            config.BindLocking("General", "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            buildingOnShips = config.Bind("Ships", "AllowBuildingOnShips", false,
                "When on, players build any piece on a launched ship (walls, floors, roofs, chests, beds, workbenches, decorations, " +
                "other mods' pieces), and it sails with the ship. When off, only ship cannons are built there. Pieces already on a " +
                "ship stay either way.");
        }
    }
}
