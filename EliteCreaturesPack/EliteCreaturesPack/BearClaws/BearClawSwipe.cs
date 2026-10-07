namespace EliteCreaturesPack.BearClaws
{
    /// <summary>
    /// The rules of one swipe of a bear claw flurry (<see cref="BearClawFlurry"/>): which attacks start a flurry, and
    /// each swipe's copy of the attack cut so the three together cost what the punch they replace did and deal
    /// <see cref="Share"/> of its damage each.
    /// </summary>
    public static class BearClawSwipe
    {
        public const string Prefab = "FistBjornClaw";
        public const int Swipes = 3;

        /// <summary>
        /// Each swipe's share of the punch it replaces, for about 52 damage per second (the user: 50 to 55, by damage
        /// rather than timing). A flurry takes about 0.75 s, a left and right click (25 + 50 in the game) about 1.5 s:
        /// 1.05 of the punches' damage makes about 52, against the game's 47.
        /// </summary>
        private const float Share = 0.35f;

        /// <summary>The game doubles the last punch of a combo (Attack.DoMeleeAttack); a swipe's share takes it out again.</summary>
        private const float LastChainFactor = 2f;

        /// <summary>A player's primary attack with the bear claws, while the triple strike is on.</summary>
        public static bool IsClawPunch(Player player)
        {
            Attack? attack = player.m_currentAttack;
            return BearClawsSettings.TripleStrike && attack != null && !player.m_currentAttackIsSecondary
                && attack.m_weapon?.m_dropPrefab != null && attack.m_weapon.m_dropPrefab.name == Prefab;
        }

        /// <summary>
        /// One swipe's attack: 0.35 of what the punch would have done (<paramref name="punchLevel"/>, its place in the
        /// combo, decides that), a third of its skill and adrenaline; only the first swipe costs stamina, as the punch did.
        /// </summary>
        public static void Shape(Attack swipe, int punchLevel, int index)
        {
            swipe.m_damageMultiplier *= ChainFactor(swipe, punchLevel) / ChainFactor(swipe, swipe.m_currentAttackCainLevel) * Share;
            swipe.m_raiseSkillAmount /= Swipes;
            swipe.m_attackAdrenaline /= Swipes;
            if (index > 0)
            {
                swipe.m_attackStamina = 0f;
                swipe.m_attackHealth = 0f;
                swipe.m_attackHealthPercentage = 0f;
                swipe.m_attackUseAdrenaline = 0f;
            }
        }

        private static float ChainFactor(Attack attack, int level)
        {
            return attack.m_attackChainLevels > 1 && level == attack.m_attackChainLevels - 1 ? LastChainFactor : 1f;
        }
    }
}
