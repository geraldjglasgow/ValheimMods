using System;
using System.Collections.Generic;
using System.IO;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Traits;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Turns the rule file's text into a <see cref="RuleSet"/>, collecting every problem against its line rather than
    /// throwing. A syntax error leaves <see cref="Result.Rules"/> null; a merely questionable value (a number in the
    /// wrong place) is skipped and recorded. The caller decides whether to adopt the result or keep the last good one.
    /// </summary>
    internal static class RuleParser
    {
        public sealed class Result
        {
            public RuleSet? Rules;
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
        }

        public static Result Parse(string text)
        {
            Result result = new Result();
            YamlMappingNode? root = LoadRoot(text, result.Errors);
            if (root == null)
            {
                return result;
            }
            RuleSet set = ReadSettings(root, result);
            ReadDefaults(set, root, result);
            ReadBosses(set, root, result);
            ReadWorld(set, root, result);
            ReadLoot(set, root, result);
            ReadCreatures(set, root, result);
            ReadBiomes(set, root, result);
            return Finish(set, result);
        }

        // The single switches live in the .cfg (Config/RuleSettings.cs): the server's values while it binds. A line the
        // file still holds for one of them is ignored.
        private static RuleSet ReadSettings(YamlMappingNode root, Result result)
        {
            foreach (string key in RuleMigration.MovedKeys)
            {
                if (YamlRead.Child(root, key) is YamlNode node)
                {
                    YamlRead.AddError(result.Warnings, node, $"'{key}' is set in the .cfg now; this line is ignored");
                }
            }
            RuleSet set = new RuleSet
            {
                Difficulty = RuleSettings.CurrentDifficulty,
                CreatureStars = RuleSettings.StarsOn,
                MaxMutations = RuleSettings.MutationCap,
                Respawn = RespawnSettings.Rules(),
            };
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                set.MutationEnabled[mutation] = MutationSettings.IsOn(mutation);
            }
            return set;
        }

        // The steps that need the whole file read: the star fallback for unlisted biomes, Extreme's longer star lines.
        private static Result Finish(RuleSet set, Result result)
        {
            SeedStarFallback(set);
            PadStarPower(set);
            result.Rules = set;
            return result;
        }

        // Extreme rolls up to 8 stars: star power lines written for five continue by the built-in steps.
        private static void PadStarPower(RuleSet set)
        {
            if (set.Difficulty != Difficulty.Extreme)
            {
                return;
            }
            StarPower reference = RuleDefaults.BaselineStarPower();
            set.Defaults.Star.PadFrom(reference);
            foreach (BiomeRules biome in set.Biomes.Values)
            {
                biome.Star.PadFrom(reference);
            }
        }

        /// <summary>An unlisted biome takes the Meadows row for stars; copy it onto the defaults so the fallback uses it.</summary>
        private static void SeedStarFallback(RuleSet set)
        {
            if (set.Biomes.TryGetValue("Meadows", out BiomeRules meadows))
            {
                set.Defaults.StarChances = (float[])meadows.StarChances.Clone();
            }
        }

        internal static YamlMappingNode? LoadRoot(string text, List<string> errors)
        {
            try
            {
                YamlStream stream = new YamlStream();
                stream.Load(new StringReader(text));
                if (stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode root)
                {
                    return root;
                }
                errors.Add("the file is empty or is not a set of keyed values");
            }
            catch (YamlException ex)
            {
                errors.Add($"line {ex.Start.Line}: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"could not read the file: {ex.Message}");
            }
            return null;
        }

        private static void ReadDefaults(RuleSet set, YamlMappingNode root, Result result)
        {
            set.Defaults = RuleDefaults.Baseline();
            if (YamlRead.Child(root, "defaults") is YamlMappingNode block)
            {
                RuleOverlay.Apply(set.Defaults, block, result.Errors, result.Warnings);
            }
        }

        private static void ReadBosses(RuleSet set, YamlMappingNode root, Result result)
        {
            set.Boss = RuleDefaults.BaselineBoss();
            if (YamlRead.Child(root, "bosses") is YamlMappingNode block)
            {
                BossOverlay.Apply(set.Boss, block, result.Errors, result.Warnings);
            }
        }

        private static void ReadWorld(RuleSet set, YamlMappingNode root, Result result)
        {
            if (YamlRead.Child(root, Fields.WorldTiers) is YamlNode tiers
                && YamlRead.Map(tiers, result.Errors, $"'{Fields.WorldTiers}'") is YamlMappingNode tierBlock)
            {
                WorldOverlay.ApplyTiers(set.Tiers, tierBlock, result.Errors);
            }
            if (YamlRead.Child(root, Fields.Breeding) is YamlNode breeding
                && YamlRead.Map(breeding, result.Errors, $"'{Fields.Breeding}'") is YamlMappingNode breedBlock)
            {
                WorldOverlay.ApplyBreeding(set.Breeding, breedBlock, result.Errors);
            }
        }

        private static void ReadLoot(RuleSet set, YamlMappingNode root, Result result)
        {
            set.Loot = new LootRules();
            if (YamlRead.Child(root, "loot") is YamlMappingNode block)
            {
                LootOverlay.Apply(set.Loot, block, result.Errors);
            }
        }

        /// <summary>Per-creature loot and mutation keys. Mutation keys are laid over a biome only at lookup time.</summary>
        private static void ReadCreatures(RuleSet set, YamlMappingNode root, Result result)
        {
            if (YamlRead.Child(root, "creatures") is YamlNode creatures)
            {
                CreatureOverlay.Apply(set, creatures, result.Errors);
            }
        }

        private static void ReadBiomes(RuleSet set, YamlMappingNode root, Result result)
        {
            YamlNode? node = YamlRead.Child(root, "biomes");
            if (node == null)
            {
                return;
            }
            if (!(node is YamlSequenceNode seq))
            {
                YamlRead.AddError(result.Errors, node, "'biomes' should be a list of biome blocks");
                return;
            }
            foreach (YamlNode item in seq.Children)
            {
                ReadBiome(set, item, result);
            }
        }

        private static void ReadBiome(RuleSet set, YamlNode item, Result result)
        {
            if (!(item is YamlMappingNode block))
            {
                YamlRead.AddError(result.Errors, item, "a biome entry should be a block of keyed values");
                return;
            }
            string? name = YamlRead.Scalar(block, "match");
            if (string.IsNullOrEmpty(name))
            {
                YamlRead.AddError(result.Errors, block, "a biome entry has no 'match' name");
                return;
            }
            BiomeRules rules = set.Defaults.Clone();
            RuleOverlay.Apply(rules, block, result.Errors, result.Warnings);
            set.Biomes[name!] = rules;
        }
    }
}
