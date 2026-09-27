using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Clean fell and Replanting (section 11): the chance that a felled tree takes its stump with it, and the chance that
    /// a sapling of the same kind takes root where it stood, each growing linearly from nothing at level 0 to its value
    /// at level 100. The feller's level counts, chain fells included. Synced.
    /// </summary>
    public static class FellPerkSettings
    {
        public const string Section = WoodcuttingSettings.FellingSection;

        /// <summary>The largest chance accepted, in percent: every time.</summary>
        public const float MaxChance = 100f;

        public static ConfigEntry<float> CleanFellAt100 { get; private set; }
        public static ConfigEntry<float> ReplantingAt100 { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            CleanFellAt100 = config.Bind(Section, "Clean Fell At 100", 100f,
                "Percent chance that a tree felled by a level 100 woodcutter takes its stump with it. The stump drops its wood as if you had chopped it.",
                acceptableValues: Settings.UpTo(MaxChance));
            ReplantingAt100 = config.Bind(Section, "Replanting At 100", 50f,
                "Percent chance that a sapling of the same kind takes root, free, where a level 100 woodcutter felled a tree; the stump comes out too. Only for trees the game has a sapling for, and only where that sapling can grow.",
                acceptableValues: Settings.UpTo(MaxChance));
        }
    }
}
