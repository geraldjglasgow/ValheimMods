using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Translations
{
    /// <summary>
    /// A mod's player-facing words in the game's language. English ships embedded (<c>translations/English*.yml</c>, flat
    /// <c>key: "text"</c> maps); another language comes from an embedded <c>translations/&lt;Language&gt;*.yml</c>, then
    /// from <c>&lt;Mod&gt;.translations.&lt;Language&gt;.yml</c> anywhere under the plugins folder, then from the same name in
    /// the config folder, each replacing the one before key by key, so a translation may be partial and anything it leaves
    /// out stays English. Whenever the game sets up a language the words are rebuilt and written into its word table, so
    /// <c>$key</c> tokens in item names, status effects and hover texts translate too. Keys are written without the <c>$</c>;
    /// <c>$1</c>, <c>$2</c> ... in a text are its values, as in the game's own words.
    /// </summary>
    public static class Lang
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> translated = new Dictionary<string, string>(StringComparer.Ordinal);
        private static WordSources? sources;

        /// <summary>The language the words were last built for: the game's selected language.</summary>
        public static string Language { get; private set; } = "English";

        /// <summary>Counts every change of the words (a language set up, a word added), for caches of built text.</summary>
        public static int Version { get; private set; }

        /// <summary>Raised after the words changed. Local machine, main thread.</summary>
        public static event Action? Changed;

        /// <summary>
        /// Once, in the plugin's Awake: loads the embedded English (<c>&lt;modName&gt;.translations.English*.yml</c>
        /// resources) and hooks the game's language setup. When the game's word table already exists, the words go in now.
        /// </summary>
        public static void Install(Harmony harmony, Assembly assembly, string modName, ManualLogSource log)
        {
            sources = new WordSources(assembly, modName, log);
            sources.LoadEnglish(English);
            GameTable.Install(harmony, Setup, log);
            global::Localization? game = GameTable.Current;
            if (game != null)
            {
                Setup(game, game.GetSelectedLanguage());
            }
        }

        /// <summary>The key's text in the current language, else its English, else <c>[key]</c> as the game shows a miss.</summary>
        public static string Get(string key)
        {
            string bare = Bare(key);
            if (translated.TryGetValue(bare, out string text) || English.TryGetValue(bare, out text))
            {
                return text;
            }
            return "[" + bare + "]";
        }

        /// <summary>The key's text with its <c>$1</c>, <c>$2</c> ... replaced by the values in order.</summary>
        public static string Format(string key, params object?[] values) => Fill(Get(key), values);

        /// <summary>The <c>$key</c> token for a field the game translates itself (an item or status effect name).</summary>
        public static string Token(string key) => "$" + Bare(key);

        /// <summary>Whether the key has English text.</summary>
        public static bool Has(string key) => English.ContainsKey(Bare(key));

        /// <summary>
        /// Adds (or replaces) one English word from code, for keys made from data at runtime; a translation of the key,
        /// if one is loaded, still wins. Returns its token.
        /// </summary>
        public static string Add(string key, string english)
        {
            string bare = Bare(key);
            English[bare] = english;
            global::Localization? game = GameTable.Current;
            if (game != null)
            {
                GameTable.WriteOne(game, bare, Get(bare));
                Version++;
                RaiseChanged();
            }
            return "$" + bare;
        }

        /// <summary>Replaces <c>$1</c>, <c>$2</c> ... in a text, the highest first so <c>$1</c> never eats <c>$10</c>.</summary>
        public static string Fill(string text, params object?[] values)
        {
            for (int i = values.Length; i >= 1; i--)
            {
                text = text.Replace("$" + i, Convert.ToString(values[i - 1], CultureInfo.CurrentCulture) ?? "");
            }
            return text;
        }

        internal static void Setup(global::Localization game, string language)
        {
            Language = language;
            translated = sources?.Translation(language, English) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            GameTable.Write(game, English, translated);
            Version++;
            RaiseChanged();
        }

        private static string Bare(string key) => key.Length > 0 && key[0] == '$' ? key.Substring(1) : key;

        // A listener's exception must not stop the words from being installed.
        private static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                sources?.Log.LogError($"a Lang.Changed handler threw: {e}");
            }
        }
    }
}
