using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>Scalar-level reads over YamlDotNet nodes. Culture-invariant, never throwing.</summary>
    internal static class YamlNodes
    {
        public static YamlNode? Child(YamlMappingNode map, string key)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> pair in map.Children)
            {
                if (pair.Key is YamlScalarNode scalar && scalar.Value == key)
                {
                    return pair.Value;
                }
            }
            return null;
        }

        /// <summary>A plain <c>~</c>, <c>null</c> or empty scalar: "remove this key" in a later layer.</summary>
        public static bool IsNull(YamlNode? node)
        {
            if (!(node is YamlScalarNode scalar) || scalar.Style == YamlDotNet.Core.ScalarStyle.SingleQuoted
                || scalar.Style == YamlDotNet.Core.ScalarStyle.DoubleQuoted)
            {
                return false;
            }
            string? v = scalar.Value;
            return string.IsNullOrEmpty(v) || v == "~" || v == "null" || v == "Null" || v == "NULL";
        }

        public static string? Text(YamlNode? node) => node is YamlScalarNode scalar ? scalar.Value : null;

        public static string KeyText(YamlNode key) => (key as YamlScalarNode)?.Value ?? "";

        public static bool TryFloat(YamlNode? node, out float value) => Numbers.TryFloat(Text(node), out value);

        public static bool TryInt(YamlNode? node, out int value) => Numbers.TryInt(Text(node), out value);

        public static bool TryBool(YamlNode? node, out bool value)
        {
            value = false;
            string? text = Text(node);
            return text != null && bool.TryParse(text, out value);
        }
    }
}
