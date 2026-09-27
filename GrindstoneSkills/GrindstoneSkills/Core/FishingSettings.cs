using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 50: the Fishing module's master switch and each player's callout preference, and the order the Fishing
    /// feature settings are bound in. The features share five sections: Fishing (this one and experience), Fishing Fight
    /// (line tension, strikes, tiring fish, grace), Bites (bite chance, conditions, starred bait, chum, the angler's
    /// senses), Big Fish (big ones and legendary fish) and Catch And Tackle (bonus items, bait saver, snags, cast and
    /// line, fillets). Gameplay values are synced and lockable; callouts are each player's own.
    /// </summary>
    public static class FishingSettings
    {
        public const string Section = "50 - Fishing";
        public const string FightSection = "51 - Fishing Fight";
        public const string BitesSection = "52 - Bites";
        public const string BigFishSection = "53 - Big Fish";
        public const string CatchSection = "54 - Catch And Tackle";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Fishing Enabled", true,
                "Turns every Fishing feature on or off: line tension, strikes, bites, big and legendary fish, snags, the angler's log, experience changes. Off, fishing plays exactly as in the game. Levels are kept either way.");
            ShowCallouts = config.Bind(Section, "Show Callouts", true,
                "Shows words like It's a big one! and finds floating above the water near you.", synced: false);
            FishingExperienceSettings.Initialize(config);
            FishingFightSettings.Initialize(config);
            FishingBiteSettings.Initialize(config);
            FishingBigFishSettings.Initialize(config);
            FishingCatchSettings.Initialize(config);
        }
    }
}
