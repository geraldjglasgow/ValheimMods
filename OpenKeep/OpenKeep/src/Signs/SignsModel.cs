using System;
using System.Collections.Generic;
using UnityEngine;
using YamlConfig;

namespace OpenKeep.Signs
{
    /// <summary>
    /// The parsed OpenKeep.Signs*.yml files: one <see cref="SignRule"/> per listed container prefab. A bad offset or
    /// rotation is an error (the file set is rejected, the previous rules stay) and the entry is left out; an
    /// unknown prefab name is accepted silently, it may belong to a mod that is not loaded.
    /// </summary>
    public sealed class SignsModel : YamlModel
    {
        private readonly Dictionary<string, SignRule> containers = new Dictionary<string, SignRule>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, SignRule> Containers => containers;

        protected override void Read(YamlNode root)
        {
            YamlNode list = root.Get("containers");
            if (list.Kind == YamlNodeKind.Null || list.Kind == YamlNodeKind.Missing)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in list.Entries)
            {
                SignRule rule = ReadRule(entry.Value);
                if (rule != null)
                    containers[entry.Key.Trim()] = rule;
            }
        }

        private static SignRule ReadRule(YamlNode node)
        {
            if (node.Kind == YamlNodeKind.Null)
            {
                node.Warn("no settings, the entry does nothing");
                return null;
            }
            bool enabled = !node.Get("enabled").TryBool(out bool value) || value;
            if (!ReadOffset(node.Get("offset"), out Vector3 offset) || !ReadRotation(node.Get("rotation"), out float rotation))
                return null;
            return new SignRule(enabled, offset, rotation);
        }

        private static bool ReadRotation(YamlNode node, out float rotation)
        {
            rotation = 0f;
            if (node.Kind == YamlNodeKind.Missing || node.Kind == YamlNodeKind.Null)
                return true;
            return node.TryFloat(out rotation);
        }

        private static bool ReadOffset(YamlNode node, out Vector3 offset)
        {
            offset = Vector3.zero;
            if (node.Kind == YamlNodeKind.Missing || node.Kind == YamlNodeKind.Null)
                return true;
            if (node.Kind != YamlNodeKind.List || node.Items.Count != 3)
            {
                node.Error("offset must be three numbers: [x, y, z]");
                return false;
            }
            IReadOnlyList<YamlNode> items = node.Items;
            if (!items[0].TryFloat(out float x) || !items[1].TryFloat(out float y) || !items[2].TryFloat(out float z))
                return false;
            offset = new Vector3(x, y, z);
            return true;
        }
    }
}
