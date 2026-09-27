using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 13: how often a felled tree hides a find, and the Finds YAML file (GrindstoneSkills.Finds*.yml, see
    /// <see cref="FindFile"/>) that says what it hides per biome or per tree. The chance grows linearly from its value
    /// at level 0 to its value at level 100. Synced, like the file.
    /// </summary>
    public static class FindSettings
    {
        public const string Section = WoodcuttingSettings.FindsSection;

        public static ConfigEntry<float> ChanceAt0 { get; private set; }
        public static ConfigEntry<float> ChanceAt100 { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ChanceAt0 = config.Bind(Section, "Find Chance At 0", 2f,
                "Percent chance that a tree felled by a level 0 woodcutter hides a find from GrindstoneSkills.Finds.yml. Domino fells count too. 0 with the next setting at 0 turns finds off.",
                acceptableValues: Settings.UpTo(100f));
            ChanceAt100 = config.Bind(Section, "Find Chance At 100", 15f,
                "Percent chance of a find for a level 100 woodcutter; levels in between are in proportion.",
                acceptableValues: Settings.UpTo(100f));
            FindFile.Register(config);
        }
    }
}
