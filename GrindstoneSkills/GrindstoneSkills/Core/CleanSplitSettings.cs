using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Clean splits, in section 12 (Chopping): how often an axe swing at a log splits it at once, and how much more
    /// wood a cleanly split log gives. The chance grows linearly from nothing at level 0. Synced.
    /// </summary>
    public static class CleanSplitSettings
    {
        public const string Section = WoodcuttingSettings.ChoppingSection;

        public static ConfigEntry<float> ChanceAt100 { get; private set; }
        public static ConfigEntry<float> WoodBonus { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ChanceAt100 = config.Bind(Section, "Clean Split Chance At 100", 20f,
                "Percent of axe hits on a log that split it at once, for a level 100 woodcutter. The axe must still be good enough for the wood. 0 turns clean splits off.",
                acceptableValues: Settings.UpTo(100f));
            WoodBonus = config.Bind(Section, "Clean Split Bonus", 50f,
                "Percent more wood from a log that was split cleanly. A whole log split cleanly passes this on to both of its halves.",
                acceptableValues: Settings.UpTo(500f));
        }
    }
}
