using System.Collections.Generic;
using EliteCrafting.Core;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>Reads <c>rarities:</c> (ladder order = list order) and <c>rolling:</c>, with the rarity.md section 7 checks.</summary>
    internal static class RarityParser
    {
        private static readonly string[] Keys = { "id", "name", "color", "glow", "affixes", "mythic_affixes", "drop_weight" };

        public static List<RarityDef> Parse(MapReader root)
        {
            List<RarityDef> rarities = new List<RarityDef>();
            YamlSequenceNode? seq = root.Seq("rarities");
            foreach (YamlNode node in seq?.Children ?? new List<YamlNode>())
            {
                RarityDef? rarity = ParseOne(node, rarities.Count, root.Issues);
                if (rarity != null)
                {
                    rarities.Add(rarity);
                }
            }
            CheckLadder(root, rarities);
            return rarities;
        }

        private static RarityDef? ParseOne(YamlNode node, int index, RuleIssues issues)
        {
            string? id = node is YamlMappingNode m ? new MapReader(m, "rarities[?]", issues).Id("id") : null;
            if (id == null)
            {
                issues.Error("rarities[?]", node, "a rarity entry needs a valid 'id'");
                return null;
            }
            MapReader r = new MapReader((YamlMappingNode)node, $"rarities[{id}]", issues);
            r.Unknown(Keys);
            RarityDef rarity = new RarityDef
            {
                Id = id, Index = index, Name = r.Str("name") ?? "$ecf_rarity_" + id,
                Glow = r.Bool("glow", index > 0), MythicAffixes = r.Int("mythic_affixes", 0, 0),
                DropWeight = r.Float("drop_weight", 1f, 0f),
            };
            ReadColor(r, rarity);
            ReadCounts(r, rarity);
            return rarity;
        }

        private static void ReadColor(MapReader r, RarityDef rarity)
        {
            string? text = r.Str("color");
            if (text == null)
            {
                r.Issues.Error(r.At("color"), r.Map, "is required (#RRGGBB)");
                return;
            }
            if (!Colors.TryParse(text, out Color32 color))
            {
                r.Error("color", $"'{text}' is not a #RRGGBB color");
                return;
            }
            rarity.Color = text.ToUpperInvariant();
            rarity.Color32 = color;
        }

        private static void ReadCounts(MapReader r, RarityDef rarity)
        {
            MapReader? counts = r.Sub("affixes");
            if (counts == null)
            {
                r.Issues.Error(r.At("affixes"), r.Map, "is required: { min: N, max: N }");
                return;
            }
            counts.Value.Unknown("min", "max");
            rarity.MinAffixes = counts.Value.Int("min", 0, 0);
            rarity.MaxAffixes = counts.Value.Int("max", 0, 0);
            if (rarity.MinAffixes > rarity.MaxAffixes)
            {
                counts.Value.Error("min", "min is above max");
            }
            if (rarity.MythicAffixes > rarity.MinAffixes)
            {
                r.Error("mythic_affixes", "cannot be above affixes.min");
            }
        }

        private static void CheckLadder(MapReader root, List<RarityDef> rarities)
        {
            if (rarities.Count < 2)
            {
                root.Error("rarities", "at least two rarities are needed (the base rarity and one magic rarity)");
                return;
            }
            if (rarities[0].MaxAffixes != 0)
            {
                root.Error("rarities", $"the first rarity ('{rarities[0].Id}') is the base rarity: its affixes.max must be 0");
            }
            if (rarities[0].Glow)
            {
                root.Warn("rarities", $"glow on the base rarity '{rarities[0].Id}' is ignored: it never glows");
                rarities[0].Glow = false;
            }
            if (rarities.FindAll(r => r.MythicAffixes > 0).Count > 1)
            {
                root.Error("rarities", "at most one rarity may set mythic_affixes");
            }
        }

        public static RollingSettings ParseRolling(MapReader root)
        {
            MapReader? sub = root.Sub("rolling");
            if (sub == null)
            {
                return new RollingSettings();
            }
            MapReader r = sub.Value;
            r.Unknown("tier_window", "promote_adds_at_least", "count_weights");
            return new RollingSettings
            {
                TierWindow = r.Int("tier_window", 3, 1, 7),
                PromoteAddsAtLeast = r.Int("promote_adds_at_least", 1, 0),
                CountWeights = ReadCountWeights(r),
            };
        }

        private static Dictionary<string, IReadOnlyDictionary<int, float>> ReadCountWeights(MapReader r)
        {
            Dictionary<string, IReadOnlyDictionary<int, float>> result = new Dictionary<string, IReadOnlyDictionary<int, float>>();
            MapReader? sub = r.Sub("count_weights");
            if (sub == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(sub.Value.Map))
            {
                result[pair.Key] = CountRow(sub.Value, pair.Key);
            }
            return result;
        }

        private static Dictionary<int, float> CountRow(MapReader sub, string rarityId)
        {
            Dictionary<int, float> weights = new Dictionary<int, float>();
            foreach (KeyValuePair<string, float> w in YamlLists.FloatMap(sub, rarityId))
            {
                if (Numbers.TryInt(w.Key, out int count) && count >= 0)
                {
                    weights[count] = w.Value;
                }
                else
                {
                    sub.Issues.Error($"{sub.At(rarityId)}.{w.Key}", sub.Node(rarityId), "keys are affix counts (whole numbers)");
                }
            }
            return weights;
        }
    }
}
