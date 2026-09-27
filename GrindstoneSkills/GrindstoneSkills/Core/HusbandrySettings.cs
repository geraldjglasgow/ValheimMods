using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 23: the Husbandry skill's master switch, the keeper range, the Animal lore level and each player's
    /// callout preference, and the order the Husbandry feature settings are bound in. The features share six sections:
    /// Husbandry (this one), Taming, Breeding, Animal Yield, Companions and Husbandry Experience. Gameplay values are
    /// synced and lockable; callouts are each player's own.
    /// </summary>
    public static class HusbandrySettings
    {
        public const string Section = "23 - Husbandry";
        public const string TamingSection = "24 - Taming";
        public const string BreedingSection = "25 - Breeding";
        public const string YieldSection = "26 - Animal Yield";
        public const string CompanionSection = "27 - Companions";
        public const string ExperienceSection = "28 - Husbandry Experience";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> KeeperRange { get; private set; }
        public static ConfigEntry<float> LoreLevel { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Husbandry Enabled", true,
                "Turns every Husbandry feature on or off: taming, breeding, yield, companions, the feeder's eating, animal lore and experience. Off, animals behave exactly as in the game. Levels are kept either way.");
            KeeperRange = config.Bind(Section, "Keeper Range", 30f,
                "Metres. An animal is tended by the best Husbandry level among the players this close to it: that level drives its fed time, breeding, twins, better offspring, growth and produce, and players this close earn experience from it. Taming speed uses the game's own taming range (60 m) instead.",
                acceptableValues: new AcceptableValueRange<float>(5f, 100f));
            LoreLevel = config.Bind(Section, "Animal Lore Level", 20f,
                "Husbandry level from which hovering an animal or an egg shows its timers: fed time left, taming time left, love and pregnancy, herd room, contentment, growing up and hatching. 0 gives it to everyone; above 100 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 101f));
            ShowCallouts = config.Bind(Section, "Show Callouts", true,
                "Shows words like Twins! and Strong offspring! floating above animals near you.", synced: false);
            HusbandryTamingSettings.Initialize(config);
            HusbandryBreedingSettings.Initialize(config);
            HusbandryYieldSettings.Initialize(config);
            HusbandryCompanionSettings.Initialize(config);
            HusbandryExperienceSettings.Initialize(config);
        }
    }
}
