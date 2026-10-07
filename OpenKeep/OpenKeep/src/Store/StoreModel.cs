using System;
using System.Collections.Generic;
using OpenKeep.Core;
using YamlConfig;

namespace OpenKeep.Store
{
    /// <summary>The model of OpenKeep.Store*.yml: the <c>groups:</c> map and the <c>containers:</c> rules by prefab.</summary>
    public sealed class StoreModel : YamlModel
    {
        public ItemGroups ItemGroups { get; } = new ItemGroups();

        public Dictionary<string, StoreRule> Containers { get; } = new Dictionary<string, StoreRule>(StringComparer.OrdinalIgnoreCase);

        protected override void Read(YamlNode root)
        {
            ItemGroups.Read(root.Get("groups"));
            YamlNode containers = root.Get("containers");
            if (containers.Kind == YamlNodeKind.Missing)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in containers.Entries)
                Containers[entry.Key.Trim()] = StoreRule.Read(entry.Value, ItemGroups);
        }
    }
}
