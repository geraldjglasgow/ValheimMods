using System;
using System.Collections.Generic;
using System.Text;

namespace EarthWright.Core
{
    /// <summary>
    /// Localizes texts that carry numbers ("$ew_preview_ground 12.34 m") one $token at a time. The game's
    /// <c>Localization.Localize</c> keeps only the last 100 whole texts it translated, so handing it a text with new
    /// numbers every frame pushes every other text (the game's and other mods') out of that cache. Here each token is
    /// translated once per language (<see cref="Language.Version"/>) and the rest of the text is copied around it.
    /// Tokens end where the game ends them; <c>$KEY_</c> tokens follow the bound key and go to the game every time.
    /// </summary>
    public static class TokenText
    {
        private const string KeyPrefix = "$KEY_";

        private static readonly Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly StringBuilder builder = new StringBuilder(128);
        private static int wordsFor = -1;

        public static string Localize(string text)
        {
            Localization localization = Localization.m_instance;
            if (string.IsNullOrEmpty(text) || localization == null || text.IndexOf('$') < 0)
                return text;
            if (wordsFor != Language.Version)
            {
                words.Clear();
                wordsFor = Language.Version;
            }
            builder.Clear();
            int start = 0;
            int at;
            while (start < text.Length - 1 && (at = text.IndexOf('$', start)) >= 0)
            {
                int end = text.IndexOfAny(localization.m_endChars, at);
                if (end < 0)
                    end = text.Length;
                builder.Append(text, start, at - start).Append(Word(localization, text.Substring(at, end - at)));
                start = end;
            }
            return builder.Append(text, start, text.Length - start).ToString();
        }

        private static string Word(Localization localization, string token)
        {
            if (token.StartsWith(KeyPrefix, StringComparison.Ordinal))
                return localization.Localize(token);
            if (!words.TryGetValue(token, out string word))
                words[token] = word = localization.Localize(token);
            return word;
        }
    }
}
