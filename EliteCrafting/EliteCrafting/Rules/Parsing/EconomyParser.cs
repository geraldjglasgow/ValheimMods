using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads the merged economy document (economy-yaml.md) into <see cref="EconomyRules"/>: each section by its own
    /// parser, then the cross-references, lookups and per-tier draw tables (<see cref="EconomyIndex"/>). Null when any
    /// error.
    /// </summary>
    internal static class EconomyParser
    {
        private static readonly string[] RootKeys =
        {
            FamilyBuilder.UseDefaultsKey, "rarities", "rolling", "runes", "item_tiers", "biomes", "drops",
        };

        public static EconomyRules? Parse(YamlMappingNode root, RuleIssues issues)
        {
            MapReader r = new MapReader(root, "", issues);
            r.Unknown(RootKeys);
            EconomyRules rules = new EconomyRules
            {
                Rarities = RarityParser.Parse(r),
                Rolling = RarityParser.ParseRolling(r),
                Stones = StoneParser.Parse(r),
                ItemTiers = TierMapParser.ParseItemTiers(r),
                Biomes = TierMapParser.ParseBiomes(r),
                Drops = DropParser.Parse(r),
            };
            if (issues.HasErrors)
            {
                return null;
            }
            EconomyIndex.Build(rules, issues);
            return issues.HasErrors ? null : rules;
        }
    }
}
