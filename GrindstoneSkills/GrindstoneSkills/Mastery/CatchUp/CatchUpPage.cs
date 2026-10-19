using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Every skill's page in the info pane: one line while the skill trains faster (<see cref="CatchUpXp"/>), Catch Up
    /// (section 6) while it is below the best level it reached, else Weapon Switch (section 43) while a weapon skill is
    /// below the player's best weapon skill. Nothing while neither applies or its switch is off. The page is the local
    /// player's own, written on their client from their own skills. The hovered term is a word of its own, filled into
    /// the line, so the line holds it in every language.
    /// </summary>
    public static class CatchUpPage
    {
        public static void Write(SkillPage page)
        {
            if (!LocalSkill.IsLocal(page.Player))
                return;
            switch (CatchUpXp.Find(page.Player.GetSkills(), page.Type, out float rate, out int until))
            {
                case CatchUpXp.Boost.CatchUp:
                    CatchUpLine(page, rate, until);
                    break;
                case CatchUpXp.Boost.WeaponSwitch:
                    WeaponSwitchLine(page, rate, until);
                    break;
            }
        }

        private static void CatchUpLine(SkillPage page, float rate, int until)
        {
            string term = Lang.Get("gs_mastery_catchup_term");
            string times = SkillPage.Number(rate);
            page.Line(Lang.Format("gs_mastery_catchup_line", term, times, until), term, Lang.Format("gs_mastery_catchup_tip", until, times));
        }

        private static void WeaponSwitchLine(SkillPage page, float rate, int until)
        {
            string term = Lang.Get("gs_mastery_switch_term");
            string bonus = SkillPage.Percent(rate - 1f);
            page.Line(Lang.Format("gs_mastery_switch_line", term, bonus), term, Lang.Format("gs_mastery_switch_tip", bonus, until));
        }
    }
}
