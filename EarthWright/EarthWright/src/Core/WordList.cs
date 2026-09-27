using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using YamlDotNet.RepresentationModel;

namespace EarthWright.Core
{
    /// <summary>
    /// The format of a word list: a flat map of key to text, one "key: text" per line. Read as YAML; when a hand-edited
    /// file is not valid YAML (typically an unquoted text containing ": "), each line is read on its own instead, split
    /// at its first colon, so one slip does not lose the whole translation. Texts are written double-quoted and escaped.
    /// </summary>
    public static class WordList
    {
        public static Dictionary<string, string> Parse(string text, string origin)
        {
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                YamlStream stream = new YamlStream();
                stream.Load(new StringReader(text));
                if (stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode map)
                    AddPairs(words, map, origin);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{origin} is not valid YAML ({e.Message}); reading it line by line. Put texts in \"double quotes\".");
                words = ParseLines(text);
            }
            return words;
        }

        private static void AddPairs(Dictionary<string, string> into, YamlMappingNode map, string origin)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> pair in map.Children)
            {
                string key = (pair.Key as YamlScalarNode)?.Value;
                string value = (pair.Value as YamlScalarNode)?.Value;
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    Plugin.Log.LogWarning($"{origin}: the entry on line {pair.Key.Start.Line} is not 'key: text' and was skipped");
                    continue;
                }
                // An empty text is a word not translated yet: it stays English rather than disappearing.
                if (value.Length > 0)
                    into[key.TrimStart('$')] = value;
            }
        }

        /// <summary>The fallback reader: "key: text" per line, texts plain, 'single' or "double" quoted; # starts a comment line.</summary>
        public static Dictionary<string, string> ParseLines(string text)
        {
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                int colon = line.IndexOf(':');
                if (line.Length == 0 || line[0] == '#' || colon <= 0)
                    continue;
                string key = Unquote(line.Substring(0, colon).Trim()).TrimStart('$');
                string value = Unquote(line.Substring(colon + 1).Trim());
                if (key.Length > 0 && value.Length > 0)
                    words[key] = value;
            }
            return words;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return Unescape(value.Substring(1, value.Length - 2));
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            // A trailing " # comment": cut the last one and look again (the rest may be a quoted text).
            int comment = value.LastIndexOf(" #", StringComparison.Ordinal);
            return comment >= 0 ? Unquote(value.Substring(0, comment).TrimEnd()) : value;
        }

        /// <summary>The escapes of a YAML double-quoted text: \\ \" \n \r \t \0 \xHH \uHHHH; any other escaped character stands for itself.</summary>
        private static string Unescape(string value)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    text.Append(value[i]);
                    continue;
                }
                char code = value[++i];
                int digits = code == 'x' ? 2 : code == 'u' ? 4 : 0;
                if (digits > 0 && i + digits < value.Length && int.TryParse(value.Substring(i + 1, digits), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int unit))
                {
                    text.Append((char)unit);
                    i += digits;
                    continue;
                }
                text.Append(code == 'n' ? '\n' : code == 'r' ? '\r' : code == 't' ? '\t' : code == '0' ? '\0' : code);
            }
            return text.ToString();
        }

        /// <summary>A YAML scalar: keys stay plain when they are simple names, texts are always double-quoted and escaped.</summary>
        public static string Quote(string value, bool keyStyle)
        {
            if (keyStyle && value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-'))
                return value;
            StringBuilder quoted = new StringBuilder("\"");
            foreach (char c in value)
                quoted.Append(Escape(c));
            return quoted.Append('"').ToString();
        }

        private static string Escape(char c)
        {
            switch (c)
            {
                case '\\': return "\\\\";
                case '"': return "\\\"";
                case '\n': return "\\n";
                case '\r': return "\\r";
                case '\t': return "\\t";
                default: return c < ' ' ? "\\x" + ((int)c).ToString("X2") : c.ToString();
            }
        }
    }
}
