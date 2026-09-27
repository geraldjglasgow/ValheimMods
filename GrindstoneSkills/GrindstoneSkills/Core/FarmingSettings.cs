using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 29: the Farming module's master switch, crop stars (heirloom seeds, companions) and giant crops, each
    /// player's own switches, and the order the Farming settings are bound in: Farming (this one), Farming Perks,
    /// Farming Experience and Compost. Gameplay values are synced and lockable; callouts, row planting and auto-replant
    /// are each player's own.
    /// </summary>
    public static class FarmingSettings
    {
        public const string Section = "29 - Farming";
        public const string PerksSection = "30 - Farming Perks";
        public const string ExperienceSection = "31 - Farming Experience";
        public const string CompostSection = "32 - Compost";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> CropStars { get; private set; }
        public static ConfigEntry<float> SeedLevelsPerStar { get; private set; }
        public static ConfigEntry<float> CompanionLevels { get; private set; }
        public static ConfigEntry<float> CompanionRadius { get; private set; }
        public static ConfigEntry<int> CompanionKinds { get; private set; }
        public static ConfigEntry<float> GiantChanceAt100 { get; private set; }
        public static ConfigEntry<int> GiantYield { get; private set; }
        public static ConfigEntry<float> GiantSize { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }
        public static ConfigEntry<bool> RowPlanting { get; private set; }
        public static ConfigEntry<bool> AutoReplant { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindStars(config);
            BindGiants(config);
            BindPlayer(config);
            FarmingPerkSettings.Initialize(config);
            FarmingExperienceSettings.Initialize(config);
            CompostSettings.Initialize(config);
        }

        private static void BindStars(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Farming Enabled", true,
                "Turns every Farming feature on or off: crop stars, perks, the windmill's stars, compost, experience changes. Off, crops behave exactly as in the game. Levels and stars already on items are kept.");
            CropStars = config.Bind(Section, "Crop Stars", true,
                "Crops roll 0 to 3 stars when they ripen, from the planter's Farming level, with the odds table of section 3.");
            SeedLevelsPerStar = config.Bind(Section, "Seed Levels Per Star", 10f,
                "Levels added to the planter's level for each star of the seed a crop was planted from (heirloom seeds).", acceptableValues: Settings.UpTo(50f));
            CompanionLevels = config.Bind(Section, "Companion Levels", 5f,
                "Levels added for each other kind of crop growing or ripe near a crop when it ripens (companion planting). A seed and its crop are one kind.",
                acceptableValues: Settings.UpTo(50f));
            CompanionRadius = config.Bind(Section, "Companion Radius", 2f,
                "How far away, in metres, another crop counts as a companion.", acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            CompanionKinds = config.Bind(Section, "Companion Kinds", 3,
                "The most companion kinds that count for one crop.", acceptableValues: new AcceptableValueRange<int>(0, 10));
        }

        private static void BindGiants(SyncedConfiguration config)
        {
            GiantChanceAt100 = config.Bind(Section, "Giant Crop Chance At Level 100", 2f,
                "Chance in percent that a crop ripens into a giant, from the planter's level (half at level 50). A giant is always 3 stars.",
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
