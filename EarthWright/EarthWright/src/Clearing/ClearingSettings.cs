using System.Linq;
using BepInEx.Configuration;
using EarthWright.Core;
using EarthWright.Terrain;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Section "5. Reset and Clearing": the reset keys and radius, and everything about clearing objects. Every setting
    /// that changes the world is synced and lockable; only the keys are personal.
    /// </summary>
    public static class ClearingSettings
    {
        /// <summary>The largest radius an admin may give to the console commands.</summary>
        public const float AdminMaxRadius = 128f;

        /// <summary>The game's pickables that are boss offerings, quest items or treasure rather than plants and stones.</summary>
        private const string DefaultKeep = "Pickable_DragonEgg,goblin_totempole,Pickable_*CoreStand,Pickable_Swordpiece*,Pickable_VoltureEgg," +
            "Pickable_Charredskull,Pickable_FrostCoreHanger,Morkhalla_Eye*,Pickable_Dvergr*,Lured*,Pickable_Fishingrod,Pickable_RoyalJelly," +
            "Pickable_ForestCryptRemains*,Pickable_MountainRemains*";

        public static ConfigEntry<float> ResetAroundRadius { get; private set; }
        public static ConfigEntry<bool> ResetSkipsPieces { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetAroundKey { get; private set; }
        public static ConfigEntry<float> CommandMaxRadius { get; private set; }

        /// <summary>"Clearing Enabled": the Clear entry exists and players may use the clearing commands.</summary>
        public static ConfigEntry<bool> EnabledEntry { get; private set; }
        public static ConfigEntry<ClearingMode> Mode { get; private set; }
        public static ConfigEntry<bool> SurvivalDrops { get; private set; }
        public static ConfigEntry<float> ClearingRadius { get; private set; }

        public static ConfigEntry<bool> ClearTrees { get; private set; }
        public static ConfigEntry<bool> ClearStumps { get; private set; }
        public static ConfigEntry<bool> ClearLogs { get; private set; }
        public static ConfigEntry<bool> ClearShrubs { get; private set; }
        public static ConfigEntry<bool> ClearRocks { get; private set; }
        public static ConfigEntry<bool> ClearPickables { get; private set; }
        public static ConfigEntry<bool> ClearOres { get; private set; }
        public static ConfigEntry<string> OreDrops { get; private set; }
        public static ConfigEntry<string> KeepPickables { get; private set; }

        /// <summary>Clearing is switched on (the Menu module shows the Clear entry only while this is true).</summary>
        public static bool Enabled => EnabledEntry != null && EnabledEntry.Value;

        /// <summary>The server's clearing mode is Survival.</summary>
        public static bool Survival => Mode != null && Mode.Value == ClearingMode.Survival;

        public static void Bind(SyncedConfiguration synced)
        {
            BindReset(synced);
            BindClearing(synced);
            BindKinds(synced);
        }

        private static void BindReset(SyncedConfiguration synced)
        {
            ResetAroundRadius = synced.Bind(Sections.Reset, "Reset Around Radius", 10f,
                "Radius in metres of the circle around you that the reset-around key (Shift+U by default) and 'ew reset' without a radius restore to the world's generated ground.",
                acceptableValues: new AcceptableValueRange<float>(1f, 50f));
            ResetSkipsPieces = synced.Bind(Sections.Reset, "Reset Skips Ground Under Buildings", true,
                "When on, resetting leaves the ground under building pieces as it is, so a reset never pulls the floor out from under a house.");
            ResetKey = synced.Bind(Sections.Reset, "Reset Key", new KeyboardShortcut(KeyCode.U),
                "Resets the ground inside the brush (height and paint) to the world's generated state while a terrain tool is out. Free of costs.", synced: false);
            ResetAroundKey = synced.Bind(Sections.Reset, "Reset Around Key", new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift),
                "Resets the ground in a circle of 'Reset Around Radius' around you while a terrain tool is out. Free of costs.", synced: false);
            CommandMaxRadius = synced.Bind(Sections.Reset, "Command Max Radius", 50f,
                "Largest radius in metres a player who is not an admin may give to 'ew reset', 'ew forestry' and 'ew debris'. Admins may clear up to 128 m; terrain changes never go past the Operations section's 'Max Radius'.",
                acceptableValues: new AcceptableValueRange<float>(1f, AdminMaxRadius));
        }

        private static void BindClearing(SyncedConfiguration synced)
        {
            EnabledEntry = synced.Bind(Sections.Reset, "Clearing Enabled", false,
                "When on, the hoe gets the Clear entry, the Groundbreaker entry also clears objects, and every player may use 'ew forestry' and 'ew debris'. Off: only admins can clear objects, by console command.");
            Mode = synced.Bind(Sections.Reset, "Clearing Mode", ClearingMode.Remove,
                "Remove: cleared objects vanish, nothing drops and no tool is needed. Survival: trees, logs and stumps need an axe and rocks a pickaxe in your inventory, of a tier high enough for the object.");
            SurvivalDrops = synced.Bind(Sections.Reset, "Survival Drops", true,
                "In Survival mode: when on, cleared objects are chopped and mined the normal way and drop their wood, stone and pickings; off, they vanish once you have the right tool.");
            ClearingRadius = synced.Bind(Sections.Reset, "Clearing Radius", 0f,
                "Radius in metres of the circle the Clear and Groundbreaker entries clear around the aimed point. 0 uses the brush itself (its size and shape).",
                acceptableValues: new AcceptableValueRange<float>(0f, 50f));
        }

        private static void BindKinds(SyncedConfiguration synced)
        {
            ClearTrees = BindKind(synced, "Clear Trees", "standing trees");
            ClearStumps = BindKind(synced, "Clear Stumps", "tree stumps");
            ClearLogs = BindKind(synced, "Clear Logs", "fallen logs");
            ClearShrubs = BindKind(synced, "Clear Shrubs", "bushes and shrubs");
            ClearRocks = BindKind(synced, "Clear Rocks", "rocks and boulders (not ore)");
            ClearPickables = BindKind(synced, "Clear Pickables", "natural pickables: berries, mushrooms, flowers, loose stones, branches and flint (never crops on cultivated ground)");
            ClearOres = synced.Bind(Sections.Reset, "Clear Ore Deposits", false,
                "When on, rocks and veins that drop ore are cleared too (by the Clear entry and 'ew debris'); off keeps them, so clearing never destroys ore.");
            OreDrops = synced.Bind(Sections.Reset, "Ore Drops", "Ore,Scrap,Obsidian,Softtissue,Tar,Sulfur",
                "A rock or pickable counts as an ore deposit when one of its drops has one of these words in its item name (comma separated, capital letters matter).");
            KeepPickables = synced.Bind(Sections.Reset, "Keep Pickables", DefaultKeep,
                "Pickables that clearing never takes, by prefab name (comma separated, * matches any run of characters): boss offerings, quest items and treasure.");
        }

        private static ConfigEntry<bool> BindKind(SyncedConfiguration synced, string key, string what)
        {
            return synced.Bind(Sections.Reset, key, true, $"When on, clearing takes away {what}.");
        }

        /// <summary>What the Clear and Groundbreaker entries clear, from the per-kind switches.</summary>
        public static ClearCategory EntryMask
        {
            get
            {
                ClearCategory mask = ClearCategory.None;
                if (ClearTrees.Value) mask |= ClearCategory.Trees;
                if (ClearStumps.Value) mask |= ClearCategory.Stumps;
                if (ClearLogs.Value) mask |= ClearCategory.Logs;
                if (ClearShrubs.Value) mask |= ClearCategory.Shrubs;
                if (ClearRocks.Value) mask |= ClearCategory.Rocks;
                if (ClearPickables.Value) mask |= ClearCategory.Pickables | ClearCategory.Debris;
                return AllowOres(mask);
            }
        }

        /// <summary>Adds ore deposits to a request when the server allows clearing them, removes them otherwise.</summary>
        public static ClearCategory AllowOres(ClearCategory mask)
        {
            return ClearOres.Value ? mask | ClearCategory.Ores : mask & ~ClearCategory.Ores;
        }

        /// <summary>The parts of item names that mark an ore drop.</summary>
        public static string[] OreKeywords() => List(OreDrops.Value);

        /// <summary>The prefab name patterns of pickables clearing leaves alone.</summary>
        public static string[] KeptPickables() => List(KeepPickables.Value);

        private static string[] List(string text)
        {
            return (text ?? "").Split(',').Select(k => k.Trim()).Where(k => k.Length > 0).ToArray();
        }

        /// <summary>The radius a console command may use: the typed or default value within the player's limit.</summary>
        public static float CommandRadius(float requested, bool admin)
        {
            float max = admin ? AdminMaxRadius : CommandMaxRadius.Value;
            return Mathf.Clamp(requested, 1f, max);
        }

        /// <summary>The radius of a terrain command: as <see cref="CommandRadius"/>, and never past the Engine's "Max Radius", which every owner would cut it to.</summary>
        public static float TerrainRadius(float requested, bool admin)
        {
            return Mathf.Min(CommandRadius(requested, admin), EngineSettings.MaxRadiusValue);
        }
    }
}
