using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EliteCreaturesPack.Custom.Files;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// Values as the custom creature files write them, so an export reads back through the readers unchanged: numbers in
    /// the invariant culture, text in double quotes, names bare when YAML would read them as the same text, lists in
    /// brackets, colours as <c>"#rrggbb"</c>, and the game's enum values as the words the files take (<c>very weak</c>,
    /// <c>forest monsters</c>, <c>hurt friend</c>).
    /// </summary>
    internal static class ExportValues
    {
        /// <summary>Words YAML could read as something other than text, or that read as a switch.</summary>
        private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "true", "false", "yes", "no", "on", "off", "y", "n", "null",
        };

        /// <summary>Up to four decimals, a dot as the separator, no "-0".</summary>
        public static string Number(float value) =>
            (value == 0f ? 0f : value).ToString("0.####", CultureInfo.InvariantCulture);

        public static bool Within(float value, float min, float max) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;

        /// <summary>One number when both ends agree, otherwise <c>[low, high]</c>.</summary>
        public static string Range(float low, float high) =>
            low == high ? Number(low) : $"[{Number(low)}, {Number(high)}]";

        /// <summary>Text in double quotes, its quotes, backslashes and line breaks escaped as YAML reads them.</summary>
        public static string Text(string text) =>
            "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";

        /// <summary>A prefab or item name: bare when it is a plain word, quoted otherwise.</summary>
        public static string Name(string name) => Plain(name) ? name : Text(name);

        /// <summary>A list of names in brackets; <c>[]</c> when empty.</summary>
        public static string Names(IEnumerable<string> names) => "[" + string.Join(", ", names.Select(Name)) + "]";

        /// <summary><c>"#rrggbb"</c>, or <c>"#rrggbbaa"</c> when the colour is see-through.</summary>
        public static string Colour(Color colour)
        {
            string html = colour.a < 1f ? ColorUtility.ToHtmlStringRGBA(colour) : ColorUtility.ToHtmlStringRGB(colour);
            return "\"#" + html.ToLowerInvariant() + "\"";
        }

        /// <summary>An enum value as the files write it: <c>VeryWeak</c> as <c>very weak</c>.</summary>
        public static string Word(Enum value) => Words.Spaced(value.ToString());

        private static bool Plain(string name) =>
            name.Length > 0
            && (char.IsLetter(name[0]) || name[0] == '_')
            && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
            && !Reserved.Contains(name);
    }
}
