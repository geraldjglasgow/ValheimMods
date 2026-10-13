using System.Collections.Generic;
using System.Text;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "Mutations" page: every mutation this server rolls, each with a star in the colour the player's nameplates
    /// draw it in and what it does with the numbers of the biome the player stands in (<see cref="MutationWords"/>), then
    /// the names of those switched off.
    /// </summary>
    internal static class MutationsPage
    {
        public const string Topic = "Elite: Mutations";

        public static string Text(RuleSet set, Heightmap.Biome here)
        {
            BiomeRules rules = set.For(here);
            CreatureTraits plain = new CreatureTraits(0, 0);
            StringBuilder text = new StringBuilder(6144);
            Intro(text, rules, here);
            List<string> off = new List<string>();
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if (set.IsEnabled(mutation))
                {
                    Entry(text, mutation, MutationWords.Describe(mutation, rules, plain));
                }
                else
                {
                    off.Add(MutationCatalog.Word(mutation));
                }
            }
            Off(text, off);
            return text.ToString();
        }

        private static void Off(StringBuilder text, List<string> off)
        {
            if (off.Count > 0)
            {
                PageText.Heading(text, "Off on this server");
                text.Append(string.Join(", ", off)).Append('\n');
            }
        }

        private static void Intro(StringBuilder text, BiomeRules rules, Heightmap.Biome here)
        {
            text.Append("A mutated creature wears its mutation's name before its own, and its stars take the mutation's "
                + "colour, the colour of the star before each name below. Gilded is the rarest and the only one in your "
                + "favour.\n");
            string large = Mathf.Approximately(rules.LargeStarPower, 1f) ? ""
                : $" A mutation on a large star is stronger: what it gains is x{rules.LargeStarPower:0.##}.";
            text.Append(PageText.Grey($"Numbers for a creature with no stars in the {PageText.BiomeName(here)}, where you "
                + "are." + large)).Append('\n');
        }

        private static void Entry(StringBuilder text, Mutation mutation, string words)
        {
            string colour = ColorUtility.ToHtmlStringRGB(PaletteSettings.Of(mutation));
            text.Append($"\n<color=#{colour}>{PageText.Star}</color> ")
                .Append($"<color=orange>{MutationCatalog.Word(mutation)}</color>\n").Append(words).Append('\n');
        }
    }
}
