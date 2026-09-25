using System;
using System.Collections.Generic;
using System.IO;
using EliteCrafting.Core;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Turns a family's texts into one merged document: the built-in defaults (unless the main file says
    /// <c>use_defaults: false</c>), then every file in the order given (main file first, then by name). Syntax errors
    /// are recorded per file; the caller parses the merged document and decides.
    /// </summary>
    internal static class FamilyBuilder
    {
        public const string UseDefaultsKey = "use_defaults";

        public static YamlMappingNode Merge(FamilySpec spec, IReadOnlyList<SourceText> files, RuleIssues issues) =>
            Merge(spec, files, issues, out _);

        /// <summary><see cref="Merge(FamilySpec, IReadOnlyList{SourceText}, RuleIssues)"/>, also saying whether the built-in defaults were layered in.</summary>
        public static YamlMappingNode Merge(FamilySpec spec, IReadOnlyList<SourceText> files, RuleIssues issues, out bool usedDefaults)
        {
            List<SourceLayer> layers = new List<SourceLayer>();
            bool useDefaults = true;
            foreach (SourceText file in files)
            {
                YamlMappingNode? root = Load(file, issues);
                if (root == null)
                {
                    continue;
                }
                useDefaults &= ReadUseDefaults(spec, file, root, issues);
                layers.Add(new SourceLayer(file.Name, root, builtIn: false));
            }
            if (useDefaults)
            {
                layers.Insert(0, Defaults(spec, issues));
            }
            usedDefaults = useDefaults;
            return YamlMerge.Merge(layers, spec.IdLists, issues);
        }

        /// <summary>The embedded default text of a family (also written as the main file on first run).</summary>
        public static string DefaultText(FamilySpec spec) =>
            Embedded.Text(spec.DefaultResource) ?? throw new InvalidOperationException($"missing resource {spec.DefaultResource}");

        private static SourceLayer Defaults(FamilySpec spec, RuleIssues issues)
        {
            SourceText text = new SourceText(FamilySpec.DefaultsOrigin, DefaultText(spec));
            YamlMappingNode root = Load(text, issues) ?? new YamlMappingNode();
            return new SourceLayer(text.Name, root, builtIn: true);
        }

        private static bool ReadUseDefaults(FamilySpec spec, SourceText file, YamlMappingNode root, RuleIssues issues)
        {
            YamlNode? node = YamlNodes.Child(root, UseDefaultsKey);
            if (node == null)
            {
                return true;
            }
            if (!string.Equals(file.Name, spec.MainFile, StringComparison.OrdinalIgnoreCase))
            {
                issues.Warn(UseDefaultsKey, node, $"only read from {spec.MainFile}; ignored here");
                return true;
            }
            if (YamlNodes.TryBool(node, out bool value))
            {
                return value;
            }
            issues.Error(UseDefaultsKey, node, "should be true or false");
            return true;
        }

        private static YamlMappingNode? Load(SourceText file, RuleIssues issues)
        {
            try
            {
                YamlStream stream = new YamlStream();
                stream.Load(new StringReader(file.Text));
                if (stream.Documents.Count == 0)
                {
                    return new YamlMappingNode();
                }
                return AsRoot(file, stream.Documents[0].RootNode, issues);
            }
            catch (YamlException e)
            {
                issues.Error("", null, $"{file.Name}:{e.Start.Line}: {e.Message}");
            }
            catch (Exception e)
            {
                issues.Error("", null, $"{file.Name}: could not be read: {e.Message}");
            }
            return null;
        }

        private static YamlMappingNode? AsRoot(SourceText file, YamlNode root, RuleIssues issues)
        {
            if (root is YamlMappingNode map)
            {
                issues.Register(map, file.Name);
                return map;
            }
            if (YamlNodes.IsNull(root))
            {
                return new YamlMappingNode();
            }
            issues.Error("", null, $"{file.Name}: the file should be a block of keyed values");
            return null;
        }
    }
}
