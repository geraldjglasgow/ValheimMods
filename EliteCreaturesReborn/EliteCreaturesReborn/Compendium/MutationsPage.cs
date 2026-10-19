using System.Collections.Generic;
using System.Text;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using Translations;
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
        public static string Topic => Lang.Get("ecr_book_mutations_topic");

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
                    off.Add(Name(mutation));
                }
            }
            Off(text, off);
            return text.ToString();
        }

        /// <summary>The mutation's name in the player's language (<c>ecr_mut_&lt;mutation&gt;</c>).</summary>
        public static string Name(Mutation mutation) => Lang.Get("ecr_mut_" + mutation.ToString().ToLowerInvariant());

        private static void Off(StringBuilder text, List<string> off)
        {
            if (off.Count > 0)
            {
                PageText.Heading(text, Lang.Get("ecr_book_mutations_off"));
                text.Append(string.Join(Lang.Get("ecr_book_list_comma"), off)).Append('\n');
            }
        }

        private static void Intro(StringBuilder text, BiomeRules rules, Heightmap.Biome here)
        {
            text.Append(Lang.Format("ecr_book_mutations_intro", Name(Mutation.Gilded))).Append('\n');
            string large = Mathf.Approximately(rules.LargeStarPower, 1f) ? ""
                : " " + Lang.Format("ecr_book_mutations_large", rules.LargeStarPower.ToString("0.##"));
            text.Append(PageText.Grey(Lang.Format("ecr_book_mutations_numbers", PageText.BiomeName(here)) + large))
                .Append('\n');
        }

        private static void Entry(StringBuilder text, Mutation mutation, string words)
        {
            string colour = ColorUtility.ToHtmlStringRGB(PaletteSettings.Of(mutation));
            text.Append($"\n<color=#{colour}>{PageText.Star}</color> ")
                .Append($"<color=orange>{Name(mutation)}</color>\n").Append(words).Append('\n');
        }
    }
}
