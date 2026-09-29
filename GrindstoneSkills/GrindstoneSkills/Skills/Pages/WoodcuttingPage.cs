using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Woodcutting's page in the info pane: the game's own damage on wood at the player's level (Skills.GetRandomSkillRange),
    /// then what the Woodcutting module adds to swings, logs and fells, each left out while its setting turns it off. The
    /// numbers are the synced settings scaled by the level exactly as the features scale them (<see cref="WoodSkill.Share"/>,
    /// <see cref="Finds.Chance"/>, <see cref="OldGrowth.SizeShare"/>, <see cref="WoodXp.TierFactor"/>).
    /// </summary>
    public static class WoodcuttingPage
    {
        public static void Write(SkillPage page)
        {
            page.About = "Fell trees faster and get more wood from them. Trained by chopping wood.";
            GameDamage(page);
            if (!WoodSkill.Active)
            {
                page.Line("GrindstoneSkills extras are off on this server.");
                return;
            }
            Chopping(page);
            page.Heading("Felling");
            Felling(page);
        }

        private static void GameDamage(SkillPage page)
        {
            page.Player.GetSkills().GetRandomSkillRange(out float min, out float max, page.Type);
            page.Line($"Chop damage {SkillPage.Range(min, max)}", "Chop damage",
                "The game's own: each axe hit on wood deals this share of the axe's damage, rolled per hit (85–100% at level 100). Swing stamina follows the Axes skill.");
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
            ChanceLine(page, "Clean fell", FellPerkSettings.CleanFellAt100.Value,
                "Chance a tree you fell takes its stump with it; the stump drops its wood.");
            ChanceLine(page, "Replanting", FellPerkSettings.ReplantingAt100.Value,
                "Chance a free sapling of the same kind takes root where a tree you felled stood, if it can grow there. Not every tree has a sapling.");
            FindsLine(page);
        }

        private static void SwingLine(SkillPage page)
        {
            float refund = SwingPerkSettings.StaminaRefund.Value;
            float wear = SwingPerkSettings.WearReduction.Value;
            if (refund <= 0f && wear <= 0f)
                return;
            page.Line($"Stamina refund {Share(refund, page.Level)}, axe wear -{Share(wear, page.Level)}", "Stamina refund",
                $"A swing that hits wood gives back that share of its stamina and wears the axe that much less. {Share(refund, WoodSkill.MaxLevel)} and -{Share(wear, WoodSkill.MaxLevel)} at level 100.");
        }

        private static void CleanSplitLine(SkillPage page)
        {
            float chance = CleanSplitSettings.ChanceAt100.Value;
            if (chance <= 0f)
                return;
            string bonus = SkillPage.Percent(CleanSplitSettings.WoodBonus.Value / 100f);
            page.Line($"Clean split {Share(chance, page.Level)}, +{bonus} wood", "Clean split",
                $"Chance an axe hit on a log splits it at once, if the axe can cut that wood; that log gives {bonus} more wood. {Share(chance, WoodSkill.MaxLevel)} at level 100.");
        }

        private static void OldGrowthLine(SkillPage page)
        {
            float bonus = OldGrowthSettings.Bonus.Value;
            if (bonus <= 0f)
                return;
            float start = OldGrowthSettings.Start.Value;
            float full = Mathf.Max(start, OldGrowthSettings.Full.Value);
            page.Line($"Old growth up to +{BiggestTree(bonus, page.Level)} wood", "Old growth",
                $"Logs of big trees give more wood, measured against their own kind: nothing up to {SkillPage.Number(start)}% of its size range, all of it from {SkillPage.Number(full)}%. Follows the level of whoever breaks the log; +{BiggestTree(bonus, WoodSkill.MaxLevel)} at level 100.");
        }

        /// <summary>Old growth's bonus for the largest tree of a kind at this level, as the feature adds it to a log's wood.</summary>
        private static string BiggestTree(float bonus, float level) => SkillPage.Percent(WoodSkill.Share(bonus, level) * OldGrowth.SizeShare(1f));

        private static void ExperienceLine(SkillPage page)
        {
            float fell = Mathf.Max(0f, WoodExperienceSettings.FellExperience.Value) * WoodXp.Multiplier;
            float split = Mathf.Max(0f, WoodExperienceSettings.SplitExperience.Value) * WoodXp.Multiplier;
            if (fell <= 0f && split <= 0f)
                return;
            page.Line($"Experience +{SkillPage.Number(fell)} per tree, +{SkillPage.Number(split)} per log", "Experience", ExperienceTip());
        }

        private static string ExperienceTip()
        {
            string tip = $"For each tree you fell and each log you break into wood, plus {SkillPage.Number(WoodXp.Multiplier)} per swing that hits wood.";
            if (WoodExperienceSettings.TierScaling.Value)
                tip += $" Birch and oak teach ×{SkillPage.Number(WoodXp.TierFactor(2))}, Yggdrasil ×{SkillPage.Number(WoodXp.TierFactor(4))}.";
            float small = WoodExperienceSettings.SmallWoodHealth.Value;
            if (small > 0f)
                tip += $" Saplings and small trees (under {SkillPage.Number(small)} health) teach {SkillPage.Percent(Mathf.Clamp01(WoodExperienceSettings.SmallWoodExperience.Value / 100f))}.";
            float discovery = Mathf.Max(1f, WoodExperienceSettings.DiscoveryMultiplier.Value);
            if (discovery > 1f)
                tip += $" Your first tree of each kind counts ×{SkillPage.Number(discovery)}.";
            return tip;
        }

        private static void TimberLine(SkillPage page)
        {
            float push = Mathf.Min(TimberSettings.FallPushAt100.Value, TimberSettings.MaxFallPush);
            float safety = Mathf.Min(TimberSettings.LogSafetyAt100.Value, TimberSettings.MaxLogSafety);
            if (push <= 0f && safety <= 0f)
                return;
            float level = page.Level;
            page.Line($"Timber! push +{Share(push, level)}, log safety {Share(safety, level)}", "Timber!",
                $"A tree you fell tips away from you, pushed ×{Times(push, level)} as hard as in the game, and its logs do you that much less damage (others take the game's). ×{Times(push, WoodSkill.MaxLevel)} and {Share(safety, WoodSkill.MaxLevel)} at level 100.");
        }

        private static void DominoLine(SkillPage page)
        {
            float impact = DominoSettings.ImpactAt100.Value;
            if (impact <= 0f)
                return;
            page.Line($"Domino impact +{Share(impact, page.Level)}", "Domino",
                $"Logs of trees you fell hit other trees, logs and stumps ×{Times(impact, page.Level)} as hard, so one tree can knock over the next, up to {DominoSettings.MaxChain.Value} down a chain. ×{Times(impact, WoodSkill.MaxLevel)} at level 100.");
        }

        private static void FindsLine(SkillPage page)
        {
            if (FindSettings.ChanceAt0.Value <= 0f && FindSettings.ChanceAt100.Value <= 0f)
                return;
            page.Line($"Finds {SkillPage.Percent(Finds.Chance(WoodSkill.Factor(page.Level)))} per tree", "Finds",
                $"Chance a tree you fell hides something, such as a bird's nest or a wild hive. {SkillPage.Percent(Finds.Chance(1f))} at level 100.");
        }

        /// <summary>A chance that grows from nothing at level 0 to its setting at 100; no line while the setting is 0.</summary>
        private static void ChanceLine(SkillPage page, string term, float percentAt100, string tip)
        {
            if (percentAt100 > 0f)
                page.Line($"{term} {Share(percentAt100, page.Level)}", term, $"{tip} {Share(percentAt100, WoodSkill.MaxLevel)} at level 100.");
        }

        private static string Share(float percentAt100, float level) => SkillPage.Percent(WoodSkill.Share(percentAt100, level));

        /// <summary>A multiplier of one plus the level's share: 1 + 200% × 0.5 is "2".</summary>
        private static string Times(float percentAt100, float level) => SkillPage.Number(1f + WoodSkill.Share(percentAt100, level));
    }
}
