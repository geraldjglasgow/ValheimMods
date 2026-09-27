using System;
using System.Globalization;

namespace EarthWright.Clearing
{
    /// <summary>Reading numbers and words from console arguments (numbers always with a dot, whatever the system language).</summary>
    public static class CommandInput
    {
        public static bool TryFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>The number at <paramref name="index"/>, or the fallback when it is missing or not a number.</summary>
        public static float Number(Terminal.ConsoleEventArgs args, int index, float fallback)
        {
            return args.Length > index && TryFloat(args[index], out float value) ? value : fallback;
        }

        /// <summary>Whether any argument after the subcommand is this word (case ignored).</summary>
        public static bool HasWord(Terminal.ConsoleEventArgs args, string word)
        {
            for (int i = 2; i < args.Length; i++)
            {
                if (string.Equals(args[i], word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>"min:max" as two heights, the smaller first.</summary>
        public static bool TryRange(string text, out float min, out float max)
        {
            min = max = 0f;
            string[] parts = text.Split(':');
            if (parts.Length != 2 || !TryFloat(parts[0], out float a) || !TryFloat(parts[1], out float b))
                return false;
            min = Math.Min(a, b);
            max = Math.Max(a, b);
            return true;
        }
    }
}
