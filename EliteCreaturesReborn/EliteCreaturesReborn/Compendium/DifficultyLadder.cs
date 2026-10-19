using System.Text;
using EliteCreaturesReborn.Rules;
using Translations;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The five difficulties side by side, one line each, the server's own in orange. The words carry the presets' agreed
    /// numbers (<see cref="PresetCaps"/> and <c>PresetMutations</c>, features/difficulty.md): the stars in the biome being
    /// entered and how far they climb, and the mutation rate there and the ceiling it climbs toward.
    /// </summary>
    internal static class DifficultyLadder
    {
        private static readonly Difficulty[] Order =
            { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.VeryHard, Difficulty.Extreme };

        public static void Append(StringBuilder text, Difficulty current)
        {
            PageText.Heading(text, Lang.Get("ecr_book_ladder"));
            foreach (Difficulty difficulty in Order)
            {
                string line = Lang.Format("ecr_book_ladder_line", DifficultyNames.Shown(difficulty), Words(difficulty));
                text.Append(difficulty == current ? $"<color=orange>{line}</color>" : line).Append('\n');
            }
            text.Append(current == Difficulty.Custom
                ? Lang.Format("ecr_book_ladder_custom", DifficultyNames.Shown(Difficulty.Custom)) + "\n"
                : PageText.Grey(Lang.Get("ecr_book_ladder_note")) + "\n");
        }

        /// <summary>The difficulty's line: <c>ecr_book_ladder_easy</c> ... <c>ecr_book_ladder_extreme</c>. Very Hard's
        /// names two others: Hard is <c>$1</c>, Extreme <c>$2</c>.</summary>
        private static string Words(Difficulty difficulty) =>
            Lang.Format("ecr_book_ladder_" + difficulty.ToString().ToLowerInvariant(),
                DifficultyNames.Shown(Difficulty.Hard), DifficultyNames.Shown(Difficulty.Extreme));
    }
}
