using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Translations;

namespace Hearthhold
{
    /// <summary>
    /// The stats a food or mead gives, by name and then their raised values, in the player's language: "health and
    /// stamina (60 / 24)". Only stats above 0 are named; "" when none is.
    /// </summary>
    public static class StatLine
    {
        private static readonly List<string> names = new List<string>(3);
        private static readonly StringBuilder numbers = new StringBuilder();

        public static string Values(float health, float stamina, float eitr, float factor)
        {
            names.Clear();
            numbers.Clear();
            Add(health, "hh_stat_health", factor);
            Add(stamina, "hh_stat_stamina", factor);
            Add(eitr, "hh_stat_eitr", factor);
            return names.Count == 0 ? "" : Lang.Format("hh_stats", Names(), numbers.ToString());
        }

        private static void Add(float value, string key, float factor)
        {
            if (value <= 0f)
                return;
            names.Add(Lang.Get(key));
            if (numbers.Length > 0)
                numbers.Append(" / ");
            numbers.Append(EatBonus.Whole(value * factor).ToString("0", CultureInfo.InvariantCulture));
        }

        /// <summary>The named stats as a list: "health", "health and stamina", "health, stamina and eitr".</summary>
        private static string Names()
        {
            if (names.Count == 1)
                return names[0];
            if (names.Count == 2)
                return Lang.Format("hh_list_two", names[0], names[1]);
            return Lang.Format("hh_list_three", names[0], names[1], names[2]);
        }
    }
}
