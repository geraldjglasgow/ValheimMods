using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 29: the Farming module's master switch, giant crops, each player's own switches, and the order the
    /// Farming settings are bound in: Farming (this one), Farming Perks, Farming Experience and Compost. Gameplay values are synced and lockable; callouts, row planting and auto-replant
    /// are each player's own.
    /// </summary>
    public static class FarmingSettings
    {
        public const string Section = "29 - Farming";
        public const string PerksSection = "30 - Farming Perks";
        public const string ExperienceSection = "31 - Farming Experience";
        public const string CompostSection = "32 - Compost";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> GiantChanceAt100 { get; private set; }
        public static ConfigEntry<int> GiantYield { get; private set; }
        public static ConfigEntry<float> GiantSize { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }
        public static ConfigEntry<bool> RowPlanting { get; private set; }
        public static ConfigEntry<bool> AutoReplant { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindGameplay(config);
            BindPlayer(config);
            FarmingPerkSettings.Initialize(config);
            FarmingExperienceSettings.Initialize(config);
            CompostSettings.Initialize(config);
        }

        private static void BindGameplay(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Farming Enabled", true,
                "Turns every Farming feature on or off: perks, giant crops, compost, experience changes. Off, crops behave exactly as in the game. Levels are kept.");
            GiantChanceAt100 = config.Bind(Section, "Giant Crop Chance At Level 100", 2f,
                "Chance in percent that a crop ripens into a giant, from the planter's level (half at level 50).",
                acceptableValues: Settings.UpTo(100f));
            GiantYield = config.Bind(Section, "Giant Crop Yield", 6,
                "How many times the crop's normal amount a giant gives.", acceptableValues: new AcceptableValueRange<int>(1, 50));
            GiantSize = config.Bind(Section, "Giant Crop Size", 2.5f,
                "How many times bigger a giant crop stands.", acceptableValues: new AcceptableValueRange<float>(1f, 5f));
        }

        private static void BindPlayer(SyncedConfiguration config)
        {
            ShowCallouts = config.Bind(Section, "Show Callouts", true,
                "Shows words like Giant turnip! and returned seeds floating above your crops.", synced: false);
            RowPlanting = config.Bind(Section, "Row Planting", true,
                "Plants a whole row at once once your level allows it (see Row Of Three Level). Hold the alternative place key (Shift) to plant one.", synced: false);
            AutoReplant = config.Bind(Section, "Auto Replant", true,
                "Puts a picked crop's plant back in its spot with a seed from your inventory, once your level allows it (see Auto Replant Level).", synced: false);
        }
    }
}
