using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// Section "13. Tools": the shovel, the upgrade levels of the three terrain tools, and what holding a terrain tool
    /// gives (reach, light, speed, a torch in the other hand). Everything that changes play is synced; the light's
    /// colour is each player's own.
    /// </summary>
    public static class GearSettings
    {
        public static ConfigEntry<bool> ShovelEnabled { get; private set; }
        public static ConfigEntry<string> ShovelRecipe { get; private set; }
        public static ConfigEntry<string> ShovelStation { get; private set; }
        public static ConfigEntry<string> ShovelEntries { get; private set; }
        public static ConfigEntry<float> ShovelMaxDurability { get; private set; }
        public static ConfigEntry<float> ShovelWearPerUse { get; private set; }

        public static ToolLevelSettings Hoe { get; private set; }
        public static ToolLevelSettings Cultivator { get; private set; }
        public static ToolLevelSettings Shovel { get; private set; }
        public static ConfigEntry<string> StationLevels { get; private set; }
        public static ConfigEntry<string> RadiusPerLevel { get; private set; }
        public static ConfigEntry<string> LevelUnlocks { get; private set; }

        public static ConfigEntry<float> Reach { get; private set; }
        public static ConfigEntry<bool> ToolLight { get; private set; }
        public static ConfigEntry<float> LightRange { get; private set; }
        public static ConfigEntry<float> LightIntensity { get; private set; }
        public static ConfigEntry<Color> LightColour { get; private set; }
        public static ConfigEntry<float> MovementSpeed { get; private set; }
        public static ConfigEntry<bool> TorchInLeftHand { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindShovel(synced);
            BindLevels(synced);
            BindHeld(synced);
            BindLight(synced);
        }

        private static void BindShovel(SyncedConfiguration synced)
        {
            ShovelEnabled = synced.Bind(Sections.Gear, "Shovel Enabled", true,
                "The craftable shovel, a terrain tool with its own build menu. Off: it can no longer be crafted or upgraded; shovels already made keep working and can be repaired.");
            ShovelRecipe = synced.Bind(Sections.Gear, "Shovel Recipe", "Wood:5, Flint:3",
                "What crafting the shovel costs, as Item:Amount pairs separated by commas (item prefab names such as Wood, Flint, Bronze).");
            ShovelStation = synced.Bind(Sections.Gear, "Shovel Station", "piece_workbench",
                "Prefab name of the crafting station that makes, upgrades and repairs the shovel (piece_workbench, forge, piece_stonecutter, ...).");
            ShovelEntries = synced.Bind(Sections.Gear, "Shovel Entries", "ew_dig, ew_smooth, mud_road_v2, ew_reset",
                "The shovel's build-menu entries in menu order, by piece prefab name: EarthWright's own entries (ew_dig, ew_lower, ew_smooth, ew_paint, " +
                "ew_reset, ew_ramp, ew_road, ...) and the game's hoe and cultivator entries (mud_road_v2, raise_v2, path_v2, paved_road_v2, cultivate_v2, replant_v2). " +
                "An entry switched off in section 12 is hidden here too. Unknown names are skipped. Custom entries for the shovel are added after these.");
            ShovelMaxDurability = synced.Bind(Sections.Gear, "Shovel Max Durability", 200f,
                "Durability of a new shovel (level 1).", acceptableValues: new AcceptableValueRange<float>(10f, 10000f));
            ShovelWearPerUse = synced.Bind(Sections.Gear, "Shovel Wear Per Use", 1f,
                "Durability each use of the shovel costs. 0: the shovel never wears out.", acceptableValues: new AcceptableValueRange<float>(0f, 100f));
        }

        private static void BindLevels(SyncedConfiguration synced)
        {
            const string gameCost = "Empty: the game's own upgrade cost.";
            Hoe = ToolLevelSettings.Bind(synced, ToolNames.Hoe, "Hoe", "", gameCost);
            Cultivator = ToolLevelSettings.Bind(synced, ToolNames.Cultivator, "Cultivator", "", gameCost);
            Shovel = ToolLevelSettings.Bind(synced, ToolNames.Shovel, "Shovel", "Wood:2, Flint:1", "Empty: upgrades cost nothing.");
            StationLevels = synced.Bind(Sections.Gear, "Upgrade Station Levels", "1, 2, 3, 4, 5, 5",
                "The crafting station level each tool level needs, comma-separated: the first number to craft the tool, the next ones for each upgrade. " +
                "Levels past the end of the list use its last number. Empty: the game's rule (one station level per tool level), which a workbench cannot meet past level 5.");
            RadiusPerLevel = synced.Bind(Sections.Gear, "Radius Per Level", "",
                "Largest brush radius in metres for each tool level, comma-separated (for example 4, 6, 8, 12, 16, 20); levels past the end use the last number. Empty: the level does not limit the radius.");
            LevelUnlocks = synced.Bind(Sections.Gear, "Level Unlocks", "",
                "What each tool level unlocks, as feature:level pairs (for example square:2, lower:3, ramp:4, instant:5). A feature is a build-menu entry " +
                "(EarthWright's by name: lower, smooth, paint, reset, ramp, road, till, uproot, dig ...; the game's by prefab name such as paved_road_v2), " +
                "a brush shape (square, rectangle, ring, frame) or a level style (step, instant). Features not listed are always available. Empty: nothing is locked.");
        }

        private static void BindHeld(SyncedConfiguration synced)
        {
            Reach = synced.Bind(Sections.Gear, "Reach", 20f,
                "How far away you can use a terrain tool, in metres, while one is in your hand (the game's reach is 5 m). Back to the game's reach when you put it away.",
                acceptableValues: new AcceptableValueRange<float>(5f, 50f));
            MovementSpeed = synced.Bind(Sections.Gear, "Movement Speed", 1.1f,
                "Running and sprinting speed while a terrain tool is in your hand, as a multiple of the normal speed. 1: the game's speed.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 3f));
            TorchInLeftHand = synced.Bind(Sections.Gear, "Torch In Left Hand", true,
                "A torch stays in your left hand while a terrain tool is in your right one: equipping the tool keeps the torch, and equipping a torch keeps the tool.");
        }

        private static void BindLight(SyncedConfiguration synced)
        {
            ToolLight = synced.Bind(Sections.Gear, "Tool Light", true,
                "Terrain tools give off light while held, seen by every player.");
            LightRange = synced.Bind(Sections.Gear, "Light Range", 8f,
                "How far the tool's light reaches, in metres.", acceptableValues: new AcceptableValueRange<float>(1f, 30f));
            LightIntensity = synced.Bind(Sections.Gear, "Light Intensity", 1.2f,
                "Brightness of the tool's light.", acceptableValues: new AcceptableValueRange<float>(0f, 5f));
            LightColour = synced.Bind(Sections.Gear, "Light Colour", new Color(1f, 0.85f, 0.6f, 1f),
                "Colour of the tools' light as you see it (on your own tool and on other players').", synced: false);
        }
    }
}
