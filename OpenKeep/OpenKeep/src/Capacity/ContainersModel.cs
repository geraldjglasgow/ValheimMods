using System;
using System.Collections.Generic;
using YamlConfig;

namespace OpenKeep.Capacity
{
    /// <summary>The parsed OpenKeep.Containers*.yml files: prefab name to grid size.</summary>
    public sealed class ContainersModel : YamlModel
    {
        /// <summary>Wider than this and the container panel cannot show every column.</summary>
        public const int PanelWidth = 8;

        public Dictionary<string, ContainerSize> Sizes { get; } = new Dictionary<string, ContainerSize>(StringComparer.OrdinalIgnoreCase);

        protected override void Read(YamlNode root)
        {
            YamlNode containers = root.Get("containers");
            if (containers.Kind == YamlNodeKind.Map)
                foreach (KeyValuePair<string, YamlNode> entry in containers.Entries)
                    ReadSize(entry.Key, entry.Value);
            ReadMisplaced(root);
        }

        /// <summary>An entry uncommented without its indentation lands at the top of the file; read it and say so.</summary>
        private void ReadMisplaced(YamlNode root)
        {
            foreach (KeyValuePair<string, YamlNode> entry in root.Entries)
            {
                if (string.Equals(entry.Key, "containers", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.Value.Kind != YamlNodeKind.Map)
                {
                    entry.Value.Warn("unknown key");
                    continue;
                }
                entry.Value.Warn("this entry sits at the top of the file; indent it two spaces so it is inside 'containers:' (applied anyway)");
                ReadSize(entry.Key, entry.Value);
            }
        }

        private void ReadSize(string prefab, YamlNode node)
        {
            bool hasWidth = node.Get("width").TryInt(out int width);
            bool hasHeight = node.Get("height").TryInt(out int height);
            if (!hasWidth || !hasHeight)
            {
                node.Error("a container needs width and height");
                return;
            }
            if (width < 1 || height < 1)
            {
                node.Error("width and height must be at least 1");
                return;
            }
            if (width > PanelWidth)
                node.Warn($"width {width} is wider than the container panel ({PanelWidth} columns); the extra columns may not be visible");
            Sizes[prefab] = new ContainerSize(width, height);
        }
    }
}
