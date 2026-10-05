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

        public static ConfigEntry<bool> SeaGatesEnabled { get; private set; }
        public static ConfigEntry<string> PillarRecipe { get; private set; }
        public static ConfigEntry<bool> AllowRestrictedCargo { get; private set; }
        public static ConfigEntry<float> MinGateWidth { get; private set; }
        public static ConfigEntry<float> MaxGateWidth { get; private set; }
        public static ConfigEntry<float> MinWaterDepth { get; private set; }
        public static ConfigEntry<float> ProtectionSeconds { get; private set; }
        public static ConfigEntry<float> CrewWaitSeconds { get; private set; }

        public static ConfigEntry<bool> QuickPortals { get; private set; }
        public static ConfigEntry<float> QuickPortalRange { get; private set; }
        public static ConfigEntry<float> QuickPortalSeconds { get; private set; }
        public static ConfigEntry<bool> ScreenOnlyWhenLoading { get; private set; }
        public static ConfigEntry<bool> QuickAreaLoading { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindGeneral(config);
            BindSeaGates(config);
            BindJumpSpeed(config);
        }

        /// <summary>How long a jump takes changes the game, so those keys are synced and lockable; the teleport screen
        /// and how fast the own machine loads the land are each player's own choice. Moved from OpenKeep's Homestead
        /// section on 2026-10-04 with the same key names.</summary>
        private static void BindJumpSpeed(SyncedConfiguration config)
        {
            QuickPortals = config.Bind("Jump Speed", "Quick Portals", true,
                "The closer together two portals are, the quicker the jump: the game's 8 s of a portal jump shrink with the distance, down to Quick Portal Seconds for portals side by side. Every long jump counts, from any portal or the console, not dungeon doors and not sea gates. The game still waits for a far area to load. Off: the game's 8 s.");
            QuickPortalRange = config.Bind("Jump Speed", "Quick Portal Range", 10000f,
                "Metres (on the map) between the two portals at which a jump takes the game's full 8 s; closer is quicker, in proportion. With 10000 a 1 km jump takes about 1.2 s; lower it (4000 to 5000) for a bigger difference between near and far portals.",
                acceptableValues: new AcceptableValueRange<float>(10f, 20000f));
            QuickPortalSeconds = config.Bind("Jump Speed", "Quick Portal Seconds", 0.5f,
                "Seconds a jump takes between portals side by side.",
                acceptableValues: new AcceptableValueRange<float>(0f, 8f));
            ScreenOnlyWhenLoading = config.Bind("Jump Speed", "Screen Only When Loading", true,
                "A jump to a place already loaded around you (about 100-150 m) keeps the screen clear: no black screen, no teleport swirl. Farther jumps show the game's teleport screen while the area loads. Off: every jump shows it, as in the game.", synced: false);
            QuickAreaLoading = config.Bind("Jump Speed", "Quick Area Loading", true,
                "After a long jump the land around you loads as fast as your PC allows instead of the game's one piece (64 m square) every 0.1 s. With a high simulation distance this saves several seconds per jump. Off: the game's pace.", synced: false);
        }

        private static void BindGeneral(SyncedConfiguration config)
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

        private static void BindSeaGates(SyncedConfiguration config)
        {
            SeaGatesEnabled = config.Bind("Sea Gates", "Enabled", true,
                "Sea gates: two pillars on land or in shallow water open a portal for ships between them. Off: pillars stand but no ship jumps.");
            PillarRecipe = config.Bind("Sea Gates", "Pillar Recipe", "Stone:10,FineWood:5,GreydwarfEye:5,SurtlingCore:1",
                "What one sea gate pillar costs, as Item:Amount pairs. Built with the hammer near a workbench.");
            AllowRestrictedCargo = config.Bind("Sea Gates", "Allow Restricted Cargo", false,
                "When on, ships carrying ore, metal and other items portals refuse may pass through sea gates.");
            MinGateWidth = config.Bind("Sea Gates", "Min Gate Width", 10f,
                "Closest two pillars may stand to pair, in metres.");
            MaxGateWidth = config.Bind("Sea Gates", "Max Gate Width", 15f,
                "Farthest two pillars may stand to pair, in metres.");
            MinWaterDepth = config.Bind("Sea Gates", "Min Water Depth", 1f,
                "Water depth a gate needs between its pillars and on the side a ship comes out, in metres.");
            ProtectionSeconds = config.Bind("Sea Gates", "Protection Seconds", 5f,
                "After a jump, players aboard and the ship take no damage for this long.");
            CrewWaitSeconds = config.Bind("Sea Gates", "Crew Wait Seconds", 20f,
                "How long the crew waits for the ship to arrive before being set ashore beside the destination gate.");
        }
    }
}
