using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The station repair key of section "8. Homestead": it changes what opening a station does to the player's gear, so it is synced and lockable; read at use time.</summary>
    public static class StationRepairSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> AutoRepair { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            AutoRepair = synced.Bind(Section, "Auto Repair", true,
                "Opening a crafting station (workbench, forge, black forge, galdr table, artisan table, a cart's workbench, any station with the game's repair button) repairs every item in your inventory that this station can repair at its current level, at once, "
                + "as if you pressed its repair button once for each: the same items, the same Crafting skill gain, no cost. One message with the count; nothing when nothing needed repair.");
        }
    }
}
