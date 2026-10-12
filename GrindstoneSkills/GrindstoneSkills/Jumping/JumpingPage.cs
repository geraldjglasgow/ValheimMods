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
            page.Perk("Light Feet", 0f, LightFeetTip(page));
            page.Perk("Soft Landing", 0f, SoftLandingTip(page.Factor));
            page.Perk("Roll", LandingRoll.Level, RollTip(page.Factor));
        }

        private static string LightFeetTip(SkillPage page)
        {
            float cost = page.Player.m_jumpStaminaUsage * Game.m_moveStaminaRate;
            float share = LightFeet.CostShare(page.Factor);
            return $"A jump costs {SkillPage.Number(cost * share)} stamina at your level instead of {SkillPage.Number(cost)}, before gear: -{SkillPage.Percent(1f - share)}. "
                + $"-{SkillPage.Percent(1f - LightFeet.CostShare(1f))} at level 100.";
        }

        private static string SoftLandingTip(float factor)
        {
            string safe = $"Falls up to {SkillPage.Number(SoftLanding.SafeHeight(factor))} m do you no harm at your level ({SkillPage.Number(SoftLanding.SafeHeight(0f))} m untrained, {SkillPage.Number(SoftLanding.SafeHeight(1f))} m at level 100).";
            string damage = $" Longer falls hurt {SkillPage.Percent(1f - SoftLanding.DamageShare(factor))} less ({SkillPage.Percent(1f - SoftLanding.DamageShare(1f))} at level 100):"
                + $" at most {SkillPage.Number(SoftLanding.MaxDamage(factor))} damage, from {SkillPage.Number(SoftLanding.FullDamageHeight(factor))} m.";
            return safe + damage;
        }

        private static string RollTip(float factor) =>
            $"Land from a fall that would hurt while moving forward and you roll: no damage from falls up to {SkillPage.Number(LandingRoll.FreeHeight(factor))} m"
            + $" ({SkillPage.Number(LandingRoll.FreeHeight(1f))} m at level 100), {SkillPage.Percent(LandingRoll.Share)} of the damage above that."
            + " The roll costs no stamina and does not shield you from hits.";
    }
}
