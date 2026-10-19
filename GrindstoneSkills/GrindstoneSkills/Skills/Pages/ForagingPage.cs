using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Foraging's page in the info pane: the extra yield of a pick at the player's level, the experience per pick, and
    /// the discovery and sweep perks. The numbers are the synced settings read as the features read them: the game's
    /// extra-item roll (skill factor times the chance <see cref="ForagePick"/> sets), <see cref="ForageXp"/>'s scale and
    /// <see cref="ForageSweep.Radius"/>.
    /// </summary>
    public static class ForagingPage
    {
        /// <summary>The highest biome step, the Ashlands' and the Deep North's (<see cref="MineXp.BiomeStep"/>).</summary>
        private const int LastBiomeStep = 6;

        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_foraging_about");
            if (!ForagingSkill.Active)
            {
                page.Line(Lang.Get("gs_page_foraging_off"));
                return;
            }
            Yield(page);
            Experience(page);
            Perks(page);
        }

        private static void Yield(SkillPage page)
        {
            float atHundred = Mathf.Clamp01(ForagePerkSettings.ExtraYieldAt100.Value / 100f);
            string term = Lang.Get("gs_page_foraging_yield");
            page.Line(Lang.Format("gs_page_foraging_yield_line", term, SkillPage.Percent(page.Factor * atHundred)), term,
                Lang.Format("gs_page_foraging_yield_tip", SkillPage.Percent(atHundred)));
        }

        private static void Experience(SkillPage page)
        {
            float perPick = Mathf.Max(0f, ForagingSettings.ExperiencePerPick.Value);
            if (perPick <= 0f)
                return;
            float step = Mathf.Max(0f, ForagingSettings.BiomeStep.Value) / 100f;
            float most = perPick * (1f + step * LastBiomeStep);
            string term = Lang.Get("gs_page_foraging_xp");
            string line = most > perPick
                ? Lang.Format("gs_page_foraging_xp_range_line", term, SkillPage.Number(perPick), SkillPage.Number(most))
                : Lang.Format("gs_page_foraging_xp_line", term, SkillPage.Number(perPick));
            page.Line(line, term,
                Lang.Format("gs_page_foraging_xp_tip", SkillPage.Number(perPick), SkillPage.Percent(step), SkillPage.Number(most)));
        }

        private static void Perks(SkillPage page)
        {
            float discovery = ForagingSettings.DiscoveryMultiplier.Value;
            if (discovery > 1f)
                page.Perk(Lang.Get("gs_perk_discovery"), 0f, Lang.Format("gs_page_foraging_discovery_tip", SkillPage.Number(discovery)));
            float reach = Mathf.Max(0f, ForagePerkSettings.SweepRadiusAt100.Value);
            float unlock = reach > 0f ? ForagePerkSettings.SweepLevel.Value : CustomSkill.MaxLevel + 1f;
            page.Perk(Lang.Get("gs_perk_sweep"), unlock, SweepTip(page.Level, unlock, reach));
        }

        private static string SweepTip(float level, float unlock, float reachAt100)
        {
            if (level >= unlock)
                return Lang.Format("gs_page_foraging_sweep_tip", SkillPage.Number(ForageSweep.Radius(level)), SkillPage.Number(reachAt100));
            return Lang.Format("gs_page_foraging_sweep_locked_tip", SkillPage.Number(ForageSweep.Radius(unlock)), unlock.ToString("0"),
                SkillPage.Number(reachAt100));
        }
    }
}
