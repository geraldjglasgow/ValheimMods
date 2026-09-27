using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 5: Cooking experience on top of the game's own amounts. With Tier Scaling off, Discovery Multiplier 1
    /// and Experience Multiplier 1, a player earns exactly what the game gives (plus the trash and fermenter credit).
    /// Synced.
    /// </summary>
    public static class ExperienceSettings
    {
        public const string Section = "5 - Experience";

        public static ConfigEntry<float> Multiplier { get; private set; }
        public static ConfigEntry<bool> TierScaling { get; private set; }
        public static ConfigEntry<float> TierReferenceValue { get; private set; }
        public static ConfigEntry<float> TierMaximum { get; private set; }
        public static ConfigEntry<float> DiscoveryMultiplier { get; private set; }
        public static ConfigEntry<float> FermenterTap { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Multiplier = config.Bind(Section, "Experience Multiplier", 1f,
                "Multiplies all Cooking experience earned at kitchens.", acceptableValues: Settings.UpTo(10f));
            TierScaling = config.Bind(Section, "Tier Scaling", true,
                "Richer dishes teach more: experience is multiplied by the dish's health + stamina + eitr divided by Tier Reference Food Value, never below 1.");
            TierReferenceValue = config.Bind(Section, "Tier Reference Food Value", 50f,
                "The food value (health + stamina + eitr) that earns the game's own experience.", acceptableValues: new AcceptableValueRange<float>(1f, 500f));
            TierMaximum = config.Bind(Section, "Tier Maximum Multiplier", 3f,
                "The most Tier Scaling can multiply experience by.", acceptableValues: new AcceptableValueRange<float>(1f, 10f));
            DiscoveryMultiplier = config.Bind(Section, "Discovery Multiplier", 3f,
                "Experience multiplier the first time a character makes each dish.", acceptableValues: new AcceptableValueRange<float>(1f, 10f));
            FermenterTap = config.Bind(Section, "Fermenter Tap Experience", 1f,
                "Cooking experience for tapping a fermenter (the game gives none). 0 turns it off.", acceptableValues: Settings.UpTo(10f));
        }
    }
}
