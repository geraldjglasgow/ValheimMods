using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Soft Landing, Jumping's perk from level 0: falls are safe up to <see cref="SafeHeightAt100"/> m higher at Jump 100
    /// and the damage of a longer fall is up to <see cref="DamageSavingAt100"/> less, both linear in the level.
    /// <para>The game's rule (Character.UpdateGroundContact, players only): a fall of h metres, the highest point since
    /// the last ground contact (m_maxAirAltitude) down to the landing, hurts above <see cref="GameSafeHeight"/> m with
    /// Clamp01((h - 4) / 16) x 100 damage. The perk recomputes that from the real fall height with the raised safe height
    /// (the same 16 m from first harm to full damage), then takes the share; it never scales the game's capped number.
    /// <see cref="Landing"/> applies it; this class holds the numbers, for the perk and the skill page. At Jump 0 it is
    /// exactly the game's rule.</para>
    /// </summary>
    public static class SoftLanding
    {
        private const float GameSafeHeight = 4f;
        private const float GameDamageSpan = 16f;
        private const float GameMaxDamage = 100f;
        private const float SafeHeightAt100 = 4f;
        private const float DamageSavingAt100 = 0.5f;

        /// <summary>The highest fall that does no damage at a Jump factor (0..1), in metres.</summary>
        public static float SafeHeight(float factor) => GameSafeHeight + SafeHeightAt100 * Mathf.Clamp01(factor);

        /// <summary>The share of the fall damage left at a Jump factor (0..1).</summary>
        public static float DamageShare(float factor) => 1f - DamageSavingAt100 * Mathf.Clamp01(factor);

        /// <summary>The fall height from which the damage stops growing, in metres.</summary>
        public static float FullDamageHeight(float factor) => SafeHeight(factor) + GameDamageSpan;

        /// <summary>The most a fall can do, before status effects.</summary>
        public static float MaxDamage(float factor) => GameMaxDamage * DamageShare(factor);

        /// <summary>The damage of a fall of <paramref name="height"/> metres, before status effects.</summary>
        public static float Damage(float height, float factor) =>
            Mathf.Clamp01((height - SafeHeight(factor)) / GameDamageSpan) * MaxDamage(factor);
    }
}
