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
            FamilyBuilder.UseDefaultsKey, "rarities", "rolling", "stones", "sigils", "item_tiers", "biomes", "drops",
            "essence_families", "salvage",
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
                Sigils = new SigilSettings { ConsumeOnUnsteered = SigilFlag(r) },
                ItemTiers = TierMapParser.ParseItemTiers(r),
                Biomes = TierMapParser.ParseBiomes(r),
                Drops = DropParser.Parse(r),
                EssenceFamilies = EssenceFamilyParser.Parse(r),
                Salvage = SalvageParser.Parse(r),
            };
            if (issues.HasErrors)
            {
                return null;
            }
            EconomyIndex.Build(rules, issues);
            return issues.HasErrors ? null : rules;
        }

        private static bool SigilFlag(MapReader r)
        {
            MapReader? sigils = r.Sub("sigils");
            if (sigils == null)
            {
                return false;
            }
            sigils.Value.Unknown("consume_on_unsteered");
            return sigils.Value.Bool("consume_on_unsteered", false);
        }
    }
}
