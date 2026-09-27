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
    /// The translation files, <c>EarthWright.Language.&lt;Language&gt;.yml</c>: read from the plugin's folder
    /// (translations shipped with the mod) and then the config folder (the player's own, which wins key by key). The
    /// English template written by <c>ew language write</c> goes to the config folder.
    /// </summary>
    public static class LanguageFiles
    {
        public const string Prefix = "EarthWright.Language.";

        /// <summary>The translated words of a language, empty when there is no file.</summary>
        public static Dictionary<string, string> Load(string language)
        {
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(language))
                return words;
            // The plugin's folder first, so the player's own file in the config folder wins key by key.
            foreach (string folder in Enumerable.Reverse(Folders()))
                Merge(words, Path.Combine(folder, Prefix + language + ".yml"));
            return words;
        }

        /// <summary>The config folder first, then the plugin's folder.</summary>
        public static List<string> Folders()
        {
            IEnumerable<string> paths = Plugin.Synced != null ? Plugin.Synced.SearchPaths : new[] { BepInEx.Paths.ConfigPath };
            return paths.Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void Merge(Dictionary<string, string> into, string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                Dictionary<string, string> words = WordList.Parse(File.ReadAllText(path), Path.GetFileName(path));
                foreach (KeyValuePair<string, string> word in words)
                    into[word.Key] = word.Value;
                Plugin.Log.LogInfo($"{words.Count} words read from {path}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{path} could not be read and was skipped: {e.Message}");
            }
        }

        /// <summary>Writes every registered English word, sorted by key, as a template for translators. Returns the path.</summary>
        public static string WriteEnglishTemplate()
        {
            string path = Path.Combine(Folders()[0], Prefix + "English.yml");
            StringBuilder text = new StringBuilder();
            AppendHeader(text);
            foreach (KeyValuePair<string, string> word in Language.Translatable.OrderBy(w => w.Key, StringComparer.Ordinal))
                text.Append(WordList.Quote(word.Key, keyStyle: true)).Append(": ").Append(WordList.Quote(word.Value, keyStyle: false)).Append('\n');
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static void AppendHeader(StringBuilder text)
        {
            text.Append("# EarthWright words in English (the built-in texts), written by the console command 'ew language write'.\n");
            text.Append("# To translate, copy this file to EarthWright.Language.<Language>.yml in this folder, where <Language> is the\n");
            text.Append("# game's language name (German, French, Spanish, Russian, Polish, Portuguese_Brazilian, Chinese, Japanese ...),\n");
            text.Append("# and translate the texts on the right. Keep the keys on the left, the $1 $2 placeholders and any <color> tags.\n");
            text.Append("# Leave out what you do not translate: missing keys stay English. 'ew language reload' applies a changed file.\n");
            text.Append("# While the game is in English, this file itself replaces the built-in English texts; delete it when done.\n");
            text.Append("# Custom menu entries (ew_custom_...) are not listed: their texts come from EarthWright.Entries.yml as written.\n");
            text.Append("# The game's language is now: " + (Language.CurrentLanguage ?? "unknown") + "\n\n");
        }
    }
}
