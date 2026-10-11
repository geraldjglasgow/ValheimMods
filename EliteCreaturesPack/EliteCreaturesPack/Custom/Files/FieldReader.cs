using System;
using System.Collections.Generic;
using System.Globalization;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// The typed reads of one creature entry, each with its check: a number within its range, a word from its list, a
    /// colour, a range or a size. A value that fails is reported as an error at its own path and line (inside
    /// <c>CollectErrors</c>, so only this creature is left out) and read as unset. Every value read has its line noted in
    /// the definition's <see cref="CreatureDefinition.FieldLines"/>, for the build steps' messages. A key that is not
    /// there reads as null and reports nothing.
    /// </summary>
    internal sealed class FieldReader
    {
        private readonly CreatureDefinition definition;
        private readonly string prefix;

        public FieldReader(CreatureDefinition definition, YamlNode entry)
        {
            this.definition = definition;
            prefix = entry.Path + ".";
        }

        public static bool Has(YamlNode node) => node.Kind != YamlNodeKind.Missing;

        /// <summary>The value under the key, its line noted; a Missing node when the key is not there.</summary>
        public YamlNode At(YamlNode map, string key)
        {
            YamlNode node = map.Get(key);
            Note(node);
            return node;
        }

        /// <summary>A block of keys under the key, or null when absent; anything but a block is an error.</summary>
        public YamlNode? Block(YamlNode map, string key)
        {
            YamlNode node = At(map, key);
            if (!Has(node))
            {
                return null;
            }
            if (node.Kind != YamlNodeKind.Map)
            {
                node.Error("expected a block of keys under it");
                return null;
            }
            return node;
        }

        public float? Number(YamlNode map, string key, float min, float max) => NumberOf(At(map, key), min, max);

        public float? NumberOf(YamlNode node, float min, float max)
        {
            if (!Has(node) || !node.TryFloat(out float value))
            {
                return null;
            }
            return InRange(node, value, min, max) ? value : (float?)null;
        }

        public int? Whole(YamlNode map, string key, int min, int max)
        {
            YamlNode node = At(map, key);
            if (!Has(node) || !node.TryInt(out int value))
            {
                return null;
            }
            return InRange(node, value, min, max) ? value : (int?)null;
        }

        public bool? Switch(YamlNode map, string key)
        {
            YamlNode node = At(map, key);
            return Has(node) && node.TryBool(out bool value) ? value : (bool?)null;
        }

        public string? Text(YamlNode map, string key) => TextOf(At(map, key));

        public string? TextOf(YamlNode node)
        {
            if (!Has(node) || !node.TryString(out string text))
            {
                return null;
            }
            text = text.Trim();
            if (text.Length == 0)
            {
                node.Error("expected a value");
                return null;
            }
            return text;
        }

        public List<string>? Names(YamlNode map, string key) => NamesOf(At(map, key));

        /// <summary>One name or a list of names, each trimmed; an empty name is an error. An empty list reads as empty.</summary>
        public List<string>? NamesOf(YamlNode node)
        {
            if (!Has(node) || !node.TryStringList(out List<string> names))
            {
                return null;
            }
            List<string> trimmed = new List<string>(names.Count);
            foreach (string name in names)
            {
                if (name.Trim().Length == 0)
                {
                    node.Error("a name in the list is empty");
                    return null;
                }
                trimmed.Add(name.Trim());
            }
            return trimmed;
        }

        public T? Word<T>(YamlNode map, string key) where T : struct, Enum => WordOf<T>(At(map, key));

        public T? WordOf<T>(YamlNode node) where T : struct, Enum
        {
            string? text = TextOf(node);
            if (text == null)
            {
                return null;
            }
            if (Words.TryParse(text, out T value))
            {
                return value;
            }
            node.Error($"'{text}' is not one of: {Words.Choices<T>()}");
            return null;
        }

        public NumberRange? Range(YamlNode map, string key, float min, float max)
        {
            YamlNode node = At(map, key);
            List<float>? numbers = Numbers(node, min, max, 2);
            if (numbers == null)
            {
                return null;
            }
            return Ordered(node, numbers[0], numbers[numbers.Count - 1]) ? new NumberRange(numbers[0], numbers[numbers.Count - 1]) : (NumberRange?)null;
        }

        public CountRange? Count(YamlNode map, string key, int min, int max)
        {
            YamlNode node = At(map, key);
            List<float>? numbers = Numbers(node, min, max, 2);
            if (numbers == null || !AllWhole(node, numbers))
            {
                return null;
            }
            int low = (int)numbers[0], high = (int)numbers[numbers.Count - 1];
            return Ordered(node, low, high) ? new CountRange(low, high) : (CountRange?)null;
        }

        /// <summary>One number for every axis, or [x, y, z].</summary>
        public Vector3? Scale(YamlNode map, string key, float min, float max)
        {
            YamlNode node = At(map, key);
            List<float>? numbers = Numbers(node, min, max, 3);
            if (numbers == null)
            {
                return null;
            }
            if (numbers.Count == 2)
            {
                node.Error("expected one number or [x, y, z]");
                return null;
            }
            return numbers.Count == 1 ? Vector3.one * numbers[0] : new Vector3(numbers[0], numbers[1], numbers[2]);
        }

        public Color? Colour(YamlNode map, string key) => ColourOf(At(map, key));

        /// <summary>A colour: <c>"#rrggbb"</c> (or <c>#rrggbbaa</c>, or a colour name), or [r, g, b] / [r, g, b, a] from 0 to 1.</summary>
        public Color? ColourOf(YamlNode node)
        {
            if (!Has(node))
            {
                return null;
            }
            if (node.Kind == YamlNodeKind.Scalar)
            {
                string? text = TextOf(node);
                if (text != null && ColorUtility.TryParseHtmlString(text, out Color parsed))
                {
                    return parsed;
                }
                node.Error($"'{text}' is not a colour: write \"#rrggbb\" or [r, g, b] with numbers from 0 to 1");
                return null;
            }
            List<float>? numbers = Numbers(node, 0f, 1f, 4);
            if (numbers == null || numbers.Count < 3)
            {
                node.Error("expected a colour: \"#rrggbb\" or [r, g, b] with numbers from 0 to 1");
                return null;
            }
            return new Color(numbers[0], numbers[1], numbers[2], numbers.Count > 3 ? numbers[3] : 1f);
        }

        /// <summary>The node's path inside the entry: <c>character.health</c>.</summary>
        public string Relative(YamlNode node) =>
            node.Path.StartsWith(prefix, StringComparison.Ordinal) ? node.Path.Substring(prefix.Length) : node.Path;

        /// <summary>Notes the line of a value that is there.</summary>
        public void Note(YamlNode node)
        {
            if (Has(node) && node.Line > 0)
            {
                definition.FieldLines[Relative(node)] = node.Line;
            }
        }

        /// <summary>One number, or a list of 1 to <paramref name="most"/> numbers, each in the range; null (reported) otherwise.</summary>
        private List<float>? Numbers(YamlNode node, float min, float max, int most)
        {
            if (!Has(node))
            {
                return null;
            }
            if (node.Kind == YamlNodeKind.Scalar)
            {
                float? one = NumberOf(node, min, max);
                return one == null ? null : new List<float> { one.Value };
            }
            if (!node.TryList(out List<float> values))
            {
                return null;
            }
            if (values.Count == 0 || values.Count > most)
            {
                node.Error($"expected one number or a list of up to {most}");
                return null;
            }
            return values.TrueForAll(value => InRange(node, value, min, max)) ? values : null;
        }

        private static bool InRange(YamlNode node, float value, float min, float max)
        {
            if (!float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max)
            {
                return true;
            }
            node.Error($"{Show(value)} is outside {Show(min)} to {Show(max)}");
            return false;
        }

        private static bool Ordered(YamlNode node, float low, float high)
        {
            if (low <= high)
            {
                return true;
            }
            node.Error($"the low end {Show(low)} is above the high end {Show(high)}");
            return false;
        }

        private static bool AllWhole(YamlNode node, List<float> numbers)
        {
            if (numbers.TrueForAll(value => value == Mathf.Floor(value)))
            {
                return true;
            }
            node.Error("expected whole numbers");
            return false;
        }

        private static string Show(float value) => value.ToString("#,0.###", CultureInfo.InvariantCulture);
    }
}
