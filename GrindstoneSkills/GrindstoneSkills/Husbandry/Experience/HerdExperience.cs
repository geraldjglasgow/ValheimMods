using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The experience between two snapshots of a creature, before its tier: a share of Taming Experience for the taming
    /// progress, Tamed Experience when it became tame while being tamed (times the Discovery Multiplier for the
    /// character's first of that kind, recorded then), Feeding Experience when it ate while tamed or being tamed, and
    /// Birth Experience per birth. Runs on the keeper's own client.
    /// </summary>
    public static class HerdExperience
    {
        private const int MaxBirthsPerScan = 4;

        public static float Between(HerdSnapshot before, HerdSnapshot now, Player player, string prefab)
        {
            float amount = Taming(before, now) + Tamed(before, now, player, prefab);
            if (now.LastFed != before.LastFed && now.LastFed != 0L && (now.Tamed || now.BeingTamed))
                amount += Mathf.Max(0f, HusbandryExperienceSettings.Feeding.Value);
            int births = Mathf.Clamp(now.Births - before.Births, 0, MaxBirthsPerScan);
            return amount + births * Mathf.Max(0f, HusbandryExperienceSettings.Birth.Value);
        }

        private static float Taming(HerdSnapshot before, HerdSnapshot now)
        {
            if (before.Tamed || now.Tamed || now.TamingLeft >= before.TamingLeft)
                return 0f;
            return (before.TamingLeft - now.TamingLeft) / now.TamingTime * Mathf.Max(0f, HusbandryExperienceSettings.Taming.Value);
        }

        private static float Tamed(HerdSnapshot before, HerdSnapshot now, Player player, string prefab)
        {
            if (!now.Tamed || !before.BeingTamed)
                return 0f;
            float amount = Mathf.Max(0f, HusbandryExperienceSettings.Tamed.Value);
            return TameDiscovery.TryRecord(player, prefab) ? amount * HusbandryExperienceSettings.Discovery.Value : amount;
        }
    }
}
