using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using YamlDotNet.RepresentationModel;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Hands the rule file's old single switches to the .cfg (<see cref="RuleSettings"/>, <see cref="MutationSettings"/>,
    /// <see cref="RespawnSettings"/>) and takes them out of the file, keeping the old text as a .bak beside it. Runs at
    /// start before the file is first read, when nothing binds yet, so the values land in this machine's own .cfg. A file
    /// without them is left alone. A file with no `difficulty:` line played as Custom, so it moves over as Custom unless
    /// the .cfg already held a difficulty.
    /// </summary>
    internal static class RuleMigration
    {
        /// <summary>The top-level keys the .cfg took over; the parser ignores them with a warning.</summary>
        public static readonly string[] MovedKeys =
            { "difficulty", "creature stars", "lock to server", "max mutations", "mutations enabled", "respawning" };

        public const string Note = "# Difficulty, creature stars, lock to server, max mutations, mutations enabled and "
            + "respawning are in the .cfg now.";

        public static void Run(string path)
        {
            string? text = Read(path);
            YamlMappingNode? root = text == null ? null : RuleParser.LoadRoot(text, new List<string>());
            if (root == null || !HasMoved(root))
            {
                return;
            }
            List<string> problems = new List<string>();
            CopyGeneral(root, problems);
            CopyMutations(root, problems);
            CopyRespawning(root, problems);
            Rewrite(path, text!);
            foreach (string problem in problems)
            {
                Log.Warn($"rule file hand-over to the .cfg, {problem}; the .cfg keeps its value");
            }
        }

        private static bool HasMoved(YamlMappingNode root)
        {
            foreach (string key in MovedKeys)
            {
                if (YamlRead.Child(root, key) != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CopyGeneral(YamlMappingNode root, List<string> problems)
        {
            CopyBool(root, "lock to server", RuleSettings.LockToServer, problems);
            CopyBool(root, "creature stars", RuleSettings.CreatureStars, problems);
            if (YamlRead.Child(root, "max mutations") != null)
            {
                RuleSettings.MaxMutations.Value = Math.Max(0, YamlRead.Int(root, "max mutations", RuleSettings.MaxMutations.Value, problems));
            }
            CopyDifficulty(root, problems);
        }

        private static void CopyBool(YamlMappingNode root, string key, ConfigEntry<bool> entry, List<string> problems)
        {
            if (YamlRead.Child(root, key) != null)
            {
                entry.Value = YamlRead.Bool(root, key, entry.Value, problems);
            }
        }

        private static void CopyDifficulty(YamlMappingNode root, List<string> problems)
        {
            YamlNode? node = YamlRead.Child(root, "difficulty");
            if (node == null)
            {
                if (!RuleSettings.DifficultyWasSaved)
                {
                    RuleSettings.DifficultyChoice.Value = Difficulty.Custom;
                }
                return;
            }
            string? text = YamlRead.Scalar(root, "difficulty");
            if (DifficultyNames.TryParse(text, out Difficulty difficulty))
            {
                RuleSettings.DifficultyChoice.Value = difficulty;
                return;
            }
            YamlRead.AddError(problems, node, $"'{text}' is not a difficulty: {DifficultyNames.Choices}");
        }

        private static void CopyMutations(YamlMappingNode root, List<string> problems)
        {
            if (!(YamlRead.Child(root, "mutations enabled") is YamlMappingNode map))
            {
                return;
            }
            foreach (KeyValuePair<YamlNode, YamlNode> pair in map.Children)
            {
                string name = (pair.Key as YamlScalarNode)?.Value ?? "";
                Mutation? mutation = MutationCatalog.FromName(name);
                ConfigEntry<bool>? entry = mutation == null ? null : MutationSettings.For(mutation.Value);
                if (entry == null)
                {
                    YamlRead.AddError(problems, pair.Key, $"'{name}' is not a mutation");
                }
                else if (YamlRead.TryBool(pair.Value, out bool on))
                {
                    entry.Value = on;
                }
                else
                {
                    YamlRead.AddError(problems, pair.Value, $"'{name}' should be true or false");
                }
            }
        }

        private static void CopyRespawning(YamlMappingNode root, List<string> problems)
        {
            if (!(YamlRead.Child(root, "respawning") is YamlMappingNode block))
            {
                return;
            }
            RespawnRules rules = RespawnSettings.Rules();
            RespawnOverlay.Apply(rules, block, problems);
            RespawnSettings.Camps.Value = rules.Camps;
            RespawnSettings.CampDays.Value = rules.CampDays;
            RespawnSettings.Dungeons.Value = rules.Dungeons;
            RespawnSettings.DungeonDays.Value = rules.DungeonDays;
            RespawnSettings.DungeonLoot.Value = rules.DungeonLoot;
            RespawnSettings.DungeonLootDays.Value = rules.DungeonLootDays;
        }

        private static string? Read(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception e)
            {
                Log.Error($"could not read the rule file: {e.Message}");
                return null;
            }
        }

        private static void Rewrite(string path, string text)
        {
            try
            {
                File.WriteAllText(path + ".bak", text);
                File.WriteAllText(path, RuleTextStrip.Strip(text, MovedKeys, Note));
                Log.Info($"moved {string.Join(", ", MovedKeys)} from {Path.GetFileName(path)} to the .cfg (sections 1 to 3); "
                    + $"the old file is kept as {Path.GetFileName(path)}.bak");
            }
            catch (Exception e)
            {
                Log.Error($"could not take the moved settings out of the rule file: {e.Message}; they are ignored there");
            }
        }
    }
}
