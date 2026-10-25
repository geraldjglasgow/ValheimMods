using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Translations
{
    /// <summary>
    /// The game's word table, reached by reflection (no publicizer needed): its private instance, the private
    /// <c>m_translations</c> dictionary and the <c>m_cache</c> of localized texts (which also keeps misses, so it is emptied
    /// after every write). One postfix on <c>Localization.SetupLanguage(string)</c>, installed by hand once per merged copy.
    /// <c>SetLanguage</c> clears the table before it sets a language up, so the postfix always writes every word again.
    /// </summary>
    internal static class GameTable
    {
        private static readonly FieldInfo? InstanceField = AccessTools.Field(typeof(global::Localization), "m_instance");
        private static readonly FieldInfo? TableField = AccessTools.Field(typeof(global::Localization), "m_translations");
        private static readonly FieldInfo? CacheField = AccessTools.Field(typeof(global::Localization), "m_cache");
        private static MethodInfo? evictAll;
        private static Action<global::Localization, string>? setup;
        private static ManualLogSource? log;
        private static bool installed;

        /// <summary>The game's localization if it exists yet; never creates it.</summary>
        public static global::Localization? Current => InstanceField?.GetValue(null) as global::Localization;

        /// <summary>
        /// Whether a table is the game's own: the one it shows, or the first, still being made (its constructor sets
        /// languages up before <c>m_instance</c> is assigned). Some mods make spare tables of their own, set up in
        /// English to name their items in config files; those are not the player's language.
        /// </summary>
        public static bool IsGames(global::Localization table)
        {
            global::Localization? current = Current;
            return current == null || ReferenceEquals(current, table);
        }

        public static void Install(Harmony harmony, Action<global::Localization, string> onSetup, ManualLogSource logSource)
        {
            setup = onSetup;
            log = logSource;
            if (installed)
            {
                return;
            }
            installed = true;
            MethodInfo target = AccessTools.Method(typeof(global::Localization), nameof(global::Localization.SetupLanguage),
                new[] { typeof(string) });
            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(GameTable), nameof(Postfix))));
        }

        /// <summary>Every English key with its translation if there is one, else its English.</summary>
        public static void Write(global::Localization game, Dictionary<string, string> english, Dictionary<string, string> translated)
        {
            if (!(TableField?.GetValue(game) is Dictionary<string, string> table))
            {
                log?.LogError("the game's word table was not found: words show as [keys]");
                return;
            }
            foreach (KeyValuePair<string, string> word in english)
            {
                table[word.Key] = translated.TryGetValue(word.Key, out string text) ? text : word.Value;
            }
            Evict(game);
        }

        public static void WriteOne(global::Localization game, string key, string text)
        {
            if (TableField?.GetValue(game) is Dictionary<string, string> table)
            {
                table[key] = text;
                Evict(game);
            }
        }

        private static void Evict(global::Localization game)
        {
            object? cache = CacheField?.GetValue(game);
            if (cache == null)
            {
                return;
            }
            evictAll ??= AccessTools.Method(cache.GetType(), "EvictAll");
            evictAll?.Invoke(cache, null);
        }

        private static void Postfix(global::Localization __instance, string language)
        {
            try
            {
                setup?.Invoke(__instance, language);
            }
            catch (Exception e)
            {
                log?.LogError($"words for {language} not installed: {e}");
            }
        }
    }
}
