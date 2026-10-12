namespace GrindstoneSkills
{
    /// <summary>
    /// Every skill's page in the info pane: one line while the skill trains faster (<see cref="CatchUpXp"/>), Catch Up
    /// (section 6) while it is below the best level it reached, else Weapon Switch (section 43) while a weapon skill is
    /// below the player's best weapon skill. Nothing while neither applies or its switch is off. The page is the local
    /// player's own, written on their client from their own skills.
    /// </summary>
    public static class CatchUpPage
    {
        private const string CatchUpTerm = "Catching up";
        private const string WeaponSwitchTerm = "Weapon Switch";

        public static void Write(SkillPage page)
        {
            if (!LocalSkill.IsLocal(page.Player))
                return;
            switch (CatchUpXp.Find(page.Player.GetSkills(), page.Type, out float rate, out int until))
            {
                case CatchUpXp.Boost.CatchUp:
                    page.Line($"{CatchUpTerm}: x{SkillPage.Number(rate)} until level {until}", CatchUpTerm, CatchUpTip(rate, until));
                    break;
                case CatchUpXp.Boost.WeaponSwitch:
                    page.Line($"{WeaponSwitchTerm}: +{SkillPage.Percent(rate - 1f)} experience", WeaponSwitchTerm, WeaponSwitchTip(rate, until));
                    break;
            }
        }

        private static string CatchUpTip(float rate, int until) =>
            $"Catch Up: this skill is below level {until}, the best it ever reached, so it gains x{SkillPage.Number(rate)} experience until it is back there. " +
            "Every skill's best level is kept with the character.";

        private static string WeaponSwitchTip(float rate, int until) =>
            $"A weapon skill below your best weapon skill gains {SkillPage.Percent(rate - 1f)} more experience, until it reaches your best weapon skill, level {until}.";
    }
}
