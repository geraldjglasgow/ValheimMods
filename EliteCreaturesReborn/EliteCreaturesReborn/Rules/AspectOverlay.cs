using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using YamlDotNet.RepresentationModel;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Reads the `aspects:` block under `bosses:` onto the aspect defaults, in the same shape as the other overlays: only
    /// the keys the block names change, an aspect name the mod does not know is reported against its line, and a value
    /// that will not parse keeps its default rather than throwing.
    /// </summary>
    internal static class AspectOverlay
    {
        public static void Apply(AspectRules rules, YamlMappingNode block, List<string> errors)
        {
            rules.Enabled = YamlRead.Bool(block, Fields.Enabled, rules.Enabled, errors);
            rules.ShiftHours = NonNegative(block, Fields.ShiftHours, rules.ShiftHours, errors);
            ReadWeights(rules.Chances, block, Fields.Chances, errors);
            ReadWeights(rules.Loot, block, Fields.Loot, errors);
            ReadPower(rules, block, errors);
            PerBossOverlay.Apply(rules, YamlRead.Child(block, Fields.PerBoss), errors);
        }

        private static float NonNegative(YamlMappingNode block, string key, float fallback, List<string> errors)
        {
            YamlNode? node = YamlRead.Child(block, key);
            if (node == null)
            {
                return fallback;
            }
            if (YamlRead.TryFloat(node, out float value) && value >= 0f)
            {
                return value;
            }
            YamlRead.AddError(errors, node, $"'{key}' should be a number, 0 or more");
            return fallback;
        }

        /// <summary>A block of `aspect: number` lines; each named aspect replaces its default, the rest stay.</summary>
        private static void ReadWeights(Dictionary<Aspect, float> into, YamlMappingNode block, string key, List<string> errors)
        {
            if (!(YamlRead.Child(block, key) is YamlNode node) || !(YamlRead.Map(node, errors, $"'{key}'") is YamlMappingNode map))
            {
                return;
            }
            foreach (KeyValuePair<YamlNode, YamlNode> pair in map.Children)
            {
                Aspect? aspect = Name(pair.Key, errors);
                if (aspect != null && YamlRead.TryFloat(pair.Value, out float value) && value >= 0f)
                {
                    into[aspect.Value] = value;
                }
                else if (aspect != null)
                {
                    YamlRead.AddError(errors, pair.Value, $"'{key}' for {AspectCatalog.Key(aspect.Value)} should be a number, 0 or more");
                }
            }
        }

        private static void ReadPower(AspectRules rules, YamlMappingNode block, List<string> errors)
        {
            if (!(YamlRead.Child(block, Fields.Power) is YamlNode node) || !(YamlRead.Map(node, errors, "'power'") is YamlMappingNode map))
            {
                return;
            }
            foreach (KeyValuePair<YamlNode, YamlNode> pair in map.Children)
            {
                Aspect? aspect = Name(pair.Key, errors);
                if (aspect != null && YamlRead.Map(pair.Value, errors, $"power for {aspect}") is YamlMappingNode fields)
                {
                    ReadFields(rules.Power, aspect.Value, fields, errors);
                }
            }
        }

        private static void ReadFields(Dictionary<Aspect, Dictionary<string, float>> power, Aspect aspect,
            YamlMappingNode fields, List<string> errors)
        {
            if (!power.TryGetValue(aspect, out Dictionary<string, float> into))
            {
                power[aspect] = into = new Dictionary<string, float>();
            }
            foreach (KeyValuePair<YamlNode, YamlNode> pair in fields.Children)
            {
                string field = (pair.Key as YamlScalarNode)?.Value ?? "";
                if (YamlRead.TryFloat(pair.Value, out float value))
                {
                    into[field] = value;
                }
                else
                {
                    YamlRead.AddError(errors, pair.Value, $"{aspect} '{field}' is not a number");
                }
            }
        }

        /// <summary>The aspect a key names, `none` included; an unknown word is reported and skipped.</summary>
        public static Aspect? Name(YamlNode key, List<string> errors)
        {
            string name = (key as YamlScalarNode)?.Value ?? "";
            Aspect? aspect = AspectCatalog.FromName(name);
            if (aspect == null)
            {
                YamlRead.AddError(errors, key, $"'{name}' is not a boss aspect - use none or one of the eight");
            }
            return aspect;
        }
    }
}
