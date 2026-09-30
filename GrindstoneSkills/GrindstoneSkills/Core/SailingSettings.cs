using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 8: the Sailing skill's perks and experience. Each perk grows linearly from nothing at level 0 to its
    /// value at level 100. Synced.
    /// </summary>
    public static class SailingSettings
    {
        public const string Section = "8 - Sailing";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> ShipHealth { get; private set; }
        public static ConfigEntry<float> ShipSpeed { get; private set; }
        public static ConfigEntry<float> ExploreRadius { get; private set; }
        public static ConfigEntry<float> HelmExperience { get; private set; }
        public static ConfigEntry<float> CrewShare { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Sailing Enabled", true,
                "Turns every Sailing perk, Wind Call, the lookout and Sailing experience on or off. Levels are kept either way.");
            ShipHealth = config.Bind(Section, "Ship Health At 100", 50f,
                "Percent more health for a ship built by a level 100 sailor. Fixed by the builder's level when the ship is placed.",
                acceptableValues: Settings.UpTo(500f));
            ShipSpeed = config.Bind(Section, "Ship Speed At 100", 20f,
                "Percent more top speed, under sail and at the oars, for a ship steered by a level 100 sailor.",
                acceptableValues: Settings.UpTo(200f));
            ExploreRadius = config.Bind(Section, "Exploration Radius At 100", 100f,
                "Percent larger map exploration radius while a level 100 sailor is aboard a ship. The game's radius is 100 m.",
                acceptableValues: Settings.UpTo(500f));
            HelmExperience = config.Bind(Section, "Helm Experience Per Kilometre", 40f,
                "Sailing experience for each kilometre a ship travels while you steer it. 0 turns it off.",
                acceptableValues: Settings.UpTo(1000f));
            CrewShare = config.Bind(Section, "Crew Experience Share", 25f,
                "Percent of the helm's experience earned by everyone else aboard while someone steers. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
        }
    }
}
