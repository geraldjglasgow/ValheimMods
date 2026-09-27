using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 31: Farming experience on top of the game's own 1 per planting and 1 per crop picked. With Tier Scaling
    /// off, Discovery and Giant Crop Multiplier 1, Tending Experience 0 and Experience Multiplier 1, a player earns
    /// exactly what the game gives. Synced.
    /// </summary>
    public static class FarmingExperienceSettings
    {
        public const string Section = FarmingSettings.ExperienceSection;

        public static ConfigEntry<float> Multiplier { get; private set; }
        public static ConfigEntry<bool> TierScaling { get; private set; }
        public static ConfigEntry<float> TierReferenceValue { get; private set; }
        public static ConfigEntry<float> TierMaximum { get; private set; }
        public static ConfigEntry<float> DiscoveryMultiplier { get; private set; }
        public static ConfigEntry<float> GiantMultiplier { get; private set; }
        public static ConfigEntry<float> TendingExperience { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindScaling(config);
            BindBonuses(config);
        }

        private static void BindScaling(SyncedConfiguration config)
        {
            Multiplier = config.Bind(Section, "Experience Multiplier", 1f,
                "Multiplies all Farming experience from planting and picking crops.", acceptableValues: Settings.UpTo(10f));
            TierScaling = config.Bind(Section, "Tier Scaling", true,
                "Richer crops teach more: experience is multiplied by the crop's value divided by Tier Reference Value, never below 1. A crop's value is its food value, or that of what it is milled into or cooked into.");
            TierReferenceValue = config.Bind(Section, "Tier Reference Value", 30f,
                "The crop value (health + stamina + eitr) that earns the game's own experience.", acceptableValues: new AcceptableValueRange<float>(1f, 500f));
            TierMaximum = config.Bind(Section, "Tier Maximum Multiplier", 3f,
                "The most Tier Scaling can multiply experience by.", acceptableValues: new AcceptableValueRange<float>(1f, 10f));
        }

        private static void BindBonuses(SyncedConfiguration config)
        {
            DiscoveryMultiplier = config.Bind(Section, "Discovery Multiplier", 3f,
                "Experience multiplier the first time a character picks each kind of crop.", acceptableValues: new AcceptableValueRange<float>(1f, 10f));
            GiantMultiplier = config.Bind(Section, "Giant Crop Multiplier", 5f,
                "Experience multiplier for picking a giant crop.", acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            TendingExperience = config.Bind(Section, "Tending Experience", 0.25f,
                "Farming experience for each plant you tend. 0 turns it off.", acceptableValues: Settings.UpTo(5f));
        }
    }
}
