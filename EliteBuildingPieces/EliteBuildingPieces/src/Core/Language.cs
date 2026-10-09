using System.Collections.Generic;
using HarmonyLib;

namespace EliteBuildingPieces.Core
{
    /// <summary>
    /// English words of the mod. A module registers its keys in Initialize with <see cref="Add"/>; the words are
    /// handed to the game's localization when a language is set up and at once when one already is.
    /// Keys are "ebp_..." without the dollar; <see cref="Add"/> returns the "$ebp_..." token to put into texts.
    /// </summary>
    public static class Language
    {
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>();

        public static string Add(string key, string english)
        {
            words[key] = english;
            if (Localization.instance != null)
                Localization.instance.AddWord(key, english);
            return "$" + key;
        }

        internal static void Install(Localization localization)
        {
            foreach (KeyValuePair<string, string> word in words)
                localization.AddWord(word.Key, word.Value);
        }
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class LanguageSetupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Localization __instance) => Language.Install(__instance);
    }
}
