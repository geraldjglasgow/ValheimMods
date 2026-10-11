using System;
using System.Linq;
using System.Text;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Enum values as the files write them: words in any case, with or without spaces, so <c>very weak</c>,
    /// <c>VeryWeak</c> and <c>very_weak</c> all read as <c>HitData.DamageModifier.VeryWeak</c>, and <c>forest monsters</c>
    /// as <c>Character.Faction.ForestMonsters</c>. Messages list the choices the way the files write them.
    /// </summary>
    internal static class Words
    {
        public static bool TryParse<T>(string text, out T value) where T : struct, Enum
        {
            string wanted = Squash(text);
            foreach (T candidate in (T[])Enum.GetValues(typeof(T)))
            {
                if (Squash(candidate.ToString()) == wanted)
                {
                    value = candidate;
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>Every value of the enum as words: "normal, resistant, weak, very resistant".</summary>
        public static string Choices<T>() where T : struct, Enum =>
            string.Join(", ", Enum.GetNames(typeof(T)).Select(Spaced));

        /// <summary>"VeryWeak" as "very weak".</summary>
        public static string Spaced(string name)
        {
            StringBuilder words = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    words.Append(' ');
                }
                words.Append(char.ToLowerInvariant(name[i]));
            }
            return words.ToString();
        }

        private static string Squash(string text)
        {
            StringBuilder squashed = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c != ' ' && c != '_' && c != '-')
                {
                    squashed.Append(char.ToLowerInvariant(c));
                }
            }
            return squashed.ToString();
        }
    }
}
