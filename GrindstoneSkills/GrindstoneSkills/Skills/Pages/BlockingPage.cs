namespace GrindstoneSkills
{
    /// <summary>
    /// Blocking's page in the info pane, from the game's code (Humanoid.BlockAttack, ItemDrop.ItemData.GetBlockPower):
    /// block armor +50% at level 100, what that makes of the blocker in hand and of its parry, and a parry effect that
    /// grows with the Blocking level (SE_Shield) when the blocker or the player has one. GrindstoneSkills changes none
    /// of it; its own Defense skill has its own page.
    /// </summary>
    public static class BlockingPage
    {
        /// <summary>The game's block armor bonus at level 100 and its parry window, in seconds.</summary>
        private const float ArmorAt100 = 0.5f;
        private const float ParryWindow = 0.25f;

        public static void Write(SkillPage page)
        {
            page.About = "Makes your blocks stronger. Trained by blocking hits, a parry counts double.";
            page.Line($"Block armor +{SkillPage.Percent(ArmorAt100 * page.Factor)}", "Block armor",
                $"How much damage a block stops, the number in brackets on a shield's tooltip. A block costs stamina by the share of block armor it used, so a stronger block costs less. +{SkillPage.Percent(ArmorAt100)} at level 100.");
            ItemDrop.ItemData blocker = page.Player.GetCurrentBlocker();
            if (blocker != null)
                Held(page, blocker);
        }

        /// <summary>The blocker in hand (the left-hand item, else the weapon): its block armor now and its parry.</summary>
        private static void Held(SkillPage page, ItemDrop.ItemData blocker)
        {
            float armor = blocker.GetBlockPower(page.Factor);
            float untrained = blocker.GetBaseBlockPower();
            string name = Localized(blocker.m_shared.m_name);
            page.Line($"{name}: block armor {armor:0}", name,
                $"{untrained:0} untrained, {untrained * (1f + ArmorAt100):0} at level 100.");
            float bonus = blocker.m_shared.m_timedBlockBonus;
            if (bonus <= 1f)
                return;
            page.Line($"Parry: block armor {armor * bonus:0}", "Parry",
                $"A block raised less than {ParryWindow:0.00} s before the hit multiplies block armor by the parry bonus (x{SkillPage.Number(bonus)}) and can stagger the attacker.");
            ParryEffect(page, blocker);
        }

        /// <summary>
        /// The effect a parry gives, chosen as the game chooses it (the blocker's, else the player's), when it is a shield
        /// that absorbs more damage per Blocking level: how much it absorbs at the player's level and world level.
        /// </summary>
        private static void ParryEffect(SkillPage page, ItemDrop.ItemData blocker)
        {
            StatusEffect effect = blocker.m_shared.m_perfectBlockStatusEffect != null ? blocker.m_shared.m_perfectBlockStatusEffect : page.Player.m_perfectBlockStatusEffect;
            if (!(effect is SE_Shield shield) || shield.m_absorbDamagePerSkillLevel <= 0f)
                return;
            float absorb = shield.m_absorbDamage + shield.m_absorbDamagePerSkillLevel * page.Level;
            if (Game.m_worldLevel > 0)
                absorb += shield.m_absorbDamageWorldLevel * Game.m_worldLevel;
            string name = Localized(shield.m_name);
            if (name.Length == 0)
                name = "Parry shield";
            page.Line($"{name}: absorbs {absorb:0} damage", name,
                $"Given by a parry. It absorbs {shield.m_absorbDamagePerSkillLevel:0.##} more damage per Blocking level.");
        }

        private static string Localized(string token) =>
            Localization.instance != null ? Localization.instance.Localize(token) : token ?? "";
    }
}
