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
            if (containers.Kind == YamlNodeKind.Null || containers.Kind == YamlNodeKind.Missing)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in containers.Entries)
                ReadSize(entry.Key, entry.Value);
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
