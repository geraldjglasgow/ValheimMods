using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The economy family's cross-entry work, once per load: id and prefab tables, references between sections
    /// (economy-yaml.md section 9) and the per-tier draw tables the drop code picks from.
    /// </summary>
    internal static class EconomyIndex
    {
        public static void Build(EconomyRules rules, RuleIssues issues)
        {
            Dictionary<string, RarityDef> rarities = new Dictionary<string, RarityDef>(System.StringComparer.Ordinal);
            foreach (RarityDef rarity in rules.Rarities)
            {
                rarities[rarity.Id] = rarity;
                if (rarity.MythicAffixes > 0)
                {
                    rules.MythicRarity = rarity;
                }
            }
            rules.RarityById = rarities;
            IndexStones(rules, issues);
            EconomyChecks.Check(rules, issues);
            SalvageChecks.Check(rules, issues);
            if (!issues.HasErrors)
            {
                BuildTables(rules);
            }
        }

        private static void IndexStones(EconomyRules rules, RuleIssues issues)
        {
            Dictionary<string, StoneDef> byId = new Dictionary<string, StoneDef>(System.StringComparer.Ordinal);
            Dictionary<string, StoneDef> byPrefab = new Dictionary<string, StoneDef>(System.StringComparer.Ordinal);
            foreach (StoneDef stone in rules.Stones)
            {
                byId[stone.Id] = stone;
                if (byPrefab.TryGetValue(stone.Prefab, out StoneDef other))
                {
                    issues.Error($"stones[{stone.Id}].prefab", null, $"{stone.Prefab} is already used by '{other.Id}'");
                    continue;
                }
                byPrefab[stone.Prefab] = stone;
            }
            rules.StoneById = byId;
            rules.StoneByPrefab = byPrefab;
        }

        private static void BuildTables(EconomyRules rules)
        {
            DropRules drops = rules.Drops;
            rules.StoneTables = new WeightedTable<StoneDef>[DropParser.Tiers];
            rules.GearTables = new WeightedTable<RarityDef>[DropParser.Tiers];
            rules.BossGearTables = new WeightedTable<RarityDef>[DropParser.Tiers];
            for (int t = 0; t < DropParser.Tiers; t++)
            {
                rules.StoneTables[t] = WeightedTable<StoneDef>.Build(StoneWeights(rules, t));
                rules.GearTables[t] = WeightedTable<RarityDef>.Build(RarityWeights(rules, drops.RarityWeights, t));
                rules.BossGearTables[t] = WeightedTable<RarityDef>.Build(RarityWeights(rules, drops.BossRarityWeights, t));
            }
        }

        private static IEnumerable<KeyValuePair<StoneDef, float>> StoneWeights(EconomyRules rules, int tierIndex)
        {
            foreach (KeyValuePair<string, float[]> row in rules.Drops.Stones)
            {
                StoneDef? stone = rules.Stone(row.Key);
                if (stone != null && stone.Enabled)
                {
                    yield return new KeyValuePair<StoneDef, float>(stone, row.Value[tierIndex]);
                }
            }
        }

        private static IEnumerable<KeyValuePair<RarityDef, float>> RarityWeights(EconomyRules rules,
            IReadOnlyDictionary<string, float[]> table, int tierIndex)
        {
            foreach (KeyValuePair<string, float[]> row in table)
            {
                RarityDef? rarity = rules.Rarity(row.Key);
                if (rarity != null && !rarity.IsBase)
                {
                    yield return new KeyValuePair<RarityDef, float>(rarity, row.Value[tierIndex] * rarity.DropWeight);
                }
            }
        }
    }
}
