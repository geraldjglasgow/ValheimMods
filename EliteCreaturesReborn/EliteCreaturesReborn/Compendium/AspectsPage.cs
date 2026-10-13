using System.Text;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "Boss aspects" page: what a boss's aspect is, then every aspect with what it pays and what it does - the very
    /// line the altar shows before a summon (<see cref="AspectText"/>), so the two can never disagree.
    /// </summary>
    internal static class AspectsPage
    {
        public const string Topic = "Elite: Boss aspects";

        public static string Text(RuleSet set)
        {
            StringBuilder text = new StringBuilder(4096);
            AspectRules rules = set.Boss.Aspects;
            if (!rules.Enabled)
            {
                text.Append("Boss aspects are off on this server: every boss fights as the game ships it.\n");
                return text.ToString();
            }
            text.Append("A boss summoned at its altar may come with an aspect: a twist on the fight, named before the "
                + "boss's own name. The altar shows it before you summon, and it changes again from time to time. An "
                + "aspect makes the fight harder and the loot richer.\n");
            foreach (Aspect aspect in AspectCatalog.InOrder)
            {
                Entry(text, aspect, rules);
            }
            return text.ToString();
        }

        private static void Entry(StringBuilder text, Aspect aspect, AspectRules rules)
        {
            float loot = rules.LootOf(aspect);
            string pays = Mathf.Approximately(loot, 1f) ? "" : " " + PageText.Grey($"loot x{loot:0.##}");
            text.Append($"\n<color=orange>{AspectCatalog.Word(aspect)}</color>{pays}\n")
                .Append(Capital(AspectText.Describe(aspect, rules))).Append(".\n");
        }

        // The altar's lines start in lower case, after the aspect's name; here each stands as a sentence of its own.
        private static string Capital(string line) =>
            line.Length == 0 ? line : char.ToUpperInvariant(line[0]) + line.Substring(1);
    }
}
