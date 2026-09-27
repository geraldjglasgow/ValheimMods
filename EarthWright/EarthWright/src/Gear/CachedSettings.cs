using System.Collections.Generic;
using BepInEx.Configuration;

namespace EarthWright.Gear
{
    /// <summary>
    /// A number-list setting parsed once per change of its text, for code that reads it often (the crafting panel asks
    /// for station levels every frame, the brush asks for its radius cap every frame).
    /// </summary>
    public sealed class CachedNumbers
    {
        private readonly ConfigEntry<string> entry;
        private string parsedText;
        private List<float> numbers = new List<float>();

        public CachedNumbers(ConfigEntry<string> entry)
        {
            this.entry = entry;
        }

        public List<float> Current
        {
            get
            {
                string text = entry.Value ?? "";
                if (text != parsedText)
                {
                    parsedText = text;
                    numbers = SettingLists.Numbers(text);
                }
                return numbers;
            }
        }
    }

    /// <summary>A "feature:level" setting parsed once per change of its text; keys as <see cref="LevelCaps.Normalize"/> makes them.</summary>
    public sealed class CachedLevels
    {
        private readonly ConfigEntry<string> entry;
        private string parsedText;
        private Dictionary<string, int> levels = new Dictionary<string, int>();

        public CachedLevels(ConfigEntry<string> entry)
        {
            this.entry = entry;
        }

        public Dictionary<string, int> Current
        {
            get
            {
                string text = entry.Value ?? "";
                if (text == parsedText)
                    return levels;
                parsedText = text;
                levels = new Dictionary<string, int>();
                foreach (KeyValuePair<string, int> pair in SettingLists.Pairs(text))
                {
                    string key = LevelCaps.Normalize(pair.Key);
                    if (key != null)
                        levels[key] = pair.Value;
                }
                return levels;
            }
        }
    }
}
