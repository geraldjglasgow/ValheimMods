using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 34: the Foraging perks. Extra yield grows linearly from 0 at level 0; sweep picking starts at its level
    /// and reaches further as the level grows; a plant picked at its best time rolls its stars higher. Synced.
    /// </summary>
    public static class ForagePerkSettings
    {
        public const string Section = ForagingSettings.PerksSection;

        public static ConfigEntry<float> ExtraYieldAt100 { get; private set; }
        public static ConfigEntry<float> BestTimeLevels { get; private set; }
        public static ConfigEntry<int> SweepLevel { get; private set; }
        public static ConfigEntry<float> SweepRadiusAt100 { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ExtraYieldAt100 = config.Bind(Section, "Extra Yield Chance At 100", 50f,
                "Percent chance at level 100 that a pick gives one more, growing linearly from 0 at level 0. It replaces the game's own 25% for these picks.",
                acceptableValues: Settings.UpTo(100f));
            BestTimeLevels = config.Bind(Section, "Best Time Levels", 20f,
                "Levels added to the picker's Foraging level for the star roll when a plant is picked at its best time (the Forage file's 'best'). Odds rows above level 100 are reached this way.",
                acceptableValues: Settings.UpTo(50f));
            SweepLevel = config.Bind(Section, "Sweep Level", 25,
                "From this Foraging level, picking a plant also picks every plant of the same kind around it. 101 turns sweep picking off.",
                acceptableValues: new AcceptableValueRange<int>(0, 101));
            SweepRadiusAt100 = config.Bind(Section, "Sweep Radius At 100", 4f,
                "How far sweep picking reaches, in metres, at level 100; at lower levels it reaches level / 100 of this (1 m at level 25).",
                acceptableValues: Settings.UpTo(10f));
        }
    }
}
