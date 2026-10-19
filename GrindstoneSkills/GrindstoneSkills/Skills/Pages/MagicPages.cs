using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's two magic skills in the info pane: the damage a spell rolls and the cut in its costs at the
    /// player's level (<see cref="WeaponLines"/>), then what the player's known staves summon and shield at that level
    /// (<see cref="StaffLines"/>).
    /// </summary>
    public static class MagicPages
    {
        public static void ElementalMagic(SkillPage page)
        {
            page.About = Lang.Get("gs_page_magic_elemental_about");
            WeaponLines.Damage(page, "gs_page_magic_damage_line");
            WeaponLines.Cost(page, "gs_page_magic_eitr", "gs_page_magic_eitr_tip");
            StaffLines.Write(page);
        }

        public static void BloodMagic(SkillPage page)
        {
            page.About = Lang.Get("gs_page_magic_blood_about");
            WeaponLines.Cost(page, "gs_page_magic_bloodcost", "gs_page_magic_bloodcost_tip");
            if (StaffLines.Write(page))
                return;
            string term = Lang.Get("gs_page_magic_grow");
            page.Line(Lang.Format("gs_page_magic_grow_line", term), term, Lang.Get("gs_page_magic_grow_tip"));
        }
    }
}
