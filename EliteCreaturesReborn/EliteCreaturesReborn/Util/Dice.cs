using UnityEngine;

namespace EliteCreaturesReborn.Util
{
    /// <summary>Percentage dice. All chance rolls in the mod go through here so the convention is uniform.</summary>
    internal static class Dice
    {
        /// <summary>True with the given percentage (0..100). A chance of 0 never fires, 100 always does.</summary>
        public static bool Percent(float chancePercent)
        {
            if (chancePercent <= 0f)
            {
                return false;
            }
            return Random.Range(0f, 100f) <= chancePercent;
        }

        /// <summary>A fresh 0..100 draw, for stepping through a probability table.</summary>
        public static float Draw() => Random.Range(0f, 100f);

        /// <summary>An amount rounded to a whole number at random by its fraction: 2.3 is 3 three times in ten, else 2.
        /// On average it is the amount itself.</summary>
        public static int Round(float amount)
        {
            int whole = Mathf.FloorToInt(amount);
            return Random.value < amount - whole ? whole + 1 : whole;
        }
    }
}
