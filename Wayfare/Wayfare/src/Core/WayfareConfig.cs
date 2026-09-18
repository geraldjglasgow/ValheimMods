using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace Wayfare.Core
{
    /// <summary>Every config entry Wayfare binds. Gameplay-affecting entries are synced and lockable; display-only
    /// entries (the hotkey, icon scale, show-tags) are bound unsynced so each player keeps their own.</summary>
    public static class WayfareConfig
    {
        public static ConfigEntry<bool> LockConfiguration { get; private set; }
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> UnownedPortalsArePublic { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ToggleIconsKey { get; private set; }
        public static ConfigEntry<float> IconScale { get; private set; }
        public static ConfigEntry<bool> ShowTags { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking("General", "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            Enabled = config.Bind("General", "Enabled", true,
                "Master switch. Off disables portal targeting and access modes; portals behave as vanilla.");
            UnownedPortalsArePublic = config.Bind("Access", "Unowned Portals Are Public", true,
                "A portal nobody has set a mode on yet is targetable by anyone (Public) when on, or by nobody but an admin (Private-like) when off.");
            ToggleIconsKey = config.Bind("Map", "Toggle Icons Key", new KeyboardShortcut(KeyCode.P),
                "Toggles portal icons on the ordinary (non-targeting) map.", synced: false);
            IconScale = config.Bind("Map", "Icon Scale", 1f,
                "Size of a portal icon on the map, relative to the default.", synced: false);
            ShowTags = config.Bind("Map", "Show Tags", true,
                "Draw a portal's tag text next to its icon on the map.", synced: false);
        }
    }
}
