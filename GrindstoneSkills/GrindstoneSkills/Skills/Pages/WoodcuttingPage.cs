using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Woodcutting's page in the info pane: the game's own damage on wood at the player's level (Skills.GetRandomSkillRange),
    /// then what the Woodcutting module adds to swings, logs and fells, each left out while its setting turns it off. The
    /// numbers are the synced settings scaled by the level exactly as the features scale them (<see cref="WoodSkill.Share"/>,
    /// <see cref="Finds.Chance"/>, <see cref="OldGrowth.SizeShare"/>, <see cref="WoodXp.TierFactor"/>). The words are the
    /// <c>gs_page_woodcutting_*</c> keys (translations/English.yml); a line's hovered word has its own key and goes
    /// into the line's sentence as <c>$1</c>.
    /// </summary>
    public static class WoodcuttingPage
    {
        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_woodcutting_about");
            GameDamage(page);
            if (!WoodSkill.Active)
            {
                page.Line(Lang.Get("gs_page_woodcutting_off"));
                return;
            }
            Chopping(page);
            page.Heading(Lang.Get("gs_page_woodcutting_felling"));
            Felling(page);
        }

        private static void GameDamage(SkillPage page)
        {
            page.Player.GetSkills().GetRandomSkillRange(out float min, out float max, page.Type);
            string damage = Lang.Get("gs_page_woodcutting_damage");
            page.Line(Lang.Format("gs_page_woodcutting_damage_line", damage, SkillPage.Range(min, max)), damage,
                Lang.Get("gs_page_woodcutting_damage_tip"));
        }

        private static void Chopping(SkillPage page)
        {
            SwingLine(page);
            CleanSplitLine(page);
            OldGrowthLine(page);
            ExperienceLine(page);
        }

        private static void Felling(SkillPage page)
        {
            TimberLine(page);
            DominoLine(page);
            ChanceLine(page, FellPerkSettings.CleanFellAt100.Value,
                "gs_page_woodcutting_cleanfell", "gs_page_woodcutting_cleanfell_line", "gs_page_woodcutting_cleanfell_tip");
            ChanceLine(page, FellPerkSettings.ReplantingAt100.Value,
                "gs_page_woodcutting_replanting", "gs_page_woodcutting_replanting_line", "gs_page_woodcutting_replanting_tip");
            FindsLine(page);
        }

        private static void SwingLine(SkillPage page)
        {
            float refund = SwingPerkSettings.StaminaRefund.Value;
            float wear = SwingPerkSettings.WearReduction.Value;
            if (refund <= 0f && wear <= 0f)
                return;
            string term = Lang.Get("gs_page_woodcutting_refund");
            page.Line(Lang.Format("gs_page_woodcutting_refund_line", term, Share(refund, page.Level), Share(wear, page.Level)), term,
                Lang.Format("gs_page_woodcutting_refund_tip", Share(refund, WoodSkill.MaxLevel), Share(wear, WoodSkill.MaxLevel)));
        }

        private static void CleanSplitLine(SkillPage page)
        {
            float chance = CleanSplitSettings.ChanceAt100.Value;
            if (chance <= 0f)
                return;
            string bonus = SkillPage.Percent(CleanSplitSettings.WoodBonus.Value / 100f);
            string term = Lang.Get("gs_page_woodcutting_split");
            page.Line(Lang.Format("gs_page_woodcutting_split_line", term, Share(chance, page.Level), bonus), term,
                Lang.Format("gs_page_woodcutting_split_tip", bonus, Share(chance, WoodSkill.MaxLevel)));
        }

        private static void OldGrowthLine(SkillPage page)
        {
            float bonus = OldGrowthSettings.Bonus.Value;
            if (bonus <= 0f)
                return;
            float start = OldGrowthSettings.Start.Value;
            float full = Mathf.Max(start, OldGrowthSettings.Full.Value);
            string term = Lang.Get("gs_page_woodcutting_oldgrowth");
            page.Line(Lang.Format("gs_page_woodcutting_oldgrowth_line", term, BiggestTree(bonus, page.Level)), term,
                Lang.Format("gs_page_woodcutting_oldgrowth_tip", SkillPage.Number(start), SkillPage.Number(full), BiggestTree(bonus, WoodSkill.MaxLevel)));
        }

        /// <summary>Old growth's bonus for the largest tree of a kind at this level, as the feature adds it to a log's wood.</summary>
        private static string BiggestTree(float bonus, float level) => SkillPage.Percent(WoodSkill.Share(bonus, level) * OldGrowth.SizeShare(1f));

        private static void ExperienceLine(SkillPage page)
        {
            float fell = Mathf.Max(0f, WoodExperienceSettings.FellExperience.Value) * WoodXp.Multiplier;
            float split = Mathf.Max(0f, WoodExperienceSettings.SplitExperience.Value) * WoodXp.Multiplier;
            if (fell <= 0f && split <= 0f)
                return;
            string term = Lang.Get("gs_page_woodcutting_xp");
            page.Line(Lang.Format("gs_page_woodcutting_xp_line", term, SkillPage.Number(fell), SkillPage.Number(split)), term, ExperienceTip());
        }

        /// <summary>Experience's tip: one sentence, then one more for each setting that changes what a tree teaches.</summary>
        private static string ExperienceTip()
        {
            string tip = Lang.Format("gs_page_woodcutting_xp_tip", SkillPage.Number(WoodXp.Multiplier));
            if (WoodExperienceSettings.TierScaling.Value)
                tip += " " + Lang.Format("gs_page_woodcutting_xp_tip_tiers", SkillPage.Number(WoodXp.TierFactor(2)), SkillPage.Number(WoodXp.TierFactor(4)));
            float small = WoodExperienceSettings.SmallWoodHealth.Value;
            if (small > 0f)
                tip += " " + Lang.Format("gs_page_woodcutting_xp_tip_small", SkillPage.Number(small),
                    SkillPage.Percent(Mathf.Clamp01(WoodExperienceSettings.SmallWoodExperience.Value / 100f)));
            float discovery = Mathf.Max(1f, WoodExperienceSettings.DiscoveryMultiplier.Value);
            if (discovery > 1f)
                tip += " " + Lang.Format("gs_page_woodcutting_xp_tip_discovery", SkillPage.Number(discovery));
            return tip;
        }

        private static void TimberLine(SkillPage page)
        {
            float push = Mathf.Min(TimberSettings.FallPushAt100.Value, TimberSettings.MaxFallPush);
            float safety = Mathf.Min(TimberSettings.LogSafetyAt100.Value, TimberSettings.MaxLogSafety);
            if (push <= 0f && safety <= 0f)
                return;
            float level = page.Level;
            string term = Lang.Get("gs_page_woodcutting_timber");
            page.Line(Lang.Format("gs_page_woodcutting_timber_line", term, Share(push, level), Share(safety, level)), term,
                Lang.Format("gs_page_woodcutting_timber_tip", Times(push, level), Times(push, WoodSkill.MaxLevel), Share(safety, WoodSkill.MaxLevel)));
        }

        private static void DominoLine(SkillPage page)
        {
            float impact = DominoSettings.ImpactAt100.Value;
            if (impact <= 0f)
                return;
            string term = Lang.Get("gs_page_woodcutting_domino");
            page.Line(Lang.Format("gs_page_woodcutting_domino_line", term, Share(impact, page.Level)), term,
                Lang.Format("gs_page_woodcutting_domino_tip", Times(impact, page.Level), DominoSettings.MaxChain.Value, Times(impact, WoodSkill.MaxLevel)));
        }

        private static void FindsLine(SkillPage page)
        {
            if (FindSettings.ChanceAt0.Value <= 0f && FindSettings.ChanceAt100.Value <= 0f)
                return;
            string term = Lang.Get("gs_page_woodcutting_finds");
            page.Line(Lang.Format("gs_page_woodcutting_finds_line", term, SkillPage.Percent(Finds.Chance(WoodSkill.Factor(page.Level)))), term,
                Lang.Format("gs_page_woodcutting_finds_tip", SkillPage.Percent(Finds.Chance(1f))));
        }

        /// <summary>
        /// A chance that grows from nothing at level 0 to its setting at 100; no line while the setting is 0. The line's
        /// <c>$1</c> is the term, <c>$2</c> the chance; the tip's <c>$1</c> the chance at level 100.
        /// </summary>
        private static void ChanceLine(SkillPage page, float percentAt100, string termKey, string lineKey, string tipKey)
        {
            if (percentAt100 <= 0f)
                return;
            string term = Lang.Get(termKey);
            page.Line(Lang.Format(lineKey, term, Share(percentAt100, page.Level)), term, Lang.Format(tipKey, Share(percentAt100, WoodSkill.MaxLevel)));
        }

        private static string Share(float percentAt100, float level) => SkillPage.Percent(WoodSkill.Share(percentAt100, level));

        /// <summary>A multiplier of one plus the level's share: 1 + 200% × 0.5 is "2".</summary>
        private static string Times(float percentAt100, float level) => SkillPage.Number(1f + WoodSkill.Share(percentAt100, level));
    }
}
