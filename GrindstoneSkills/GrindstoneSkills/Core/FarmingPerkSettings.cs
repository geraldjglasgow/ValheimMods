using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 30: the Farming perks. "At Level 100" perks grow linearly from nothing at level 0; "Level" perks switch on
    /// at that level (set above 100 to turn one off). Growth, space and tolerance follow the planter's level, stored on
    /// the plant; yield, seed return and auto-replant follow the picker's. Rain and tending are the same for everyone.
    /// Synced.
    /// </summary>
    public static class FarmingPerkSettings
    {
        public const string Section = FarmingSettings.PerksSection;

        private static readonly AcceptableValueRange<int> Levels = new AcceptableValueRange<int>(0, 101);

        public static ConfigEntry<float> GrowthSpeedAt100 { get; private set; }
        public static ConfigEntry<float> GrowSpaceAt100 { get; private set; }
        public static ConfigEntry<float> BonusYieldAt100 { get; private set; }
        public static ConfigEntry<float> SeedReturnAt100 { get; private set; }
        public static ConfigEntry<int> AutoReplantLevel { get; private set; }
        public static ConfigEntry<int> RowOfThreeLevel { get; private set; }
        public static ConfigEntry<int> RowOfFiveLevel { get; private set; }
        public static ConfigEntry<int> HeatToleranceLevel { get; private set; }
        public static ConfigEntry<int> ColdToleranceLevel { get; private set; }
        public static ConfigEntry<float> RainBonus { get; private set; }
        public static ConfigEntry<float> TendingBonus { get; private set; }
        public static ConfigEntry<float> TendingRadius { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindGrowth(config);
            BindPicking(config);
            BindMilestones(config);
            BindCare(config);
        }

        private static void BindGrowth(SyncedConfiguration config)
        {
            GrowthSpeedAt100 = config.Bind(Section, "Growth Speed At Level 100", 40f,
                "Percent faster growth for everything a player plants, from the planter's level.", acceptableValues: Settings.UpTo(500f));
            GrowSpaceAt100 = config.Bind(Section, "Grow Space Reduction At Level 100", 40f,
                "Percent less room a crop needs around it to grow, from the planter's level, so crops stand closer.", acceptableValues: Settings.UpTo(80f));
        }

        private static void BindPicking(SyncedConfiguration config)
        {
            BonusYieldAt100 = config.Bind(Section, "Bonus Yield Chance At Level 100", 50f,
                "Chance in percent of the game's bonus crop when picking a crop, from the picker's level. Replaces the game's own 25%.",
                acceptableValues: Settings.UpTo(100f));
            SeedReturnAt100 = config.Bind(Section, "Seed Return Chance At Level 100", 30f,
                "Chance in percent that picking a crop also gives back the seed it grew from, from the picker's level.",
                acceptableValues: Settings.UpTo(100f));
            AutoReplantLevel = config.Bind(Section, "Auto Replant Level", 50,
                "From this level, picking a crop puts its plant back in the same spot with a seed from your inventory (each player can turn it off). 101 turns it off.",
                acceptableValues: Levels);
        }

        private static void BindMilestones(SyncedConfiguration config)
        {
            RowOfThreeLevel = config.Bind(Section, "Row Of Three Level", 25,
                "From this level, placing a seed plants a row of three (each pays its own seed). 101 turns it off.", acceptableValues: Levels);
            RowOfFiveLevel = config.Bind(Section, "Row Of Five Level", 50,
                "From this level, a row of five. 101 turns it off.", acceptableValues: Levels);
            HeatToleranceLevel = config.Bind(Section, "Heat Tolerance Level", 75,
                "From this planter level, crops grow in the Ashlands without a shield. 101 turns it off.", acceptableValues: Levels);
            ColdToleranceLevel = config.Bind(Section, "Cold Tolerance Level", 100,
                "From this planter level, crops that grow in the Meadows also grow in the Mountain and Deep North. 101 turns it off.", acceptableValues: Levels);
        }

        private static void BindCare(SyncedConfiguration config)
        {
            RainBonus = config.Bind(Section, "Rain Growth Bonus", 50f,
                "Percent faster growth while it rains, for everything a player planted.", acceptableValues: Settings.UpTo(500f));
            TendingBonus = config.Bind(Section, "Tending Bonus", 10f,
                "Percent of its grow time a plant gains when tended (the use key on a growing plant), once per in-game day. 0 turns tending off.",
                acceptableValues: Settings.UpTo(100f));
            TendingRadius = config.Bind(Section, "Tending Radius", 2.5f,
                "Tending reaches every growing plant within this many metres of the one you use.", acceptableValues: new AcceptableValueRange<float>(0f, 10f));
        }
    }
}
