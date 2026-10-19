using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Translations
{
    /// <summary>
    /// One word file: a flat YAML map of <c>key: "text"</c>. A file YamlDotNet cannot read is skipped whole with a warning
    /// naming its line (a line break inside a quoted text is the usual cause: write <c>\n</c> instead); a line that is not
    /// a key and a text is skipped alone.
    /// </summary>
    internal static class WordFile
    {
        public static void Merge(Dictionary<string, string> words, string text, string source, ManualLogSource log)
        {
            YamlMappingNode? root;
            try
            {
                root = Root(text);
            }
            catch (YamlException e)
            {
                log.LogWarning($"{source} is not a valid word list, none of its words are used (line {e.Start.Line}): {e.Message}");
                return;
            }
            if (root == null)
            {
                return;
            }
            foreach (KeyValuePair<YamlNode, YamlNode> pair in root.Children)
            {
                Add(words, pair, source, log);
            }
        }

        private static void Add(Dictionary<string, string> words, KeyValuePair<YamlNode, YamlNode> pair, string source, ManualLogSource log)
        {
            if (pair.Key is YamlScalarNode key && !string.IsNullOrEmpty(key.Value) && pair.Value is YamlScalarNode value)
            {
                words[key.Value!.TrimStart('$')] = value.Value ?? "";
                return;
            }
            log.LogWarning($"{source} line {pair.Key.Start.Line}: not a 'key: text' line, skipped");
        }

        private static YamlMappingNode? Root(string text)
        {
            YamlStream stream = new YamlStream();
            stream.Load(new StringReader(text));
            return stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode as YamlMappingNode;
        }
    }
}
