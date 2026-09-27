using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Shared pieces of the animal lore lines: whether the local player sees them (Husbandry on and the Animal Lore Level
    /// reached) and how a time span reads ("40 s", "12 min", "1 h 5 min").
    /// </summary>
    public static class LoreText
    {
        public static bool Visible =>
            HusbandrySkill.Active && HusbandrySkill.Reaches(HusbandrySkill.Local(), HusbandrySettings.LoreLevel.Value);

        public static string Duration(double seconds)
        {
            if (seconds < 60.0)
                return $"{Mathf.Max(1, Mathf.CeilToInt((float)seconds))} s";
            int minutes = Mathf.CeilToInt((float)(seconds / 60.0));
            return minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h {minutes % 60} min";
        }

        /// <summary>Adds <paramref name="line"/> on a new line; an empty line is skipped.</summary>
        public static string Add(string text, string line)
        {
            if (string.IsNullOrEmpty(line))
                return text;
            return string.IsNullOrEmpty(text) ? line : text + "\n" + line;
        }
    }
}
