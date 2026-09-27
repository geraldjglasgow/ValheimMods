using System;
using System.Collections.Generic;
using System.Globalization;

namespace EarthWright.Gear
{
    /// <summary>
    /// Reads the list-shaped text settings of the Tools and Cultivator sections: "Item:Amount" pairs, "name:level"
    /// pairs and comma-separated numbers. Malformed parts are skipped, so a typo never throws.
    /// </summary>
    public static class SettingLists
    {
        /// <summary>"Wood:5, Flint:3" as (name, amount) pairs; a missing amount counts as 1, a negative one as 0.</summary>
        public static List<KeyValuePair<string, int>> Pairs(string text)
        {
            List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrWhiteSpace(text))
                return result;
            foreach (string part in text.Split(','))
            {
                string[] pair = part.Split(':');
                string name = pair[0].Trim();
                if (name.Length == 0)
                    continue;
                int amount = 1;
                if (pair.Length > 1 && !int.TryParse(pair[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
                    amount = 1;
                result.Add(new KeyValuePair<string, int>(name, Math.Max(0, amount)));
            }
            return result;
        }

        /// <summary>"1, 2, 3.5" as numbers; unreadable parts are skipped.</summary>
        public static List<float> Numbers(string text)
        {
            List<float> result = new List<float>();
            if (string.IsNullOrWhiteSpace(text))
                return result;
            foreach (string part in text.Split(','))
            {
                if (float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    result.Add(value);
            }
            return result;
        }

        /// <summary>The number for a 1-based level: levels past the end of the list use its last number; null when the list is empty.</summary>
        public static float? ForLevel(List<float> numbers, int level)
        {
            if (numbers == null || numbers.Count == 0)
                return null;
            int index = Math.Max(0, Math.Min(level, numbers.Count) - 1);
            return numbers[index];
        }

        /// <summary>Comma-separated names, trimmed, empty ones dropped.</summary>
        public static List<string> Names(string text)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;
            foreach (string part in text.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    result.Add(name);
            }
            return result;
        }
    }
}
