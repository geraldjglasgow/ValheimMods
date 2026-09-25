using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Merges a list of entries with an <c>id</c> (affixes, rarities, stones): a new id is appended, an existing id
    /// changes only the fields the later entry names (logged as a note, so an owner can see what their files did), and
    /// the same id twice in one file is an error. Entries without an id pass through for the parser to report.
    /// </summary>
    internal static class IdListMerge
    {
        public static YamlSequenceNode Merge(YamlSequenceNode? earlier, YamlSequenceNode later, string listKey,
            SourceLayer layer, RuleIssues issues)
        {
            List<YamlNode> result = earlier != null ? new List<YamlNode>(earlier.Children) : new List<YamlNode>();
            HashSet<string> seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (YamlNode entry in later.Children)
            {
                string? id = EntryId(entry);
                if (id == null)
                {
                    result.Add(entry);
                    continue;
                }
                if (!seen.Add(id))
                {
                    issues.Error($"{listKey}[{id}]", entry, $"the id '{id}' appears twice in {layer.Origin}");
                    continue;
                }
                MergeEntry(result, (YamlMappingNode)entry, id, listKey, layer, issues);
            }
            return new YamlSequenceNode(result);
        }

        private static void MergeEntry(List<YamlNode> result, YamlMappingNode entry, string id, string listKey,
            SourceLayer layer, RuleIssues issues)
        {
            int index = result.FindIndex(n => EntryId(n) == id);
            if (index < 0)
            {
                result.Add(entry);
                return;
            }
            result[index] = YamlMerge.MergeMaps((YamlMappingNode)result[index], entry);
            if (!layer.BuiltIn)
            {
                issues.Note($"{listKey} '{id}' overridden by {layer.Origin}: {FieldList(entry)}");
            }
        }

        public static string? EntryId(YamlNode node) =>
            node is YamlMappingNode map ? YamlNodes.Text(YamlNodes.Child(map, "id")) : null;

        private static string FieldList(YamlMappingNode entry)
        {
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(entry))
            {
                if (pair.Key != "id")
                {
                    keys.Add(pair.Key);
                }
            }
            return keys.Count == 0 ? "(nothing)" : string.Join(", ", keys);
        }
    }
}
