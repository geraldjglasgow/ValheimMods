using System.Globalization;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The odds table from section 3, parsed once per change of its text: the chance of 0..3 stars at an effective
    /// level, blended linearly between rows and clamped at the first and last row.
    /// </summary>
    public static class StarOdds
    {
        private static readonly string[] seen = new string[OddsSettings.Levels.Length];
        private static readonly float[][] rows = new float[OddsSettings.Levels.Length][];

        /// <summary>The chance of each star count (index = stars), adding up to 1.</summary>
        public static float[] At(float effectiveLevel)
        {
            Refresh();
            float[] levels = OddsSettings.Levels;
            if (effectiveLevel <= levels[0])
                return (float[])rows[0].Clone();
            for (int i = 1; i < levels.Length; i++)
            {
                if (effectiveLevel <= levels[i])
                {
                    float t = (effectiveLevel - levels[i - 1]) / (levels[i] - levels[i - 1]);
                    return Blend(rows[i - 1], rows[i], t);
                }
            }
            return (float[])rows[levels.Length - 1].Clone();
        }

        /// <summary>Rolls a star count at the effective level.</summary>
        public static int Roll(float effectiveLevel)
        {
            float[] odds = At(effectiveLevel);
            float roll = Random.value;
            for (int stars = 0; stars < Stars.Max; stars++)
            {
                if (roll < odds[stars])
                    return stars;
                roll -= odds[stars];
            }
            return Stars.Max;
        }

        /// <summary>The chance, 0..1, that a roll at the effective level gives at least the given stars.</summary>
        public static float ChanceAtLeast(float effectiveLevel, int minStars)
        {
            float[] odds = At(effectiveLevel);
            float chance = 0f;
            for (int stars = Mathf.Max(0, minStars); stars <= Stars.Max; stars++)
                chance += odds[stars];
            return Mathf.Clamp01(chance);
        }

        private static float[] Blend(float[] from, float[] to, float t)
        {
            float[] result = new float[Stars.Max + 1];
            for (int stars = 0; stars <= Stars.Max; stars++)
                result[stars] = Mathf.Lerp(from[stars], to[stars], t);
            return result;
        }

        private static void Refresh()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                string text = OddsSettings.Rows[i]?.Value ?? OddsSettings.DefaultRow(i);
                if (rows[i] != null && text == seen[i])
                    continue;
                seen[i] = text;
                rows[i] = Parse(text) ?? Parse(OddsSettings.DefaultRow(i));
            }
        }

        /// <summary>Four non-negative numbers scaled to add up to 1, or null when the text is not that.</summary>
        private static float[] Parse(string text)
        {
            string[] parts = (text ?? "").Split(',');
            if (parts.Length != Stars.Max + 1)
                return null;
            float[] row = new float[parts.Length];
            float total = 0f;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out row[i]) || row[i] < 0f)
                    return null;
                total += row[i];
            }
            if (total <= 0f)
                return null;
            for (int i = 0; i < row.Length; i++)
                row[i] /= total;
            return row;
        }
    }
}
