using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>Localized names for the words of refusal and feedback messages ($1..$3 take finished text).</summary>
    internal static class StoneNames
    {
        public static string Rarity(RarityDef rarity) => Words.Localize(rarity.Name);

        /// <summary>
        /// An affix's name: its definition's name, else <c>$ecf_affix_&lt;id&gt;</c> when a word exists (an orphaned
        /// affix a translation still names), else the raw id (item-data.md section 6).
        /// </summary>
        public static string Affix(RuleSet rules, string? id)
        {
            if (id == null)
            {
                return "";
            }
            AffixDef? def = rules.Affix(id);
            if (def != null)
            {
                return Words.Localize(def.Name);
            }
            string key = "ecf_affix_" + id;
            return Words.Has(key) ? Words.Localize("$" + key) : id;
        }
    }
}
