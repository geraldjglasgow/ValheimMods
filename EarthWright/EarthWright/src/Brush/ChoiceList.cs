using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace EarthWright.Brush
{
    /// <summary>
    /// A comma-separated list of enum names in a string setting ("Circle, Square, Ring"), parsed once per change of the
    /// setting's text. Unknown names are logged once and skipped, so a typo never breaks the key that cycles the list.
    /// </summary>
    public sealed class ChoiceList<T> where T : struct, Enum
    {
        private readonly Func<ConfigEntry<string>> entry;
        private string parsedText;
        private List<T> parsed = new List<T>();

        public ChoiceList(Func<ConfigEntry<string>> entry)
        {
            this.entry = entry;
        }

        /// <summary>The allowed values, in the order of the setting.</summary>
        public List<T> Values
        {
            get
            {
                ConfigEntry<string> setting = entry();
                string text = setting != null ? setting.Value : "";
                if (text != parsedText)
                {
                    parsedText = text;
                    parsed = Parse(text, setting?.Definition.Key);
                }
                return parsed;
            }
        }

        public bool Contains(T value) => Values.Contains(value);

        private static List<T> Parse(string text, string key)
        {
            List<T> values = new List<T>();
            foreach (string part in (text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (name.Length == 0)
                    continue;
                if (Enum.TryParse(name, true, out T value) && Enum.IsDefined(typeof(T), value))
                {
                    if (!values.Contains(value))
                        values.Add(value);
                }
                else
                {
                    Plugin.Log.LogWarning($"EarthWright: '{name}' in the setting '{key}' is not one of {string.Join(", ", Enum.GetNames(typeof(T)))}; ignored.");
                }
            }
            return values;
        }
    }
}
