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
    }
}
