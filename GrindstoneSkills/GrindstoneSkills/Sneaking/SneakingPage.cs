using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sneak's page in the info pane: what the Sneaking perks (section 38) add at the player's level, after the game's
    /// own Sneak lines (<see cref="MovementPages.Sneak"/>), with the perks' own numbers. Written on the local client as
    /// the page opens; nothing while the module is off.
    /// </summary>
    public static class SneakingPage
    {
        public static void Write(SkillPage page)
        {
            if (!SneakSkill.On)
                return;
            float factor = page.Factor;
            page.Perk(Lang.Get("gs_perk_prowl"), 0f, ProwlTip(page.Player, factor));
            page.Perk(Lang.Get("gs_perk_softsteps"), 0f, SoftStepsTip(factor));
            page.Perk(Lang.Get("gs_perk_assassin"), 0f, AssassinTip(factor));
            page.Perk(Lang.Get("gs_perk_vanish"), Vanish.UnlockLevel, VanishTip());
        }

        private static string ProwlTip(Player player, float factor)
        {
            float speed = player.m_crouchSpeed;
            float bonus = Prowl.Bonus(factor);
            return Lang.Format("gs_sneak_prowl_tip", SkillPage.Number(speed * (1f + bonus)), SkillPage.Percent(bonus), SkillPage.Number(speed),
                SkillPage.Percent(Prowl.Bonus(1f)));
        }

        private static string SoftStepsTip(float factor) =>
            Lang.Format("gs_sneak_softsteps_tip", SkillPage.Percent(SoftSteps.Cut(factor)), SkillPage.Percent(SoftSteps.Cut(1f)));

        private static string AssassinTip(float factor) =>
            Lang.Format("gs_sneak_assassin_tip", SkillPage.Percent(SneakAttack.Bonus(factor)), SkillPage.Percent(SneakAttack.Bonus(1f)),
                SkillPage.Number(SneakAttack.ExperiencePerAttack), SkillPage.Duration(SneakAttack.ResetSeconds));

        private static string VanishTip() =>
            Lang.Format("gs_sneak_vanish_tip", SkillPage.Duration(Vanish.QuietSeconds), SkillPage.Number(Vanish.ClearRange),
                SkillPage.Number(Vanish.ReachRange), SkillPage.Duration(Vanish.CooldownSeconds));
    }
}
