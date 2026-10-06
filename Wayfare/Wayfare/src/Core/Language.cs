using System.Collections.Generic;
using HarmonyLib;

namespace Wayfare.Core
{
    /// <summary>English words of the mod. Registered in <see cref="Words"/> and handed to the game's localization
    /// when a language is set up, and at once when one already is. Keys are "wf_..." without the dollar.</summary>
    public static class Language
    {
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>();

        /// <summary>Goes up whenever a language is set up or a word is added to the one set up, so text localized once
        /// and kept (<see cref="LocalWord"/>, the hover lines) knows to localize again.</summary>
        public static int Revision { get; private set; }

        public static string Add(string key, string english)
        {
            words[key] = english;
            if (Localization.instance != null)
            {
                Localization.instance.AddWord(key, english);
                Revision++;
            }
            return "$" + key;
        }

        internal static void Install(Localization localization)
        {
            foreach (KeyValuePair<string, string> word in words)
                localization.AddWord(word.Key, word.Value);
            Revision++;
        }
    }

    /// <summary>A word localized once and again only when the language changes (<see cref="Language.Revision"/>), for
    /// text drawn every frame.</summary>
    internal sealed class LocalWord
    {
        private readonly string token;
        private string text;
        private int revision = -1;

        internal LocalWord(string token) => this.token = token;

        internal string Text
        {
            get
            {
                if (text == null || revision != Language.Revision)
                {
                    text = Localization.instance != null ? Localization.instance.Localize(token) : token;
                    revision = Language.Revision;
                }
                return text;
            }
        }
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class LanguageSetupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Localization __instance) => Language.Install(__instance);
    }
}
