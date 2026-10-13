using System.Text;
using EliteCreaturesReborn.Rules;

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
            PageText.Heading(text, "The difficulties");
            foreach (Difficulty difficulty in Order)
            {
                string line = $"{DifficultyNames.Name(difficulty)}: {Words(difficulty)}";
                text.Append(difficulty == current ? $"<color=orange>{line}</color>" : line).Append('\n');
            }
            text.Append(current == Difficulty.Custom
                ? "Custom: the server's own chances for each biome, raised by each world tier.\n"
                : PageText.Grey("Stars: the most a creature can have. Mutated: creatures with no stars that carry one.")
                    + "\n");
        }

        private static string Words(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => "1 star where you're entering, one more for every boss killed after a biome, up to 4; "
                + "10% mutated, rising to 35% as bosses fall after a biome",
            Difficulty.Medium => "2 stars, up to 5; 25% mutated, rising to 55%",
            Difficulty.Hard => "3 stars, up to 5; 30% mutated, rising to 65%",
            Difficulty.VeryHard => "4 stars, up to 5; mutations halfway between Hard and Extreme",
            _ => "5 stars in every biome you've reached, 6 to 8 as more bosses fall; 45% mutated, rising to 98%",
        };
    }
}
