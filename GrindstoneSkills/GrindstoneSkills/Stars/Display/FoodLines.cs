using System;
using System.Globalization;

namespace GrindstoneSkills
{
    /// <summary>
    /// Raises the food values in a tooltip's text to what a starred dish gives when eaten. ItemData.GetTooltip prints
    /// each value in the first colour tag after its label, before localization: "$item_food_health:
    /// &lt;color=#ff8080ff&gt;44&lt;/color&gt;" (the float in the current culture), likewise stamina and eitr, and
    /// "$item_food_duration: &lt;color=orange&gt;30m &lt;/color&gt;" (GetDurationString of m_foodBurnTime). Health,
    /// stamina and eitr rise by StarBonus.Food, the duration by StarBonus.Duration; each value becomes the boosted value
    /// followed by the bonus in gold, "48.4 (+4.4 ★)". A value another mod has reworded is left as it is.
    /// </summary>
    public static class FoodLines
    {
        private static readonly string[] ValueKeys = { "$item_food_health:", "$item_food_stamina:", "$item_food_eitr:" };
        private const string DurationKey = "$item_food_duration:";
        private const string Open = "<color=";
        private const string Close = "</color>";

        public static string Boost(string text, ItemDrop.ItemData.SharedData shared, int stars)
        {
            float food = StarBonus.Food(stars);
            if (food > 0f)
                foreach (string key in ValueKeys)
                    text = BoostValue(text, key, food, stars);
            float duration = StarBonus.Duration(stars);
            if (duration > 0f && shared.m_foodBurnTime > 0f)
                text = BoostDuration(text, shared.m_foodBurnTime, duration, stars);
            return text;
        }

        private static string BoostValue(string text, string key, float bonus, int stars)
        {
            if (!Find(text, key, out int start, out int end))
                return text;
            string shown = text.Substring(start, end - start);
            if (!float.TryParse(shown, NumberStyles.Float, CultureInfo.CurrentCulture, out float value))
                return text;
            float extra = value * bonus;
            return Replace(text, start, end, Number(value + extra), Number(extra), stars);
        }

        private static string BoostDuration(string text, float burnTime, float bonus, int stars)
        {
            if (!Find(text, DurationKey, out int start, out int end))
                return text;
            if (text.Substring(start, end - start) != ItemDrop.ItemData.GetDurationString(burnTime))
                return text;
            float extra = burnTime * bonus;
            return Replace(text, start, end, Duration(burnTime + extra), Duration(extra), stars);
        }

        /// <summary>The value's span in the first colour tag after the key, on the key's own line.</summary>
        private static bool Find(string text, string key, out int start, out int end)
        {
            start = end = -1;
            int at = text.IndexOf(key, StringComparison.Ordinal);
            if (at < 0)
                return false;
            int lineEnd = text.IndexOf('\n', at);
            if (lineEnd < 0)
                lineEnd = text.Length;
            int tag = text.IndexOf(Open, at + key.Length, StringComparison.Ordinal);
            if (tag < 0 || tag > lineEnd)
                return false;
            start = text.IndexOf('>', tag) + 1;
            end = start > 0 ? text.IndexOf(Close, start, StringComparison.Ordinal) : -1;
            return start > 0 && end >= start && end < lineEnd;
        }

        /// <summary>Puts the boosted value in the tag and the gold bonus after it; the value's own closing tag ends the bonus.</summary>
        private static string Replace(string text, int start, int end, string value, string extra, int stars)
        {
            string bonus = Close + " " + Open + StarText.Gold + ">(+" + extra + " " + StarText.Glyphs(stars) + ")";
            return text.Substring(0, start) + value + bonus + text.Substring(end);
        }

        private static string Number(float value) => value.ToString("0.#", CultureInfo.CurrentCulture);

        private static string Duration(float seconds) => ItemDrop.ItemData.GetDurationString(seconds).Trim();
    }
}
