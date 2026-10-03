using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars are switched off: every roll gives 0 stars, so no dish, pick, crop or seed is ever starred and
    /// everything stacks like vanilla. The callers and the odds the skill pages show stay as they were.
    /// </summary>
    public static class StarOdds
    {
        /// <summary>The chance of each star count (index = stars): always 0 stars.</summary>
        public static float[] At(float effectiveLevel) => new float[] { 1f, 0f, 0f, 0f };

        /// <summary>Rolls a star count at the effective level: always 0.</summary>
        public static int Roll(float effectiveLevel) => 0;

        /// <summary>The chance, 0..1, that a roll gives at least the given stars: 0 above 0 stars.</summary>
        public static float ChanceAtLeast(float effectiveLevel, int minStars) => minStars <= 0 ? 1f : 0f;
    }
}
