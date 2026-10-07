namespace Hearthhold
{
    /// <summary>
    /// What stars do to a consumable's status effect (a mead's m_consumeStatusEffect). A restoring mead (health,
    /// stamina and eitr meads) is an SE_Stats that heals or adds stamina or eitr up front or over time; its m_ttl is
    /// also its re-drink cooldown (Player.CanConsumeItem refuses while the effect or its category is active), so its
    /// stars raise what it restores and never its time. Any other effect with a time (resistance, regeneration,
    /// carry weight, movement meads) is lasting: its stars lengthen m_ttl. An effect without a time gets nothing.
    /// </summary>
    public enum MeadKind
    {
        None,
        Restore,
        Lasting
    }

    /// <summary>Sorts a status effect into a <see cref="MeadKind"/>, read from its fields (prefab or instance alike).</summary>
    public static class MeadKinds
    {
        public static MeadKind Of(StatusEffect effect)
        {
            if (effect == null)
                return MeadKind.None;
            if (effect is SE_Stats stats && Restores(stats))
                return MeadKind.Restore;
            return effect.m_ttl > 0f ? MeadKind.Lasting : MeadKind.None;
        }

        private static bool Restores(SE_Stats stats) =>
            stats.m_healthUpFront > 0f || stats.m_healthOverTime > 0f
            || stats.m_staminaUpFront > 0f || stats.m_staminaOverTime > 0f
            || stats.m_eitrUpFront > 0f || stats.m_eitrOverTime > 0f;
    }
}
