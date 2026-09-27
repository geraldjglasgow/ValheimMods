using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 10: the Woodcutting module's master switch and each player's callout preference, and the order the
    /// Woodcutting feature settings are bound in. The features share four sections: Woodcutting (this one and
    /// experience), Felling (Timber!, Domino, Clean fell, Replanting), Chopping (stamina, axe wear, Clean splits, Old
    /// growth) and Finds. Gameplay values are synced and lockable; callouts are each player's own.
    /// </summary>
    public static class WoodcuttingSettings
    {
        public const string Section = "10 - Woodcutting";
        public const string FellingSection = "11 - Felling";
        public const string ChoppingSection = "12 - Chopping";
        public const string FindsSection = "13 - Finds";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Woodcutting Enabled", true,
                "Turns every Woodcutting feature on or off: perks, finds, experience changes. Off, trees and logs behave exactly as in the game. Levels are kept either way.");
            ShowCallouts = config.Bind(Section, "Show Callouts", true,
                "Shows words like Timber!, Clean split! and finds floating above trees and logs near you.", synced: false);
            WoodExperienceSettings.Initialize(config);
            TimberSettings.Initialize(config);
            DominoSettings.Initialize(config);
            FellPerkSettings.Initialize(config);
            SwingPerkSettings.Initialize(config);
            CleanSplitSettings.Initialize(config);
            OldGrowthSettings.Initialize(config);
            FindSettings.Initialize(config);
        }
    }
}
