using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>What a dish's stars are worth when eaten, from section 2.</summary>
    public static class StarBonus
    {
        /// <summary>The share added to health, stamina and eitr: 0 for 0 stars, 0.35 for a +35% bonus.</summary>
        public static float Food(int stars) => Percent(StarSettings.FoodBonus, stars);

        /// <summary>The share added to the duration: 0 for 0 stars, 0.3 for a +30% bonus.</summary>
        public static float Duration(int stars) => Percent(StarSettings.DurationBonus, stars);

        private static float Percent(BepInEx.Configuration.ConfigEntry<float>[] entries, int stars)
        {
            stars = Mathf.Clamp(stars, 0, Stars.Max);
            if (stars == 0 || entries[stars] == null)
                return 0f;
            return Mathf.Max(0f, entries[stars].Value) / 100f;
        }
    }
}
