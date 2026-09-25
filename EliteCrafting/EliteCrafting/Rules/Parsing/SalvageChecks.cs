using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The economy checks for essences and salvage (essences.md and salvage.md, "Validation"): references from
    /// <c>imbue</c> stones to <c>essence_families</c>, and from <c>salvage</c> to rarities, stones and shards. Base-rarity
    /// yield rows are dropped with a warning (Common never grinds).
    /// </summary>
    internal static class SalvageChecks
    {
        public static void Check(EconomyRules rules, RuleIssues issues)
        {
            CheckImbueStones(rules, issues);
            CheckFragments(rules, issues);
            rules.Salvage.Yields = CheckYields(rules, issues);
        }

        private static void CheckImbueStones(EconomyRules rules, RuleIssues issues)
        {
            RarityDef? baseRarity = rules.BaseRarity;
            foreach (StoneDef stone in rules.Stones)
            {
                if (stone.Verb != StoneVerb.Imbue)
                {
                    continue;
                }
                string path = $"stones[{stone.Id}]";
                if (stone.Family != null && rules.Family(stone.Family) == null)
                {
                    issues.Error(path + ".family", null, $"'{stone.Family}' is not an essence_families entry");
                }
                if (baseRarity != null && stone.AppliesToRarity(baseRarity.Id))
                {
                    issues.Error(path + ".applies_to", null, $"an imbue stone cannot apply to '{baseRarity.Id}': there is nothing to reroll (ESS-8)");
                }
            }
        }

        private static void CheckFragments(EconomyRules rules, RuleIssues issues)
        {
            HashSet<string> names = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (StoneDef stone in rules.Stones)
            {
                names.Add(stone.Name);
            }
            foreach (FragmentDef fragment in rules.Salvage.Fragments)
            {
                string path = $"salvage.fragments[{fragment.Id}]";
                if (fragment.Stone.Length > 0 && rules.Stone(fragment.Stone) == null)
                {
                    issues.Error(path + ".stone", null, $"'{fragment.Stone}' is not a defined stone");
                }
                if (!names.Add(fragment.Name))
                {
                    issues.Error(path + ".name", null, $"'{fragment.Name}' is already the name of a stone or shard: they would merge into one stack");
                }
                if (fragment.Fuse > fragment.Stack)
                {
                    issues.Warn(path + ".fuse", null, $"fuse {fragment.Fuse} is above the stack of {fragment.Stack}: one stack can never hold enough to fuse");
                }
            }
        }

        private static Dictionary<string, IReadOnlyList<SalvageYield>> CheckYields(EconomyRules rules, RuleIssues issues)
        {
            Dictionary<string, IReadOnlyList<SalvageYield>> kept = new Dictionary<string, IReadOnlyList<SalvageYield>>(System.StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<SalvageYield>> entry in rules.Salvage.Yields)
            {
                string path = "salvage.yields." + entry.Key;
                RarityDef? rarity = rules.Rarity(entry.Key);
                if (rarity == null)
                {
                    issues.Error(path, null, "is not a rarity");
                    continue;
                }
                if (rarity.IsBase)
                {
                    issues.Warn(path, null, "the base rarity never grinds: ignored");
                    continue;
                }
                CheckRows(rules.Salvage, entry.Value, path, issues);
                kept[entry.Key] = entry.Value;
            }
            return kept;
        }

        private static void CheckRows(SalvageRules salvage, IReadOnlyList<SalvageYield> rows, string path, RuleIssues issues)
        {
            foreach (SalvageYield row in rows)
            {
                FragmentDef? fragment = salvage.Fragment(row.Fragment);
                if (fragment == null)
                {
                    issues.Error(path, null, $"'{row.Fragment}' is not a defined fragment");
                }
                else if (row.Amount > fragment.Stack)
                {
                    issues.Warn(path, null, $"amount {row.Amount} is above the {fragment.Id} stack of {fragment.Stack}: grinding needs a partial stack or several free slots");
                }
            }
        }
    }
}
