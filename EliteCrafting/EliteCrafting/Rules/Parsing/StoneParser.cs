using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads <c>stones:</c> (economy-yaml.md section 4): the common fields here, the verb-specific ones in
    /// <see cref="StoneVerbParser"/>. Prefab binding is checked here: a built-in id uses its own prefab, an
    /// owner-defined id one of the reserved <c>ECF_CustomNN</c>.
    /// </summary>
    internal static class StoneParser
    {
        private static readonly string[] Keys =
        {
            "id", "prefab", "name", "description", "verb", "grade", "applies_to", "cost", "tier_floor", "enabled", "confirm", "stack",
            "item_weight", "tint", "slots", "outcomes", "overflow", "weights", "seal_copy", "max_bound", "step", "cap",
            "steer", "category", "family",
        };

        public static List<StoneDef> Parse(MapReader root)
        {
            List<StoneDef> stones = new List<StoneDef>();
            YamlSequenceNode? seq = root.Seq("stones");
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
            string? id = node is YamlMappingNode m ? new MapReader(m, "stones[?]", issues).Id("id") : null;
            if (id == null)
            {
                issues.Error("stones[?]", node, "a stone entry needs a valid 'id'");
                return null;
            }
            MapReader r = new MapReader((YamlMappingNode)node, $"stones[{id}]", issues);
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
            stone.Grade = r.Enum("grade", StoneGrade.None);
            stone.AppliesTo = r.Strings("applies_to") ?? new List<string>();
            if (stone.AppliesTo.Count == 0)
            {
                r.Issues.Error(r.At("applies_to"), r.Map, "is required: the rarities the stone accepts");
            }
            stone.Cost = YamlLists.IntMap(r, "cost", 0, 999);   // 0 = a free stone (applying-stones.md 3)
            stone.TierFloor = r.Int("tier_floor", 0, 1, 7);
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
            stone.Slots = ReadSlots(r);
        }

        private static string ReadPrefab(MapReader r, string id)
        {
            bool builtIn = StoneCatalog.IsBuiltIn(id);
            string? prefab = r.Str("prefab");
            if (builtIn)
            {
                string own = StoneCatalog.PrefabFor(id);
                if (prefab != null && prefab != own)
                {
                    r.Error("prefab", $"a built-in stone uses its own prefab {own}");
                }
                return own;
            }
            if (prefab == null || !StoneCatalog.IsCustomPrefab(prefab))
            {
                r.Issues.Error(r.At("prefab"), r.Node("prefab") ?? r.Map,
                    "a stone of your own must name one of the reserved prefabs ECF_Custom01 ... ECF_Custom16");
                return prefab ?? "";
            }
            return prefab;
        }

        private static List<ItemSlot> ReadSlots(MapReader r)
        {
            List<ItemSlot> slots = new List<ItemSlot>();
            foreach (string id in r.Strings("slots") ?? new List<string>())
            {
                if (ItemSlots.TryParse(id, out ItemSlot slot))
                {
                    slots.Add(slot);
                }
                else
                {
                    r.Error("slots", $"'{id}' is not a slot");
                }
            }
            return slots;
        }
    }
}
