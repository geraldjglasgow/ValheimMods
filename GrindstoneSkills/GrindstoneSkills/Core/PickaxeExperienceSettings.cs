using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Pickaxes experience (section 14): the game's own +1 per swing that hits rock, scaled by the hardest rock the swing
    /// hit (its biome, and ore deposits more), plus credits for clean strikes and for the first hit on each kind of ore
    /// deposit. With Experience Per Biome Step and Ore Experience Bonus at 0, Clean Strike and Discovery Experience at 0
    /// and Experience Multiplier 1, a player earns exactly what the game gives. Synced.
    /// </summary>
    public static class PickaxeExperienceSettings
    {
        public const string Section = PickaxeSettings.Section;

        public static ConfigEntry<float> Multiplier { get; private set; }
        public static ConfigEntry<float> BiomeStep { get; private set; }
        public static ConfigEntry<float> OreBonus { get; private set; }
        public static ConfigEntry<float> CleanStrike { get; private set; }
        public static ConfigEntry<float> Discovery { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindSwing(config);
            BindCredits(config);
        }

        private static void BindSwing(SyncedConfiguration config)
        {
            Multiplier = config.Bind(Section, "Experience Multiplier", 1f,
                "Multiplies all Pickaxes experience: swings, clean strikes and discoveries.", acceptableValues: Settings.UpTo(10f));
            BiomeStep = config.Bind(Section, "Experience Per Biome Step", 25f,
                "Percent more experience per swing for each biome step of the rock's biome: Meadows 0, Black Forest 1, Swamp 2, Mountain 3, Plains 4, Mistlands 5, Ashlands and Deep North 6. 25 gives x1.25 in the Black Forest and x2.5 in the Ashlands. Rock too hard for your pickaxe earns no bonus.",
                acceptableValues: Settings.UpTo(200f));
            OreBonus = config.Bind(Section, "Ore Experience Bonus", 50f,
                "Percent more experience per swing when the hardest rock it hit is an ore deposit.", acceptableValues: Settings.UpTo(500f));
        }

        private static void BindCredits(SyncedConfiguration config)
        {
            CleanStrike = config.Bind(Section, "Clean Strike Experience", 1f,
                "Pickaxes experience for each clean strike, times the rock's biome and ore scale. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
            Discovery = config.Bind(Section, "Discovery Experience", 10f,
                "Pickaxes experience for the first hit on each kind of ore deposit, times its biome and ore scale. Each character discovers each kind once. 0 turns it off.",
                acceptableValues: Settings.UpTo(1000f));
        }
    }
}
