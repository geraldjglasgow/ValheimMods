using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's melee skills in the info pane: the damage a hit rolls and the attack stamina at the
    /// player's level (<see cref="WeaponLines"/>), plus what one skill does differently: axes on trees, bare fists.
    /// </summary>
    public static class MeleePages
    {
        public static void Swords(SkillPage page) => WeaponLines.Melee(page, "gs_page_swords_about");

        public static void Knives(SkillPage page) => WeaponLines.Melee(page, "gs_page_knives_about");

        public static void Clubs(SkillPage page) => WeaponLines.Melee(page, "gs_page_clubs_about");

        public static void Polearms(SkillPage page) => WeaponLines.Melee(page, "gs_page_polearms_about");

        public static void Spears(SkillPage page) => WeaponLines.Melee(page, "gs_page_spears_about");

        public static void Axes(SkillPage page)
        {
            WeaponLines.Melee(page, "gs_page_axes_about");
            string term = Lang.Get("gs_page_axes_woodcutting");
            page.Line(Lang.Format("gs_page_axes_trees_line", term), term, Lang.Get("gs_page_axes_trees_tip"));
        }

        public static void Unarmed(SkillPage page)
        {
            WeaponLines.Melee(page, "gs_page_unarmed_about");
            Fists(page);
        }

        /// <summary>"Bare fists hit for 2–4": the player's fists' damage times the range a hit rolls in.</summary>
        private static void Fists(SkillPage page)
        {
            ItemDrop fists = page.Player.m_unarmedWeapon;
            if (fists == null)
                return;
            float damage = fists.m_itemData.GetDamage().GetTotalDamage();
            if (damage <= 0f)
                return;
            WeaponLines.Roll(page, out float low, out float high);
            string term = Lang.Get("gs_page_unarmed_fists");
            page.Line(Lang.Format("gs_page_unarmed_fists_line", term, (damage * low).ToString("0"), (damage * high).ToString("0")), term,
                Lang.Get("gs_page_unarmed_fists_tip"));
        }
    }
}
