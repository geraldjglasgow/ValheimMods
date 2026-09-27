using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 6: what dying costs every skill. The defaults are the game's own (5% of each level, progress toward the
    /// next level lost). Read at the moment of death, so an edited .cfg applies to the next death. Synced.
    /// </summary>
    public static class DeathSettings
    {
        public const string Section = "6 - Death";

        public static ConfigEntry<float> SkillLoss { get; private set; }
        public static ConfigEntry<bool> LoseProgress { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            SkillLoss = config.Bind(Section, "Skill Loss On Death", 5f,
                "Percent of every skill's level lost when you die. The game's own value is 5; 0 keeps every level. " +
                "The world's death penalty modifier still scales it, and a world that resets skills on death still does.",
                acceptableValues: Settings.UpTo(100f));
            LoseProgress = config.Bind(Section, "Lose Progress On Death", true,
                "Whether dying also loses the experience gathered toward each skill's next level, as in the game. Off keeps it.");
        }
    }
}
