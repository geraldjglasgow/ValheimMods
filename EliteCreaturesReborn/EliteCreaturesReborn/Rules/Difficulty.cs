namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The .cfg's Difficulty (features/difficulty.md). <see cref="Custom"/> is the rule file's own per-biome
    /// star and mutation rows with the world tier's boosts, and what a rule file with no `difficulty:` line moved over
    /// as, so an existing server keeps playing as before. The five presets replace those rows with one shape: a biome's star cap, star odds and
    /// mutation rate follow how many bosses the world has killed and how many of them came after that biome.
    /// </summary>
    public enum Difficulty
    {
        Custom,
        Easy,
        Medium,
        Hard,
        VeryHard,
        Extreme,
    }

    /// <summary>The words a rule file and the console use for a difficulty, and the name a player reads.</summary>
    public static class DifficultyNames
    {
        public const string Choices = "Easy, Medium, Hard, Very Hard, Extreme or Custom";

        /// <summary>Case, spaces, dashes and underscores are ignored: <c>very hard</c>, <c>VeryHard</c>, <c>very_hard</c>.</summary>
        public static bool TryParse(string? text, out Difficulty difficulty)
        {
            string key = (text ?? "").Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            switch (key)
            {
                case "custom": difficulty = Difficulty.Custom; return true;
                case "easy": difficulty = Difficulty.Easy; return true;
                case "medium": difficulty = Difficulty.Medium; return true;
                case "hard": difficulty = Difficulty.Hard; return true;
                case "veryhard": difficulty = Difficulty.VeryHard; return true;
                case "extreme": difficulty = Difficulty.Extreme; return true;
                default: difficulty = Difficulty.Custom; return false;
            }
        }

        /// <summary>The console's and the log's word for it, always English: "Very Hard".</summary>
        public static string Name(Difficulty difficulty) => difficulty == Difficulty.VeryHard ? "Very Hard" : difficulty.ToString();

        /// <summary>Its name in the player's language, for the compendium (<c>ecr_difficulty_&lt;difficulty&gt;</c>).</summary>
        public static string Shown(Difficulty difficulty) =>
            Translations.Lang.Get("ecr_difficulty_" + difficulty.ToString().ToLowerInvariant());
    }
}
