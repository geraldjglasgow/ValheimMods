using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 3: the odds of each star count by effective level (the cook's Cooking level plus the ingredient bonus).
    /// One row per level; odds blend linearly between rows. Rows above 100 are reached only with starred ingredients.
    /// Synced.
    /// </summary>
    public static class OddsSettings
    {
        public const string Section = "3 - Odds";

        /// <summary>The effective level of each row, ascending.</summary>
        public static readonly float[] Levels = { 0f, 25f, 50f, 75f, 100f, 130f };

        private static readonly string[] Defaults =
        {
            "90, 10, 0, 0", "45, 40, 15, 0", "15, 40, 35, 10", "5, 20, 45, 30", "0, 10, 40, 50", "0, 0, 25, 75",
        };

        /// <summary>The raw rows, one per entry of <see cref="Levels"/>.</summary>
        public static ConfigEntry<string>[] Rows { get; } = new ConfigEntry<string>[Levels.Length];

        public static ConfigEntry<float> IngredientLevelsPerStar { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            for (int i = 0; i < Levels.Length; i++)
                Rows[i] = config.Bind(Section, $"Odds At Level {Levels[i]:0}", Defaults[i],
                    "Chances in percent of 0, 1, 2 and 3 stars at this effective level, separated by commas. " +
                    "They are scaled to add up to 100. A row that cannot be read uses its default.");
            IngredientLevelsPerStar = config.Bind(Section, "Ingredient Levels Per Star", 10f,
                "Levels added to the cook's level for each star, on average, of the ingredients that can carry stars.",
                acceptableValues: Settings.UpTo(50f));
        }

        /// <summary>The default text of a row, used when the configured text cannot be read.</summary>
        public static string DefaultRow(int index) => Defaults[index];
    }
}
