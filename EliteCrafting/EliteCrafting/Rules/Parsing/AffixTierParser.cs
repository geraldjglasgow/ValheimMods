using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads an affix's <c>tiers</c> list: tier 1-7 at most once each (8 is reserved), <c>min</c>/<c>max</c> required
    /// for percent and flat values and refused on flags, weight at least 0. Sorted by tier.
    /// </summary>
    internal static class AffixTierParser
    {
        public const int MaxShippedTier = 7;

        public static List<AffixTierDef> Parse(MapReader r, AffixValueType valueType)
        {
            List<AffixTierDef> tiers = new List<AffixTierDef>();
            YamlSequenceNode? seq = r.Seq("tiers");
            if (seq == null || seq.Children.Count == 0)
            {
                r.Issues.Error(r.At("tiers"), r.Map, "is required: at least one tier");
                return tiers;
            }
            for (int i = 0; i < seq.Children.Count; i++)
            {
                AffixTierDef? tier = ParseRow(seq.Children[i], $"{r.At("tiers")}[{i}]", valueType, r.Issues);
                if (tier != null)
                {
                    AddUnique(tiers, tier, seq.Children[i], r);
                }
            }
            tiers.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            WarnDescending(r, tiers, valueType);
            return tiers;
        }

        private static AffixTierDef? ParseRow(YamlNode node, string path, AffixValueType valueType, RuleIssues issues)
        {
            if (!(node is YamlMappingNode map))
            {
                issues.Error(path, node, "a tier row should look like { tier: 1, min: 2, max: 3 }");
                return null;
            }
            MapReader row = new MapReader(map, path, issues);
            row.Unknown("tier", "min", "max", "weight");
            AffixTierDef tier = new AffixTierDef
            {
                Tier = row.Int("tier", 0, 1, MaxShippedTier),
                Weight = row.Float("weight", 100f, 0f),
            };
            if (!row.Has("tier"))
            {
                row.Error("tier", "is required (1 meadows ... 7 ashlands)");
            }
            return ReadBounds(row, tier, valueType) ? tier : null;
        }

        private static bool ReadBounds(MapReader row, AffixTierDef tier, AffixValueType valueType)
        {
            if (valueType == AffixValueType.Flag)
            {
                if (row.Has("min") || row.Has("max"))
                {
                    row.Error("min", "a flag affix has no min or max");
                }
                return true;
            }
            if (!row.Has("min") || !row.Has("max"))
            {
                row.Error("min", "min and max are required");
                return false;
            }
            tier.Min = row.Float("min", 0f);
            tier.Max = row.Float("max", 0f);
            tier.Decimals = Math.Min(2, Math.Max(Numbers.Decimals(row.Str("min") ?? ""), Numbers.Decimals(row.Str("max") ?? "")));
            if (tier.Min > tier.Max)
            {
                row.Error("min", "min is above max");
            }
            return true;
        }

        private static void AddUnique(List<AffixTierDef> tiers, AffixTierDef tier, YamlNode node, MapReader r)
        {
            if (tiers.Exists(t => t.Tier == tier.Tier))
            {
                r.Issues.Error(r.At("tiers"), node, $"tier {tier.Tier} is listed twice");
                return;
            }
            tiers.Add(tier);
        }

        private static void WarnDescending(MapReader r, List<AffixTierDef> tiers, AffixValueType valueType)
        {
            for (int i = 1; valueType != AffixValueType.Flag && i < tiers.Count; i++)
            {
                if (tiers[i].Max < tiers[i - 1].Max)
                {
                    r.Warn("tiers", $"tier {tiers[i].Tier} rolls lower than tier {tiers[i - 1].Tier} (a typo?)");
                    return;
                }
            }
        }
    }
}
