using Translations;

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
            page.About = Lang.Get("gs_page_blocking_about");
            string term = Lang.Get("gs_page_blocking_armor");
            page.Line(Lang.Format("gs_page_blocking_armor_line", term, SkillPage.Percent(ArmorAt100 * page.Factor)), term,
                Lang.Format("gs_page_blocking_armor_tip", SkillPage.Percent(ArmorAt100)));
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
            page.Line(Lang.Format("gs_page_blocking_held_line", name, armor.ToString("0")), name,
                Lang.Format("gs_page_blocking_held_tip", untrained.ToString("0"), (untrained * (1f + ArmorAt100)).ToString("0")));
            float bonus = blocker.m_shared.m_timedBlockBonus;
            if (bonus <= 1f)
                return;
            string parry = Lang.Get("gs_page_blocking_parry");
            page.Line(Lang.Format("gs_page_blocking_parry_line", parry, (armor * bonus).ToString("0")), parry,
                Lang.Format("gs_page_blocking_parry_tip", ParryWindow.ToString("0.00"), SkillPage.Number(bonus)));
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
                name = Lang.Get("gs_page_blocking_shield");
            page.Line(Lang.Format("gs_page_blocking_shield_line", name, absorb.ToString("0")), name,
                Lang.Format("gs_page_blocking_shield_tip", shield.m_absorbDamagePerSkillLevel.ToString("0.##")));
        }

        private static string Localized(string token) =>
            Localization.instance != null ? Localization.instance.Localize(token) : token ?? "";
    }
}
