using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Woodcutting experience (section 10): the game's own +1 per swing that hits wood, scaled by the hardest wood the
    /// swing could cut, plus credits for felling trees and breaking logs into wood, and a first-fell discovery bonus.
    /// With Tier Scaling off, Small Wood Experience 100, Fell and Split Experience 0 and Experience Multiplier 1, a
    /// player earns exactly what the game gives. Synced.
    /// </summary>
    public static class WoodExperienceSettings
    {
        public const string Section = WoodcuttingSettings.Section;

        public static ConfigEntry<float> Multiplier { get; private set; }
        public static ConfigEntry<bool> TierScaling { get; private set; }
        public static ConfigEntry<float> TierExperience { get; private set; }
        public static ConfigEntry<float> SmallWoodHealth { get; private set; }
        public static ConfigEntry<float> SmallWoodExperience { get; private set; }
        public static ConfigEntry<float> FellExperience { get; private set; }
        public static ConfigEntry<float> SplitExperience { get; private set; }
        public static ConfigEntry<float> DiscoveryMultiplier { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindSwing(config);
            BindCredits(config);
        }

        private static void BindSwing(SyncedConfiguration config)
        {
            Multiplier = config.Bind(Section, "Experience Multiplier", 1f,
                "Multiplies all Woodcutting experience: swings, felled trees and logs broken into wood.", acceptableValues: Settings.UpTo(10f));
            TierScaling = config.Bind(Section, "Tier Scaling", true,
                "Harder wood teaches more: swings, fells and splits earn more for wood that needs a better axe (birch and oak, Yggdrasil).");
            TierExperience = config.Bind(Section, "Tier Experience Per Tool Tier", 50f,
                "Percent more experience for each tool tier the wood needs, while Tier Scaling is on. 50 doubles it for birch and oak and triples it for Yggdrasil shoots. Wood too hard for your axe earns no tier bonus.",
                acceptableValues: Settings.UpTo(500f));
            SmallWoodHealth = config.Bind(Section, "Small Wood Health", 30f,
                "Wood with less health than this (saplings, small trees) counts as small wood. 0 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 200f));
            SmallWoodExperience = config.Bind(Section, "Small Wood Experience", 25f,
                "Percent of the experience a swing earns when the hardest wood it hit is small wood, so saplings are no experience farm.",
                acceptableValues: Settings.UpTo(100f));
        }

        private static void BindCredits(SyncedConfiguration config)
        {
            FellExperience = config.Bind(Section, "Fell Experience", 5f,
                "Woodcutting experience for each tree you fell, trees knocked over by your falling logs included. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
            SplitExperience = config.Bind(Section, "Split Experience", 2f,
                "Woodcutting experience for each log you break into wood. 0 turns it off.", acceptableValues: Settings.UpTo(100f));
            DiscoveryMultiplier = config.Bind(Section, "Discovery Multiplier", 3f,
                "Multiplies the fell experience the first time a character fells each kind of tree.",
                acceptableValues: new AcceptableValueRange<float>(1f, 10f));
        }
    }
}
