using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EliteCrafting.Rolling;
using EliteCrafting.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EliteCrafting.Epic
{
    /// <summary>
    /// New effects for an Epic Loot item, rolled the way Epic Loot rolls a dropped item's: the types its API allows on
    /// the item as it stands (<c>GetAvailableEffectTypes</c>, asked again after each pick, so no type comes twice), picked
    /// by each definition's <c>SelectionWeight</c>, the value a whole number of <c>Increment</c> steps above
    /// <c>MinValue</c>, at most <c>MaxValue</c>, for the item's rarity (just <c>MinValue</c> without an increment, 1 for
    /// an effect without values). Local client only, on a working copy.
    /// </summary>
    internal static class EpicEffects
    {
        /// <summary>Adds up to <paramref name="count"/> effects; returns how many could be added.</summary>
        public static int Add(ItemDrop.ItemData target, EpicItem item, int count, Random random)
        {
            int added = 0;
            while (added < count && AddOne(target, item, random))
            {
                added++;
            }
            return added;
        }

        /// <summary>The newest effect in Epic Loot's own words, for the rune's message ("+6% Damage").</summary>
        public static string LastText(EpicItem item)
        {
            (string type, float value) = item.LastEffect();
            JObject? def = Definition(type);
            string format = Words.Localize((string?)def?["DisplayText"] ?? "");
            if (string.IsNullOrEmpty(format) || format.StartsWith("[", StringComparison.Ordinal))
            {
                return Regex.Replace(type, "(?<=[a-z])(?=[A-Z])", " ");
            }
            try
            {
                return string.Format(format, value);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        private static bool AddOne(ItemDrop.ItemData target, EpicItem item, Random random)
        {
            List<(string Type, JObject Def)> choices = Choices(EpicApi.AvailableEffects(target, item.Json, item.Rarity));
            List<float> weights = choices.ConvertAll(c => (float?)c.Def["SelectionWeight"] ?? 1f);
            int pick = RollMath.PickWeighted(weights, random);
            if (pick < 0)
            {
                return false;
            }
            item.AddEffect(choices[pick].Type, Value(choices[pick].Def, item.RarityName, random));
            return true;
        }

        // Types whose definition cannot be read are left out.
        private static List<(string Type, JObject Def)> Choices(List<string> types)
        {
            List<(string Type, JObject Def)> choices = new List<(string Type, JObject Def)>(types.Count);
            foreach (string type in types)
            {
                JObject? def = Definition(type);
                if (def != null)
                {
                    choices.Add((type, def));
                }
            }
            return choices;
        }

        private static float Value(JObject def, string rarity, Random random)
        {
            if (!(def["ValuesPerRarity"]?[rarity] is JObject values))
            {
                return 1f;
            }
            float min = (float?)values["MinValue"] ?? 1f;
            float max = (float?)values["MaxValue"] ?? min;
            float step = (float?)values["Increment"] ?? 0f;
            if (step == 0f)
            {
                return min;
            }
            int steps = Math.Max((int)((max - min) / step), 0);
            return min + random.Next(0, steps + 1) * step;
        }

        private static JObject? Definition(string type)
        {
            string json = type.Length == 0 ? "" : EpicApi.EffectDefinition(type);
            if (json.Length == 0)
            {
                return null;
            }
            try
            {
                return JToken.Parse(json) as JObject;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
