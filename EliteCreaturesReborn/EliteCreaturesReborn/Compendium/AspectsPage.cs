using System.Text;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "Boss aspects" page: what a boss's aspect is, then every aspect with what it pays and what it does - the very
    /// line the altar shows before a summon (<see cref="AspectText"/>), so the two can never disagree.
    /// </summary>
    internal static class AspectsPage
    {
        public static string Topic => Lang.Get("ecr_book_aspects_topic");

        public static string Text(RuleSet set)
        {
            StringBuilder text = new StringBuilder(4096);
            AspectRules rules = set.Boss.Aspects;
            if (!rules.Enabled)
            {
                text.Append(Lang.Get("ecr_book_aspects_off")).Append('\n');
                return text.ToString();
            }
            text.Append(Lang.Get("ecr_book_aspects_intro")).Append('\n');
            foreach (Aspect aspect in AspectCatalog.InOrder)
            {
                Entry(text, aspect, rules);
            }
            return text.ToString();
        }

        // The altar's lines start in lower case, after the aspect's name; here each stands as a sentence of its own.
        private static void Entry(StringBuilder text, Aspect aspect, AspectRules rules)
        {
            float loot = rules.LootOf(aspect);
            string pays = Mathf.Approximately(loot, 1f) ? ""
                : " " + PageText.Grey(Lang.Format("ecr_book_loot", loot.ToString("0.##")));
            text.Append($"\n<color=orange>{AspectText.Name(aspect)}</color>{pays}\n")
                .Append(AspectText.Sentence(aspect, rules)).Append('\n');
        }
    }
}
