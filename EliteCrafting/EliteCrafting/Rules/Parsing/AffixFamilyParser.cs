using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads the merged affix document (configuration.md section 6) into <see cref="AffixRules"/>: the root keys,
    /// every affix entry, then the cross-entry checks and lookups (<see cref="AffixIndex"/>). Null when any error.
    /// </summary>
    internal static class AffixFamilyParser
    {
        private static readonly string[] RootKeys = { FamilyBuilder.UseDefaultsKey, "health_critical", "caps", "affixes" };

        public static AffixRules? Parse(YamlMappingNode root, RuleIssues issues)
        {
            MapReader r = new MapReader(root, "", issues);
            r.Unknown(RootKeys);
            AffixRules rules = new AffixRules();
            ReadHealthCritical(r, rules);
            rules.Caps = ReadCaps(r);
            rules.Affixes = ReadAffixes(r);
            if (issues.HasErrors)
            {
                return null;
            }
            AffixIndex.Build(rules, issues);
            return issues.HasErrors ? null : rules;
        }

        private static void ReadHealthCritical(MapReader r, AffixRules rules)
        {
            MapReader? hc = r.Sub("health_critical");
            if (hc == null)
            {
                return;
            }
            hc.Value.Unknown("threshold", "max_threshold");
            rules.HealthCriticalThreshold = hc.Value.Float("threshold", 30f, 0f, 100f);
            rules.HealthCriticalMaxThreshold = hc.Value.Float("max_threshold", 50f, 0f, 100f);
            if (rules.HealthCriticalThreshold <= 0f || rules.HealthCriticalThreshold > rules.HealthCriticalMaxThreshold)
            {
                hc.Value.Error("threshold", "needs 0 < threshold <= max_threshold <= 100");
            }
        }

        private static Dictionary<string, float> ReadCaps(MapReader r)
        {
            Dictionary<string, float> caps = YamlLists.FloatMap(r, "caps");
            foreach (KeyValuePair<string, float> cap in new List<KeyValuePair<string, float>>(caps))
            {
                if (cap.Value <= 0f)
                {
                    r.Issues.Error($"caps.{cap.Key}", r.Node("caps"), "a cap must be above 0 (use null to remove one)");
                }
            }
            return caps;
        }

        private static List<AffixDef> ReadAffixes(MapReader r)
        {
            List<AffixDef> affixes = new List<AffixDef>();
            YamlSequenceNode? seq = r.Seq("affixes");
            if (seq == null)
            {
                return affixes;
            }
            foreach (YamlNode entry in seq.Children)
            {
                AffixDef? def = AffixParser.Parse(entry, r.Issues);
                if (def != null)
                {
                    affixes.Add(def);
                }
            }
            return affixes;
        }
    }
}
