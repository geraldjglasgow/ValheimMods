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
    /// The words of the mod. English is built in: a module registers its keys in Initialize with <see cref="Add"/>.
    /// Whenever the game sets up a language, the English words are handed to the game's localization and then the
    /// words of <c>EarthWright.Language.&lt;Language&gt;.yml</c> replace them key by key, so a translation may be
    /// partial and anything it leaves out stays English. Keys are "ew_..." without the dollar; <see cref="Add"/>
    /// returns the "$ew_..." token to put into texts.
    /// </summary>
    public static class Language
    {
        private static readonly Dictionary<string, string> english = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> translated = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>The language the words were last installed for (the game's language), or null before the game set one up.</summary>
        public static string CurrentLanguage { get; private set; }

        /// <summary>Counts the times the words the game shows changed (a language set up, a word added), for localized text caches.</summary>
        public static int Version { get; private set; }

        /// <summary>
        /// Key prefixes of words that come from configuration data rather than from the mod: the custom entries of
        /// EarthWright.Entries.yml ("ew_custom_&lt;id&gt;", registered by the Menu module when the YAML loads). They are shown
        /// as the data gives them, never replaced by a translation file, and left out of the template.
        /// </summary>
        private static readonly string[] DataPrefixes = { "ew_custom_" };

        /// <summary>Every registered key with its built-in English text.</summary>
        public static IReadOnlyDictionary<string, string> English => english;

        /// <summary>The registered words a translation may replace (every word but the data words), with their English text.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Translatable => english.Where(word => !IsDataWord(word.Key));

        /// <summary>How many words the translation files of <see cref="CurrentLanguage"/> replaced.</summary>
        public static int TranslatedCount => translated.Count(pair => english.ContainsKey(pair.Key) && !IsDataWord(pair.Key));

        /// <summary>The key belongs to a word taken from configuration data (see <see cref="DataPrefixes"/>).</summary>
        public static bool IsDataWord(string key) => DataPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));

        public static string Add(string key, string englishText)
        {
            english[key] = englishText;
            Localization localization = GameLocalization;
            if (localization != null)
            {
                localization.AddWord(key, Text(key));
                localization.m_cache.EvictAll();
                Version++;
            }
            return "$" + key;
        }

        /// <summary>Localizes a text with $tokens through the game, or returns it unchanged before the game is up.</summary>
        public static string Localize(string text)
        {
            Localization localization = GameLocalization;
            return localization != null ? localization.Localize(text) : text;
        }

        /// <summary>Localizes a text and fills its $1, $2 ... placeholders with the given words.</summary>
        public static string Format(string text, params string[] words)
        {
            Localization localization = GameLocalization;
            if (localization != null)
                return localization.Localize(text, words);
            for (int i = 0; i < words.Length; i++)
                text = text.Replace("$" + (i + 1), words[i]);
            return text;
        }

        /// <summary>The text of a key in the current language: the translation, else the English word, else the key.</summary>
        public static string Text(string key)
        {
            if (translated.TryGetValue(key, out string text) && !IsDataWord(key))
                return text;
            return english.TryGetValue(key, out string word) ? word : key;
        }

        /// <summary>Reads the translation files again and hands every word to the game. False before the game set up a language.</summary>
        public static bool Reload()
        {
            Localization localization = GameLocalization;
            if (localization == null)
                return false;
            Install(localization, CurrentLanguage ?? localization.GetSelectedLanguage());
            return true;
        }

        internal static void Install(Localization localization, string language)
        {
            CurrentLanguage = language;
            translated = LanguageFiles.Load(language);
            foreach (string key in english.Keys)
                localization.AddWord(key, Text(key));
            localization.m_cache.EvictAll();
            Version++;
        }

        // The game's localization only once the game made it: reading Localization.instance would create it early,
        // before the platform knows the player's locale.
        private static Localization GameLocalization => Localization.m_instance;
    }

    /// <summary>Hands the words to the game each time it sets up a language (at start and when the player picks another).</summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class LanguageSetupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Localization __instance, string language)
        {
            Safe.Run("EarthWright language setup", () => Language.Install(__instance, language));
        }
    }
}
