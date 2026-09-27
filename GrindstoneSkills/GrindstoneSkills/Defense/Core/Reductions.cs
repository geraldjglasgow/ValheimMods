using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Every way Defense lowers the damage the local player takes, multiplied together so none can reach 100%:
    /// <list type="bullet">
    /// <item>Damage Reduction, the core perk, stronger while Desperation holds (<see cref="Desperation"/>);</item>
    /// <item>Hardened's stacks (<see cref="Hardened"/>);</item>
    /// <item>Shield Wall, when another player shelters this one (<see cref="ShieldWall"/>).</item>
    /// </list>
    /// </summary>
    public static class Reductions
    {
        private const float MaxEach = 0.9f;

        /// <summary>The share of the damage the local player still takes, 0.1 to 1.</summary>
        public static float Keep(Player player)
        {
            float keep = 1f - Toughness(player);
            keep *= 1f - Mathf.Min(MaxEach, Hardened.Reduction());
            keep *= 1f - Mathf.Min(MaxEach, ShieldWall.Reduction(player));
            return keep;
        }

        /// <summary>The core Damage Reduction at the local player's level, with Desperation's multiplier when it holds.</summary>
        public static float Toughness(Player player)
        {
            float share = DefenseSkill.LocalShare(DefenseSettings.DamageReduction.Value);
            if (Desperation.Holds(player))
                share *= Mathf.Max(1f, DefenseGuardSettings.DesperationMultiplier.Value);
            return Mathf.Min(MaxEach, share);
        }
    }
}
