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
            page.About = "Makes you tougher. Trained by blocking and by taking hits.";
            if (!DefenseSkill.Active)
            {
                page.Line("Turned off on this server.");
                return;
            }
            Core(page);
            Guard(page);
            Milestones(page);
        }

        private static void Core(SkillPage page)
        {
            float level = page.Level;
            page.Line($"Max health +{SkillPage.Number(Vitality.BonusHealth(level))}");
            page.Line($"Health from food +{Share(DefenseSettings.FoodHealth.Value, level)}");
            page.Line($"Damage taken -{Share(DefenseSettings.DamageReduction.Value, level)}");
            page.Line($"Out of combat: heal {Share(DefenseSettings.Regeneration.Value, level)} of max health every {SkillPage.Duration(DefenseSettings.RegenerationInterval.Value)}",
                "Out of combat", $"After {SkillPage.Duration(DefenseSettings.OutOfCombatDelay.Value)} without attacking, blocking or taking damage.");
            page.Line($"Poise +{Share(DefenseSettings.Poise.Value, level)}", "Poise",
                "It takes that much more stagger damage to stagger you, or to break your guard while you block.");
            page.Line($"Parry window +{DefenseSettings.ParryWindow.Value * DefenseSkill.Factor(level):0.00} s", "Parry window",
                "Added to the game's 0.25 s, so a block raised a little early still parries.");
            page.Line($"Block stamina -{Share(DefenseSettings.BlockStamina.Value, level)}, dodge stamina -{Share(DefenseSettings.DodgeStamina.Value, level)}");
        }

        private static void Guard(SkillPage page)
        {
            float level = page.Level;
            page.Heading("Guard");
            page.Line($"Reflex {Share(DefenseGuardSettings.ReflexChance.Value, level)}", "Reflex",
                "Chance that a hit from the front is blocked by your shield although you were not blocking. Costs stamina like a block, never parries.");
            page.Line($"Shield bash {Share(DefenseGuardSettings.BashChance.Value, level)}", "Shield bash",
                "Chance that an ordinary shield block staggers the attacker, as a parry does.");
            page.Line($"Thorns {Share(DefenseGuardSettings.Thorns.Value, level)}", "Thorns",
                "Share of the damage your block stopped that goes back to a melee attacker.");
            page.Line($"Adrenaline +{Share(DefenseGuardSettings.Adrenaline.Value, level)}", "Adrenaline",
                "More adrenaline from blocks and parries, and that much less lost to hits you did not block.");
            page.Line($"Block wear -{Share(DefenseGuardSettings.ShieldWear.Value, level)}, knockback on blocks -{Share(DefenseGuardSettings.Knockback.Value, level)}");
            if (DefenseGuardSettings.DesperationHealth.Value > 0f)
                page.Line($"Desperation: below {DefenseGuardSettings.DesperationHealth.Value:0}% health, damage reduction x{SkillPage.Number(DefenseGuardSettings.DesperationMultiplier.Value)}");
        }

        private static void Milestones(SkillPage page)
        {
            page.Perk("Riposte", DefenseMilestoneSettings.RiposteLevel.Value,
                $"For {SkillPage.Duration(DefenseMilestoneSettings.RiposteWindow.Value)} after a parry, your next melee attack deals {DefenseMilestoneSettings.RiposteDamage.Value:0}% more damage and staggers what it hits (not bosses).");
            page.Perk("Shield Wall", DefenseMilestoneSettings.ShieldWallLevel.Value,
                $"While you block with a shield, players within {SkillPage.Number(DefenseMilestoneSettings.ShieldWallRadius.Value)} m behind you take {DefenseMilestoneSettings.ShieldWallReduction.Value:0}% less damage. Several blockers do not add up.");
            page.Perk("Hardened", DefenseMilestoneSettings.HardenedLevel.Value, HardenedTip());
            page.Perk("Last Stand", DefenseMilestoneSettings.LastStandLevel.Value,
                $"A blow that would kill you leaves you at 1 health, and you take no damage for {SkillPage.Duration(DefenseMilestoneSettings.LastStandInvulnerability.Value)}. Once every {SkillPage.Duration(DefenseMilestoneSettings.LastStandCooldown.Value)}.");
        }

        private static string HardenedTip()
        {
            float perStack = DefenseMilestoneSettings.HardenedPerStack.Value;
            int stacks = DefenseMilestoneSettings.HardenedMaxStacks.Value;
            return $"Each hit that hurts you adds a stack: {SkillPage.Number(perStack)}% less damage per stack, up to {stacks} stacks ({SkillPage.Number(perStack * stacks)}%). They last {SkillPage.Duration(DefenseMilestoneSettings.HardenedDuration.Value)} after the last hit.";
        }

        private static string Share(float percentAt100, float level) => SkillPage.Percent(DefenseSkill.Share(percentAt100, level));
    }
}
