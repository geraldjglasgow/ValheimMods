namespace Hearthhold
{
    /// <summary>
    /// Makes the status effect a player just got from a starred mead stronger. It works on the player's own instance,
    /// the clone SEMan.AddStatusEffect made (StatusEffect.Clone is a MemberwiseClone, so the float fields are its own;
    /// lists such as m_mods are shared with the prefab and never touched), after SE_Stats.Setup has run:
    /// <list type="bullet">
    /// <item>Setup already applied the up-front heal, stamina and eitr (StartupEffects), so the extra share is applied
    /// here and the fields are raised too, in case ResetTime applies them again.</item>
    /// <item>Health over time: Setup fixed m_healthOverTimeTickHP = m_healthOverTime / ticks, and the update heals
    /// that per tick, so the tick amount is raised (and m_healthOverTime, which the effect's tooltip shows).</item>
    /// <item>Stamina and eitr over time are read from m_staminaOverTime and m_eitrOverTime on every update, so raising
    /// them is enough.</item>
    /// <item>Lasting effects: IsDone compares the elapsed time with m_ttl, read on every update, and the HUD shows
    /// m_ttl minus the elapsed time, so a longer m_ttl is a longer effect.</item>
    /// </list>
    /// Status effects are not saved with the character, so nothing needs to survive a relog.
    /// </summary>
    public static class MeadBoost
    {
        public static void Apply(StatusEffect effect, int stars)
        {
            MeadKind kind = MeadKinds.Of(effect);
            if (kind == MeadKind.Restore)
                Restore((SE_Stats)effect, EatBonus.Restore(stars));
            else if (kind == MeadKind.Lasting)
                effect.m_ttl *= 1f + EatBonus.Lasting(stars);
        }

        private static void Restore(SE_Stats stats, float bonus)
        {
            if (bonus <= 0f || stats.m_character == null)
                return;
            UpFront(stats, bonus);
            if (stats.m_healthOverTime > 0f)
            {
                stats.m_healthOverTimeTickHP *= 1f + EatBonus.RoundedShare(stats.m_healthOverTime, bonus);
                stats.m_healthOverTime = EatBonus.Boosted(stats.m_healthOverTime, bonus);
            }
            if (stats.m_staminaOverTime > 0f)
                stats.m_staminaOverTime *= 1f + EatBonus.RoundedShare(stats.m_staminaOverTime, bonus);
            if (stats.m_eitrOverTime > 0f)
                stats.m_eitrOverTime *= 1f + EatBonus.RoundedShare(stats.m_eitrOverTime, bonus);
        }

        private static void UpFront(SE_Stats stats, float bonus)
        {
            Character character = stats.m_character;
            if (stats.m_healthUpFront > 0f)
            {
                character.Heal(EatBonus.Boosted(stats.m_healthUpFront, bonus) - stats.m_healthUpFront);
                stats.m_healthUpFront = EatBonus.Boosted(stats.m_healthUpFront, bonus);
            }
            if (stats.m_staminaUpFront > 0f)
            {
                character.AddStamina(EatBonus.Boosted(stats.m_staminaUpFront, bonus) - stats.m_staminaUpFront);
                stats.m_staminaUpFront = EatBonus.Boosted(stats.m_staminaUpFront, bonus);
            }
            if (stats.m_eitrUpFront > 0f)
            {
                character.AddEitr(EatBonus.Boosted(stats.m_eitrUpFront, bonus) - stats.m_eitrUpFront);
                stats.m_eitrUpFront = EatBonus.Boosted(stats.m_eitrUpFront, bonus);
            }
        }
    }
}
