namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A reference player maximum health, for the one owner-side decision that needs one - whether a Devouring
    /// creature's per-hit damage has grown past a share of a player's health and it should start hunting players. Like
    /// <see cref="PlayerSpeed"/>, it tracks the local player when there is one and remembers the last value, so a
    /// headless dedicated server (which never has a local player) still has a sane figure once any player has been
    /// observed. Before that, a documented fallback stands in - see DECISIONS.md on why it is what it is.
    /// </summary>
    internal static class PlayerReference
    {
        /// <summary>A mid-progression player's health; the stand-in until a real local player is seen on this machine.</summary>
        public const float FallbackHealth = 100f;

        private static float _health = FallbackHealth;

        public static float MaxHealth()
        {
            Player local = Player.m_localPlayer;
            if (local != null && local.GetMaxHealth() > 0f)
            {
                _health = local.GetMaxHealth();
            }
            return _health;
        }
    }
}
