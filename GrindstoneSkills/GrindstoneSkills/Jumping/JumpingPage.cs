using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Jump's page in the info pane: the Jumping perks (section 37) with what they give at the player's level and at 100,
    /// from the same constants and formulas the perks use (<see cref="LightFeet"/>, <see cref="SoftLanding"/>,
    /// <see cref="LandingRoll"/>). The jump's stamina is the player's own m_jumpStaminaUsage times the world's movement
    /// stamina rate, before gear, as Player.OnJump spends it. Nothing is written while Jumping is off on the server.
    /// </summary>
    public static class JumpingPage
    {
        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.Jumping.Value)
                return;
            page.Perk(Lang.Get("gs_perk_lightfeet"), 0f, LightFeetTip(page));
            page.Perk(Lang.Get("gs_perk_softlanding"), 0f, SoftLandingTip(page.Factor));
            page.Perk(Lang.Get("gs_perk_roll"), LandingRoll.Level, RollTip(page.Factor));
        }

        private static string LightFeetTip(SkillPage page)
        {
            float cost = page.Player.m_jumpStaminaUsage * Game.m_moveStaminaRate;
            float share = LightFeet.CostShare(page.Factor);
            return Lang.Format("gs_jump_lightfeet_tip", SkillPage.Number(cost * share), SkillPage.Number(cost),
                SkillPage.Percent(1f - share), SkillPage.Percent(1f - LightFeet.CostShare(1f)));
        }

        private static string SoftLandingTip(float factor) => Lang.Format("gs_jump_softlanding_tip",
            SkillPage.Number(SoftLanding.SafeHeight(factor)), SkillPage.Number(SoftLanding.SafeHeight(0f)), SkillPage.Number(SoftLanding.SafeHeight(1f)),
            SkillPage.Percent(1f - SoftLanding.DamageShare(factor)), SkillPage.Percent(1f - SoftLanding.DamageShare(1f)),
            SkillPage.Number(SoftLanding.MaxDamage(factor)), SkillPage.Number(SoftLanding.FullDamageHeight(factor)));

        private static string RollTip(float factor) => Lang.Format("gs_jump_roll_tip",
            SkillPage.Number(LandingRoll.FreeHeight(factor)), SkillPage.Number(LandingRoll.FreeHeight(1f)), SkillPage.Percent(LandingRoll.Share));
    }
}
