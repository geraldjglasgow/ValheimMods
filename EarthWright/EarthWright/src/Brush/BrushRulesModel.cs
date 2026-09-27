using System;
using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;
using YamlConfig;

namespace EarthWright.Brush
{
    /// <summary>
    /// What EarthWright.Brushes.yml says about one entry or one tool family. Every value is optional; a missing value
    /// falls through to the family, then to the .cfg and the entry's own (vanilla) values.
    /// </summary>
    public sealed class EntryRule
    {
        public static readonly EntryRule Empty = new EntryRule();

        public float? Radius;
        public float? MinRadius;
        public float? MaxRadius;
        public float? Amount;
        public float? MaxStep;
        public float? Strength;
        public float? Hardness;
        public LevelStyle? Style;
        public BrushShape? Shape;
        public bool? Resizable;
    }

    /// <summary>
    /// The parsed EarthWright.Brushes*.yml files: a <c>families:</c> map (hoe, cultivator, shovel, modded) and an
    /// <c>entries:</c> map by piece prefab name, each with the keys of <see cref="EntryRule"/>. Out-of-range values
    /// are errors, so a broken file never applies half-read.
    /// </summary>
    public sealed class BrushRulesModel : YamlModel
    {
        public readonly Dictionary<string, EntryRule> EntryRules = new Dictionary<string, EntryRule>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<ToolFamily, EntryRule> FamilyRules = new Dictionary<ToolFamily, EntryRule>();

        protected override void Read(YamlNode root)
        {
            foreach (KeyValuePair<string, YamlNode> family in MapOf(root.Get("families")))
            {
                if (Enum.TryParse(family.Key.Trim(), true, out ToolFamily kind) && Enum.IsDefined(typeof(ToolFamily), kind))
                    FamilyRules[kind] = ReadRule(family.Value);
                else
                    family.Value.Warn("unknown tool family '" + family.Key + "' (hoe, cultivator, shovel or modded), ignored");
            }
            foreach (KeyValuePair<string, YamlNode> entry in MapOf(root.Get("entries")))
                EntryRules[entry.Key.Trim()] = ReadRule(entry.Value);
        }

        private static IEnumerable<KeyValuePair<string, YamlNode>> MapOf(YamlNode node)
        {
            if (node.Kind == YamlNodeKind.Null || node.Kind == YamlNodeKind.Missing)
                return Array.Empty<KeyValuePair<string, YamlNode>>();
            return node.Entries;
        }

        private static EntryRule ReadRule(YamlNode node)
        {
            if (node.Kind == YamlNodeKind.Null)
                return EntryRule.Empty;
            EntryRule rule = new EntryRule
            {
                Radius = Positive(node.Get("radius"), 100f),
                MinRadius = Positive(node.Get("minRadius"), 100f),
                MaxRadius = Positive(node.Get("maxRadius"), 100f),
                Amount = Positive(node.Get("amount"), 50f),
                MaxStep = Positive(node.Get("maxStep"), 1000f),
                Strength = Share(node.Get("strength")),
                Hardness = Share(node.Get("hardness")),
            };
            ReadChoices(node, rule);
            if (rule.MinRadius.HasValue && rule.MaxRadius.HasValue && rule.MinRadius.Value > rule.MaxRadius.Value)
                node.Error("minRadius is larger than maxRadius");
            return rule;
        }

        private static void ReadChoices(YamlNode node, EntryRule rule)
        {
            if (node.Get("style").TryEnum(out LevelStyle style))
                rule.Style = style;
            if (node.Get("shape").TryEnum(out BrushShape shape))
                rule.Shape = shape;
            if (node.Get("resizable").TryBool(out bool resizable))
                rule.Resizable = resizable;
        }

        private static float? Positive(YamlNode node, float max)
        {
            if (!node.TryFloat(out float value))
                return null;
            if (value > 0f && value <= max)
                return value;
            node.Error($"must be more than 0 and at most {max}");
            return null;
        }

        private static float? Share(YamlNode node)
        {
            if (!node.TryFloat(out float value))
                return null;
            if (value >= 0f && value <= 1f)
                return value;
            node.Error("must be between 0 and 1");
            return null;
        }
    }
}
