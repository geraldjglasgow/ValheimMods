using System.Collections.Generic;
using HarmonyLib;

namespace Wayfare.Core
{
    /// <summary>English words of the mod. Registered in <see cref="Words"/> and handed to the game's localization
    /// when a language is set up, and at once when one already is. Keys are "wf_..." without the dollar.</summary>
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
