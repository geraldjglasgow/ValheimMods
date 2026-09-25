using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Effects;
using EliteCrafting.Items;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads and checks one merged affix entry. Completeness is checked here, after all layers merged, so a two-line
    /// override of a built-in affix is valid while a new id missing a required field is an error.
    /// </summary>
    internal static class AffixParser
    {
        private static readonly string[] Keys =
        {
            "id", "name", "effect", "param", "value", "unit", "slots", "requires", "category", "mythic_only",
            "condition", "exclusion_group", "weight", "enabled", "hook", "tiers",
        };

        public static AffixDef? Parse(YamlNode node, RuleIssues issues)
        {
            if (!(node is YamlMappingNode map))
            {
                issues.Error("affixes", node, "an affix entry should be a block of keyed values");
                return null;
            }
            string? id = new MapReader(map, "affixes[?]", issues).Id("id");
            if (id == null)
            {
                issues.Error("affixes[?]", map, "an affix entry has no valid 'id'");
                return null;
            }
            MapReader r = new MapReader(map, $"affixes[{id}]", issues);
            r.Unknown(Keys);
            AffixDef def = new AffixDef { Id = id, Name = r.Str("name") ?? "$ecf_affix_" + id };
            ReadEffect(r, def);
            ReadPlacement(r, def);
            ReadDrawing(r, def);
            def.Tiers = AffixTierParser.Parse(r, def.Value);
            return def;
        }

        private static void ReadEffect(MapReader r, AffixDef def)
        {
            def.Effect = Required(r, "effect") ?? "";
            def.Param = r.Str("param");
            def.Value = r.Enum("value", AffixValueType.Percent);
            Required(r, "value");
            def.Unit = r.Enum("unit", AffixUnit.None);
            if (def.Unit != AffixUnit.None && def.Value != AffixValueType.Flat)
            {
                r.Error("unit", "a unit is only for flat values");
            }
            CheckEffect(r, def);
        }

        private static void CheckEffect(MapReader r, AffixDef def)
        {
            if (def.Effect.Length == 0)
            {
                return;
            }
            if (!EffectRegistry.TryGet(def.Effect, out EffectDef effect))
            {
                r.Error("effect", $"'{def.Effect}' is not an effect this version implements");
                return;
            }
            def.EffectDef = effect;
            if (!effect.Accepts(def.Value))
            {
                r.Error("value", $"effect '{def.Effect}' does not take {EnumIds<AffixValueType>.Id(def.Value)} values");
            }
            string? paramError = ParamKinds.Apply(def, effect);
            if (paramError != null)
            {
                r.Error("param", paramError);
            }
        }

        private static void ReadPlacement(MapReader r, AffixDef def)
        {
            def.Slots = ReadSlots(r);
            def.Requires = RequirementsParser.Parse(r);
            def.Category = r.Enum("category", AffixCategory.Utility);
            Required(r, "category");
            def.MythicOnly = r.Bool("mythic_only", false);
            def.Condition = r.Enum("condition", AffixCondition.None);
        }

        private static void ReadDrawing(MapReader r, AffixDef def)
        {
            def.ExclusionGroup = r.Id("exclusion_group");
            def.Weight = r.Float("weight", 100f, 0f);
            def.Enabled = r.Bool("enabled", true);
            def.Hook = r.Enum("hook", HookDifficulty.None);
        }

        private static List<ItemSlot> ReadSlots(MapReader r)
        {
            List<ItemSlot> slots = new List<ItemSlot>();
            List<string>? ids = r.Strings("slots");
            if (ids == null || ids.Count == 0)
            {
                r.Error("slots", "is required: at least one slot");
                return slots;
            }
            foreach (string id in ids)
            {
                if (ItemSlots.TryParse(id, out ItemSlot slot))
                {
                    slots.Add(slot);
                }
                else
                {
                    r.Error("slots", $"'{id}' is not a slot ({SlotList})");
                }
            }
            return slots;
        }

        private static string SlotList =>
            "melee_weapon, ranged_weapon, magic_weapon, shield, head, chest, legs, cape, utility_item, tool";

        private static string? Required(MapReader r, string key)
        {
            string? value = r.Str(key);
            if (string.IsNullOrEmpty(value))
            {
                r.Issues.Error(r.At(key), r.Map, "is required");
                return null;
            }
            return value;
        }
    }
}
