namespace GrindstoneSkills
{
    /// <summary>
    /// Desperation: below Desperation Health percent of max health, the core Damage Reduction is multiplied by
    /// Desperation Multiplier (<see cref="Reductions"/>). It follows Damage Reduction, so it does nothing at level 0.
    /// An icon shows while it holds (<see cref="DefenseEffects"/>).
    /// </summary>
    public static class Desperation
    {
        public static bool Holds(Player player)
        {
            float threshold = DefenseGuardSettings.DesperationHealth.Value / 100f;
            if (player == null || threshold <= 0f || DefenseGuardSettings.DesperationMultiplier.Value <= 1f)
                return false;
            if (DefenseSkill.LocalShare(DefenseSettings.DamageReduction.Value) <= 0f)
                return false;
            float max = player.GetMaxHealth();
            return max > 0f && player.GetHealth() > 0f && player.GetHealth() / max < threshold;
        }
    }
}
