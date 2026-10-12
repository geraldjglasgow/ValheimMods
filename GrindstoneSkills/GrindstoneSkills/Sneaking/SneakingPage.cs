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
            page.Perk("Prowl", 0f, ProwlTip(page.Player, factor));
            page.Perk("Soft Steps", 0f, SoftStepsTip(factor));
            page.Perk("Assassin", 0f, AssassinTip(factor));
            page.Perk("Vanish", Vanish.UnlockLevel, VanishTip());
        }

        private static string ProwlTip(Player player, float factor)
        {
            float speed = player.m_crouchSpeed;
            float bonus = Prowl.Bonus(factor);
            return $"Crouched walking {SkillPage.Number(speed * (1f + bonus))} m/s at your level (+{SkillPage.Percent(bonus)}), {SkillPage.Number(speed)} untrained, before gear. +{SkillPage.Percent(Prowl.Bonus(1f))} at level 100. Not while over-encumbered.";
        }

        private static string SoftStepsTip(float factor) =>
            $"Attacking, dodging, mining and building from a crouch make {SkillPage.Percent(SoftSteps.Cut(factor))} less noise at your level, {SkillPage.Percent(SoftSteps.Cut(1f))} less at level 100. Creatures hear you only within your noise; crouched walking makes none.";

        private static string AssassinTip(float factor) =>
            $"A sneak attack, on a creature that has not noticed you, deals {SkillPage.Percent(SneakAttack.Bonus(factor))} more at your level, {SkillPage.Percent(SneakAttack.Bonus(1f))} more at level 100, on top of the weapon's own bonus, and gives {SkillPage.Number(SneakAttack.ExperiencePerAttack)} Sneak experience. One per creature every {SkillPage.Duration(SneakAttack.ResetSeconds)}, as in the game.";

        private static string VanishTip() =>
            $"Crouch for {SkillPage.Duration(Vanish.QuietSeconds)} with no enemy within {SkillPage.Number(Vanish.ClearRange)} m while creatures hunt you, and every one within {SkillPage.Number(Vanish.ReachRange)} m loses track of you. Not bosses. Once every {SkillPage.Duration(Vanish.CooldownSeconds)}.";
    }
}
