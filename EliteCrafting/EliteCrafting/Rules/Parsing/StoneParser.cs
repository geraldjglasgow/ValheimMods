using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads <c>runes:</c> (economy-yaml.md section 4), the seven runes: the common fields here, the Serpent Rune's in
    /// <see cref="StoneVerbParser"/>. Only the seven built-in ids exist; each uses its own prefab.
    /// </summary>
    internal static class StoneParser
    {
        private static readonly string[] Keys =
        {
            "id", "prefab", "name", "description", "verb", "applies_to", "cost", "tier_floor", "enabled", "confirm", "stack",
            "item_weight", "tint", "outcomes", "overflow",
        };

        public static List<StoneDef> Parse(MapReader root)
        {
            List<StoneDef> stones = new List<StoneDef>();
            YamlSequenceNode? seq = root.Seq("runes");
            foreach (YamlNode node in seq?.Children ?? new List<YamlNode>())
            {
                StoneDef? stone = ParseOne(node, root.Issues);
                if (stone != null)
                {
                    stones.Add(stone);
                }
            }
            return stones;
        }

        private static StoneDef? ParseOne(YamlNode node, RuleIssues issues)
        {
            string? id = node is YamlMappingNode m ? new MapReader(m, "runes[?]", issues).Id("id") : null;
            if (id == null)
            {
                issues.Error("runes[?]", node, "a rune entry needs a valid 'id'");
                return null;
            }
            MapReader r = new MapReader((YamlMappingNode)node, $"runes[{id}]", issues);
            r.Unknown(Keys);
            StoneDef stone = new StoneDef
            {
                Id = id,
                Name = r.Str("name") ?? "$ecf_stone_" + id,
                Description = r.Str("description") ?? "$ecf_stone_" + id + "_desc",
            };
            ReadCommon(r, stone);
            ReadItem(r, stone);
            StoneVerbParser.Read(r, stone);
            return stone;
        }

        private static void ReadCommon(MapReader r, StoneDef stone)
        {
            stone.Prefab = ReadPrefab(r, stone.Id);
            stone.Verb = r.Enum("verb", StoneVerb.Promote);
            if (!r.Has("verb"))
            {
                r.Issues.Error(r.At("verb"), r.Map, $"is required ({EnumIds<StoneVerb>.Joined})");
            }
            stone.AppliesTo = r.Strings("applies_to") ?? new List<string>();
            if (stone.AppliesTo.Count == 0)
            {
                r.Issues.Error(r.At("applies_to"), r.Map, "is required: the rarities the rune accepts");
            }
            stone.Cost = YamlLists.IntMap(r, "cost", 0, 999);   // 0 = a free stone (applying-stones.md 3)
            stone.TierFloor = r.Int("tier_floor", 0, 1, TierLadder.MaxCount);   // 0 = none
            stone.Enabled = r.Bool("enabled", true);
            stone.Confirm = r.Bool("confirm", false);
        }

        private static void ReadItem(MapReader r, StoneDef stone)
        {
            stone.Stack = r.Int("stack", 50, 1, 9999);
            stone.ItemWeight = r.Float("item_weight", 0.2f, 0f);
            stone.Tint = r.Str("tint");
            if (stone.Tint != null && !Colors.TryParse(stone.Tint, out _))
            {
                r.Error("tint", $"'{stone.Tint}' is not a #RRGGBB color");
            }
        }

        private static string ReadPrefab(MapReader r, string id)
        {
            string? prefab = r.Str("prefab");
            if (!StoneCatalog.IsBuiltIn(id))
            {
                r.Error("id", $"'{id}' is not a rune: the runes are {string.Join(", ", StoneCatalog.BuiltInIds)}");
                return prefab ?? "";
            }
            string own = StoneCatalog.PrefabFor(id);
            if (prefab != null && prefab != own)
            {
                r.Error("prefab", $"a rune uses its own prefab {own}");
            }
            return own;
        }
    }
}
