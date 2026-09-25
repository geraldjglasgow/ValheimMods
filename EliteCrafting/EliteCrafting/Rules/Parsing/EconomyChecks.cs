using System.Collections.Generic;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>The economy validation table (economy-yaml.md section 9) for references between entries and sections.</summary>
    internal static class EconomyChecks
    {
        public static void Check(EconomyRules rules, RuleIssues issues)
        {
            foreach (StoneDef stone in rules.Stones)
            {
                CheckStone(rules, stone, issues);
            }
            CheckStoneNames(rules, issues);
            CheckRows(rules, rules.Drops.Stones.Keys, "drops.stones", issues, stone: true);
            CheckRows(rules, rules.Drops.RarityWeights.Keys, "drops.rarity_weights", issues, stone: false);
            CheckRows(rules, rules.Drops.BossRarityWeights.Keys, "drops.boss_rarity_weights", issues, stone: false);
            CheckBonuses(rules, issues);
            WarnClosedMythic(rules, issues);
        }

        private static void CheckStone(EconomyRules rules, StoneDef stone, RuleIssues issues)
        {
            string path = $"stones[{stone.Id}]";
            RaritiesExist(rules, stone.AppliesTo, path + ".applies_to", issues);
            RaritiesExist(rules, stone.Cost.Keys, path + ".cost", issues);
            RaritiesExist(rules, stone.GambleWeights.Keys, path + ".weights", issues);
            if (!stone.Enabled)
            {
                return;
            }
            if (!Stones.StoneVerbs.IsImplemented(stone.Verb))
            {
                // economy-yaml.md section 9: treated as disabled, so it never drops and refuses as stone_disabled
                issues.Warn(path, null, $"verb '{EnumIds<StoneVerb>.Id(stone.Verb)}' is not in this version: the stone is treated as disabled");
                stone.Enabled = false;
                return;
            }
            bool deadCorrupt = stone.Verb == StoneVerb.Corrupt && AllZero(stone.Outcomes);
            bool deadGamble = stone.Verb == StoneVerb.Gamble && AllZero(stone.GambleWeights.Values);
            if (deadCorrupt || deadGamble)
            {
                issues.Warn(path, null, "every weight is 0: the stone is treated as disabled");
                stone.Enabled = false;
            }
        }

        // IMP-56: the game stacks items by display name alone, so two stones sharing a name would merge into one stack
        // (and one named like a vanilla item would merge into that item's stack and become it).
        private static void CheckStoneNames(EconomyRules rules, RuleIssues issues)
        {
            Dictionary<string, string> seen = new Dictionary<string, string>();
            foreach (StoneDef stone in rules.Stones)
            {
                if (stone.Name.Length == 0)
                {
                    continue;
                }
                if (seen.TryGetValue(stone.Name, out string other))
                {
                    issues.Error($"stones[{stone.Id}].name", null, $"'{stone.Name}' is already the name of '{other}': stones with one name merge into one stack");
                    continue;
                }
                seen[stone.Name] = stone.Id;
                if (stone.Name.StartsWith("$item_", System.StringComparison.Ordinal))
                {
                    issues.Warn($"stones[{stone.Id}].name", null, $"'{stone.Name}' is a game item's name: the stone would stack with that item");
                }
            }
        }

        private static void RaritiesExist(EconomyRules rules, IEnumerable<string> ids, string path, RuleIssues issues)
        {
            foreach (string id in ids)
            {
                if (rules.Rarity(id) == null)
                {
                    issues.Error(path, null, $"'{id}' is not a rarity");
                }
            }
        }

        private static void CheckRows(EconomyRules rules, IEnumerable<string> ids, string path, RuleIssues issues, bool stone)
        {
            foreach (string id in ids)
            {
                bool known = stone ? rules.Stone(id) != null : rules.Rarity(id) != null;
                if (!known)
                {
                    issues.Error($"{path}.{id}", null, stone ? "is not a defined stone" : "is not a rarity");
                }
            }
        }

        private static void CheckBonuses(EconomyRules rules, RuleIssues issues)
        {
            List<KeyValuePair<string, IReadOnlyList<DropBonus>>> all = new List<KeyValuePair<string, IReadOnlyList<DropBonus>>>();
            foreach (KeyValuePair<string, BossDrop> boss in rules.Drops.Bosses)
            {
                all.Add(new KeyValuePair<string, IReadOnlyList<DropBonus>>("drops.bosses." + boss.Key, boss.Value.Bonus));
            }
            AddRows(all, "drops.creatures.", rules.Drops.Creatures);
            AddRows(all, "drops.chests.containers.", rules.Drops.Chests.Containers);
            foreach (KeyValuePair<string, IReadOnlyList<DropBonus>> entry in all)
            {
                foreach (DropBonus bonus in entry.Value)
                {
                    if (rules.Stone(bonus.Stone) == null)
                    {
                        issues.Error(entry.Key + ".bonus", null, $"'{bonus.Stone}' is not a defined stone");
                    }
                }
            }
        }

        private static void AddRows(List<KeyValuePair<string, IReadOnlyList<DropBonus>>> all, string prefix,
            IReadOnlyDictionary<string, CreatureDrop> entries)
        {
            foreach (KeyValuePair<string, CreatureDrop> entry in entries)
            {
                all.Add(new KeyValuePair<string, IReadOnlyList<DropBonus>>(prefix + entry.Key, entry.Value.Bonus));
            }
        }

        private static void WarnClosedMythic(EconomyRules rules, RuleIssues issues)
        {
            RarityDef? mythic = rules.MythicRarity;
            if (mythic == null || mythic.DropWeight > 0f)
            {
                return;
            }
            bool weighted = rules.Drops.RarityWeights.TryGetValue(mythic.Id, out float[] row) && !AllZero(row);
            weighted |= rules.Drops.BossRarityWeights.TryGetValue(mythic.Id, out float[] boss) && !AllZero(boss);
            if (weighted)
            {
                issues.Warn("drops", null, $"'{mythic.Id}' has drop weights but drop_weight 0: it still never drops");
            }
        }

        private static bool AllZero(IEnumerable<float> values)
        {
            foreach (float v in values)
            {
                if (v > 0f)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AllZero(IEnumerable<CorruptWeight> rows)
        {
            foreach (CorruptWeight row in rows)
            {
                if (row.Weight > 0f)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
