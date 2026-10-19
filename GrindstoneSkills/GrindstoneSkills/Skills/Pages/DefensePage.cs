using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Defense's page in the info pane: every bonus at the player's level, the guard perks, and the four milestones with
    /// the level each needs. The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="DefenseSkill.Share"/>, <see cref="Vitality.BonusHealth"/>).
    /// </summary>
    public static class DefensePage
    {
        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_defense_about");
            if (!DefenseSkill.Active)
            {
                page.Line(Lang.Get("gs_page_defense_off"));
                return;
            }
            Core(page);
            Guard(page);
            Milestones(page);
        }

        private static void Core(SkillPage page)
        {
            float level = page.Level;
            page.Line(Lang.Format("gs_page_defense_health", SkillPage.Number(Vitality.BonusHealth(level))));
            page.Line(Lang.Format("gs_page_defense_food", Share(DefenseSettings.FoodHealth.Value, level)));
            page.Line(Lang.Format("gs_page_defense_damage", Share(DefenseSettings.DamageReduction.Value, level)));
            Regeneration(page, level);
            Termed(page, "gs_page_defense_poise", "gs_page_defense_poise_line", Share(DefenseSettings.Poise.Value, level),
                Lang.Get("gs_page_defense_poise_tip"));
            Termed(page, "gs_page_defense_parry", "gs_page_defense_parry_line",
                (DefenseSettings.ParryWindow.Value * DefenseSkill.Factor(level)).ToString("0.00"), Lang.Get("gs_page_defense_parry_tip"));
            page.Line(Lang.Format("gs_page_defense_stamina", Share(DefenseSettings.BlockStamina.Value, level),
                Share(DefenseSettings.DodgeStamina.Value, level)));
        }

        /// <summary>"Out of combat: heal 5% of max health every 10 s", the delay before it on hover.</summary>
        private static void Regeneration(SkillPage page, float level)
        {
            string term = Lang.Get("gs_page_defense_outofcombat");
            string line = Lang.Format("gs_page_defense_outofcombat_line", term, Share(DefenseSettings.Regeneration.Value, level),
                SkillPage.Duration(DefenseSettings.RegenerationInterval.Value));
            page.Line(line, term, Lang.Format("gs_page_defense_outofcombat_tip", SkillPage.Duration(DefenseSettings.OutOfCombatDelay.Value)));
        }

        private static void Guard(SkillPage page)
        {
            float level = page.Level;
            page.Heading(Lang.Get("gs_page_defense_guard"));
            Termed(page, "gs_page_defense_reflex", "gs_page_defense_reflex_line", Share(DefenseGuardSettings.ReflexChance.Value, level),
                Lang.Get("gs_page_defense_reflex_tip"));
            Termed(page, "gs_page_defense_bash", "gs_page_defense_bash_line", Share(DefenseGuardSettings.BashChance.Value, level),
                Lang.Get("gs_page_defense_bash_tip"));
            Termed(page, "gs_page_defense_thorns", "gs_page_defense_thorns_line", Share(DefenseGuardSettings.Thorns.Value, level),
                Lang.Get("gs_page_defense_thorns_tip"));
            Termed(page, "gs_page_defense_adrenaline", "gs_page_defense_adrenaline_line", Share(DefenseGuardSettings.Adrenaline.Value, level),
                Lang.Get("gs_page_defense_adrenaline_tip"));
            page.Line(Lang.Format("gs_page_defense_wear", Share(DefenseGuardSettings.ShieldWear.Value, level),
                Share(DefenseGuardSettings.Knockback.Value, level)));
            if (DefenseGuardSettings.DesperationHealth.Value > 0f)
                page.Line(Lang.Format("gs_page_defense_desperation", DefenseGuardSettings.DesperationHealth.Value.ToString("0"),
                    SkillPage.Number(DefenseGuardSettings.DesperationMultiplier.Value)));
        }

        private static void Milestones(SkillPage page)
        {
            page.Perk(Lang.Get("gs_perk_riposte"), DefenseMilestoneSettings.RiposteLevel.Value,
                Lang.Format("gs_page_defense_riposte_tip", SkillPage.Duration(DefenseMilestoneSettings.RiposteWindow.Value),
                    DefenseMilestoneSettings.RiposteDamage.Value.ToString("0")));
            page.Perk(Lang.Get("gs_perk_shieldwall"), DefenseMilestoneSettings.ShieldWallLevel.Value,
                Lang.Format("gs_page_defense_shieldwall_tip", SkillPage.Number(DefenseMilestoneSettings.ShieldWallRadius.Value),
                    DefenseMilestoneSettings.ShieldWallReduction.Value.ToString("0")));
            page.Perk(Lang.Get("gs_perk_hardened"), DefenseMilestoneSettings.HardenedLevel.Value, HardenedTip());
            page.Perk(Lang.Get("gs_perk_laststand"), DefenseMilestoneSettings.LastStandLevel.Value,
                Lang.Format("gs_page_defense_laststand_tip", SkillPage.Duration(DefenseMilestoneSettings.LastStandInvulnerability.Value),
                    SkillPage.Duration(DefenseMilestoneSettings.LastStandCooldown.Value)));
        }

        private static string HardenedTip()
        {
            float perStack = DefenseMilestoneSettings.HardenedPerStack.Value;
            int stacks = DefenseMilestoneSettings.HardenedMaxStacks.Value;
            return Lang.Format("gs_page_defense_hardened_tip", SkillPage.Number(perStack), stacks, SkillPage.Number(perStack * stacks),
                SkillPage.Duration(DefenseMilestoneSettings.HardenedDuration.Value));
        }

        /// <summary>A line with a hover word: the word's key, the line's key (the word as $1, the value as $2) and the tip.</summary>
        private static void Termed(SkillPage page, string termKey, string lineKey, string value, string tip)
        {
            string term = Lang.Get(termKey);
            page.Line(Lang.Format(lineKey, term, value), term, tip);
        }

        private static string Share(float percentAt100, float level) => SkillPage.Percent(DefenseSkill.Share(percentAt100, level));
    }
}
