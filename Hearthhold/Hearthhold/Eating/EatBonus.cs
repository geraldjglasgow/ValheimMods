using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// What stars are worth when an item is eaten or drunk, as shares (0.2 for +20%). Fixed numbers: a dish gets +10%
    /// health, stamina and eitr and +10% duration per star; a mead that restores (<see cref="MeadKind.Restore"/>)
    /// restores +10% per star; a mead with a lasting effect (<see cref="MeadKind.Lasting"/>) lasts +15% per star.
    /// </summary>
    public static class EatBonus
    {
        private const float FoodPerStar = 0.10f;
        private const float DurationPerStar = 0.10f;
        private const float RestorePerStar = 0.10f;
        private const float LastingPerStar = 0.15f;

        public static float Food(int stars) => FoodPerStar * Clamp(stars);

        public static float Duration(int stars) => DurationPerStar * Clamp(stars);

        public static float Restore(int stars) => RestorePerStar * Clamp(stars);

        public static float Lasting(int stars) => LastingPerStar * Clamp(stars);

        /// <summary>A share as a whole percent: 0.2 is 20.</summary>
        public static int Percent(float share) => Mathf.RoundToInt(share * 100f);

        /// <summary>
        /// A value as a whole number, the way the user asked (2026-10-06): .5 and below round down, above .5 up
        /// (9.5 -> 9, 9.6 -> 10). Every boosted number a player sees or gets goes through this.
        /// </summary>
        public static float Whole(float value) => value <= 0f ? 0f : Mathf.Ceil(value - 0.5f - 0.001f);

        /// <summary>A base value raised by a bonus share, as a whole number: 7 health +20% = 8.</summary>
        public static float Boosted(float baseValue, float bonus) => Whole(baseValue * (1f + bonus));

        /// <summary>The share a base value really rises by once rounded (7 -> 8 is +14%); 0 for no base.</summary>
        public static float RoundedShare(float baseValue, float bonus) =>
            baseValue > 0f ? Boosted(baseValue, bonus) / baseValue - 1f : 0f;

        private static int Clamp(int stars) => Mathf.Clamp(stars, 0, Stars.Max);
    }
}
