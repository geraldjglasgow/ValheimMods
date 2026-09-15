using System;
using System.Collections.Generic;
using OpenKeep.Core;
using YamlConfig;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The parsed OpenKeep.Reach*.yml files: an optional range that replaces the cfg Range, the groups map and one
    /// <see cref="ContainerRule"/> per listed container prefab. Keys nobody asks for are warned about by the library.
    /// </summary>
    public sealed class ReachModel : YamlModel
    {
        private readonly ItemGroups groups = new ItemGroups();
        private readonly Dictionary<string, ContainerRule> containers = new Dictionary<string, ContainerRule>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Replaces the cfg Range when present.</summary>
        public float? Range { get; private set; }

        public IReadOnlyDictionary<string, ContainerRule> Containers => containers;

        protected override void Read(YamlNode root)
        {
            Range = ReadRange(root.Get("range"));
            YamlNode groupsNode = root.Get("groups");
            if (groupsNode.Kind != YamlNodeKind.Null)
                groups.Read(groupsNode);
            YamlNode list = root.Get("containers");
            if (list.Kind == YamlNodeKind.Null || list.Kind == YamlNodeKind.Missing)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in list.Entries)
                containers[entry.Key.Trim()] = ReadContainer(entry.Value);
        }

        private static float? ReadRange(YamlNode node)
        {
            if (!node.TryFloat(out float range))
                return null;
            if (range > 0f)
                return range;
            node.Error("range must be greater than 0");
            return null;
        }

        private ContainerRule ReadContainer(YamlNode node)
        {
            if (node.Kind == YamlNodeKind.Null)
            {
                node.Warn("no settings, the entry does nothing");
                return ContainerRule.Default;
            }
            bool enabled = !node.Get("enabled").TryBool(out bool value) || value;
            float? range = ReadRange(node.Get("range"));
            return new ContainerRule(enabled, range, ReadSet(node.Get("allow")), ReadSet(node.Get("deny")));
        }

        private ItemMatchSet ReadSet(YamlNode node)
        {
            if (!node.TryStringList(out List<string> entries))
                return ItemMatchSet.Empty;
            ItemMatchSet set = ItemMatchSet.Parse(entries, groups);
            foreach (ItemMatcher matcher in set.Matchers)
            {
                if (matcher.Problem != null)
                    node.Warn("'" + matcher + "' matches nothing: " + matcher.Problem);
            }
            return set;
        }
    }
}
