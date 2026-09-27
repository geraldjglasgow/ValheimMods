using BepInEx.Configuration;
using SyncedConfig;

namespace EarthWright.Core
{
    /// <summary>Section "0. General": the lock, the master switch and the local on/off preference.</summary>
    public static class GeneralSettings
    {
        public static ConfigEntry<bool> LockConfiguration { get; private set; }
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> LocalEnabled { get; private set; }
        public static ConfigEntry<bool> DebugLog { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            LockConfiguration = synced.BindLocking(Sections.General, "Lock Configuration", true,
                "[Server Only] When on, every player uses the server's values for the synced settings and cannot change them locally; server admins still can.");
            Enabled = synced.Bind(Sections.General, "Enabled", true,
                "Master switch. Off: the hoe and the cultivator behave exactly as in the unmodded game and EarthWright's own menu entries are hidden.");
            LocalEnabled = synced.Bind(Sections.General, "Use EarthWright", true,
                "Your own switch (also the console command 'ew on' / 'ew off'). Off: your hoe and cultivator behave as in the unmodded game. Height limits and protection still apply.",
                synced: false);
            DebugLog = synced.Bind(Sections.General, "Debug Log", false,
                "Writes every terrain edit sent and applied to the BepInEx log.", synced: false);
        }

        /// <summary>EarthWright's hoe features are on for this player: the server allows them and the player wants them.</summary>
        public static bool Active => Enabled.Value && LocalEnabled.Value;
    }
}
