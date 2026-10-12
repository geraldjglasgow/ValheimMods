namespace GrindstoneSkills
{
    /// <summary>
    /// Run's page in the info pane: the Running perks (section 36) with what they give at the player's level and at 100,
    /// from the same constants and formulas the perks use (<see cref="PackMule"/>, <see cref="Burden"/>,
    /// <see cref="SecondWind"/>). Nothing is written while Running is off on the server.
    /// </summary>
    public static class RunningPage
    {
        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.Running.Value)
                return;
            page.Perk("Pack Mule", 0f,
                $"You can carry {SkillPage.Number(PackMule.Bonus(page.Factor))} more at your level, {SkillPage.Number(PackMule.Bonus(1f))} at level 100.");
            page.Perk("Burden", Burden.Level,
                $"While over-encumbered, moving drains {SkillPage.Number(Burden.Drain(page.Player))} stamina/s instead of {SkillPage.Number(page.Player.m_encumberedStaminaDrain)}.");
            page.Perk("Second Wind", SecondWind.Level, SecondWindTip());
        }

        private static string SecondWindTip()
        {
            string tip = $"Running out of stamina while you run gives {SkillPage.Number(SecondWind.Stamina)} stamina back at once, and you keep running. Once every {SkillPage.Duration(SecondWind.Rest)}.";
            float wait = SecondWind.ReadyIn;
            return wait > 0f ? $"{tip} Ready again in {SkillPage.Duration(wait)}." : tip;
        }
    }
}
