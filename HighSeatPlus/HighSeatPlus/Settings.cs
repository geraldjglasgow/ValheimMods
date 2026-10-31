using BepInEx.Configuration;

namespace HighSeatPlus
{
    /// <summary>
    /// The .cfg. Every setting is this player's own (how their PC draws the world), so nothing is synced. Building support
    /// checked on change has no setting: it gives the game's own results.
    /// </summary>
    internal static class Settings
    {
        private const string General = "1 - General";

        public static ConfigEntry<bool> CombineBuildingMeshes = null!;
        public static ConfigEntry<bool> SimplerFarPlayers = null!;

        public static void Bind(ConfigFile config)
        {
            CombineBuildingMeshes = config.Bind(General, "Combine building meshes", true,
                "A building that has settled is drawn as a few combined meshes instead of one per piece; a piece that is " +
                "placed, removed, damaged or repaired is split back out. Colliders and everything the game checks stay as " +
                "they are.");
            SimplerFarPlayers = config.Bind(General, "Simpler far players", true,
                "Players beyond the nearest few are drawn without cloth and animate less often. Nobody is ever hidden, and " +
                "party members and anyone near enough to fight are always drawn in full.");
        }
    }
}
