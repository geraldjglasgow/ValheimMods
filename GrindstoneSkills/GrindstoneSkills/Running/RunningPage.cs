using Translations;

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
            page.Perk(Lang.Get("gs_perk_packmule"), 0f,
                Lang.Format("gs_run_packmule_tip", SkillPage.Number(PackMule.Bonus(page.Factor)), SkillPage.Number(PackMule.Bonus(1f))));
            page.Perk(Lang.Get("gs_perk_burden"), Burden.Level,
                Lang.Format("gs_run_burden_tip", SkillPage.Number(Burden.Drain(page.Player)), SkillPage.Number(page.Player.m_encumberedStaminaDrain)));
            page.Perk(Lang.Get("gs_perk_secondwind"), SecondWind.Level, SecondWindTip());
        }

        private static string SecondWindTip()
        {
            string tip = Lang.Format("gs_run_secondwind_tip", SkillPage.Number(SecondWind.Stamina), SkillPage.Duration(SecondWind.Rest));
            float wait = SecondWind.ReadyIn;
            return wait > 0f ? tip + " " + Lang.Format("gs_run_secondwind_ready", SkillPage.Duration(wait)) : tip;
        }
    }
}
