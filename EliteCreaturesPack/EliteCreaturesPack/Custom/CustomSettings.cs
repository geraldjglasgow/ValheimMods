using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Custom
{
    /// <summary>
    /// Section 29: custom creatures (features/custom-creatures.md). One switch; everything else lives in the YAML files.
    /// Synced and locked like every other setting. Read by the server when it builds a world's creatures: a change applies
    /// the next time a world is loaded. Players follow the server's build whatever their own value.
    /// </summary>
    public static class CustomSettings
    {
        public const string Section = "29 - Custom Creatures";

        private static ConfigEntry<bool> enabled = null!;

        /// <summary>Whether the server builds the custom creatures of its files.</summary>
        public static bool On => enabled.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "Custom creatures and human enemies, defined in EliteCreaturesPack.Creatures*.yml in the config folder and its "
                + "subfolders (the server's files are sent to every player). Off: none is built; their creatures already in the "
                + "world are kept, unseen, until it is on again. A change applies the next time a world is loaded.");
        }
    }
}
