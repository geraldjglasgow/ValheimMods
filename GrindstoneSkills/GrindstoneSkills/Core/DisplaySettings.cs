using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>Section 7: each player's own display preferences. Not synced.</summary>
    public static class DisplaySettings
    {
        public const string Section = "7 - Display";

        public static ConfigEntry<bool> StarsOnIcons { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            StarsOnIcons = config.Bind(Section, "Stars On Icons", true,
                "Shows an egg's stars (the level of the hen that laid it) on its icon in inventories, containers and the hotbar.", synced: false);
        }
    }
}
