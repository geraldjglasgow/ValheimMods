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
            page.About = "Better wild picks. Trained by picking berries, mushrooms and herbs.";
            if (!ForagingSkill.Active)
            {
                page.Line("Turned off on this server.");
                return;
            }
            Yield(page);
            Experience(page);
            Perks(page);
        }

        private static void Yield(SkillPage page)
        {
            float atHundred = Mathf.Clamp01(ForagePerkSettings.ExtraYieldAt100.Value / 100f);
            page.Line($"Extra yield {SkillPage.Percent(page.Factor * atHundred)}", "Extra yield",
                $"Chance that a pick gives one more. {SkillPage.Percent(atHundred)} at level 100.");
        }

        private static void Experience(SkillPage page)
        {
            float perPick = Mathf.Max(0f, ForagingSettings.ExperiencePerPick.Value);
            if (perPick <= 0f)
                return;
            float step = Mathf.Max(0f, ForagingSettings.BiomeStep.Value) / 100f;
            float most = perPick * (1f + step * LastBiomeStep);
            string range = most > perPick ? $"{SkillPage.Number(perPick)} to {SkillPage.Number(most)}" : SkillPage.Number(perPick);
            page.Line($"Experience {range} per pick", "Experience",
                $"{SkillPage.Number(perPick)} in the Meadows, +{SkillPage.Percent(step)} for each biome beyond, up to {SkillPage.Number(most)} in the Ashlands. Crops, even wild ones, train Farming.");
        }

        private static void Perks(SkillPage page)
        {
            float discovery = ForagingSettings.DiscoveryMultiplier.Value;
            if (discovery > 1f)
                page.Perk("Discovery", 0f,
                    $"The first pick of each kind of forage earns {SkillPage.Number(discovery)}x the experience, once per character.");
            float reach = Mathf.Max(0f, ForagePerkSettings.SweepRadiusAt100.Value);
            float unlock = reach > 0f ? ForagePerkSettings.SweepLevel.Value : CustomSkill.MaxLevel + 1f;
            page.Perk("Sweep", unlock, SweepTip(page.Level, unlock, reach));
        }

        private static string SweepTip(float level, float unlock, float reachAt100)
        {
            const string What = "Picking a plant also picks every plant of the same kind within";
            if (level >= unlock)
                return $"{What} {SkillPage.Number(ForageSweep.Radius(level))} m of it. {SkillPage.Number(reachAt100)} m at level 100.";
            return $"{What} {SkillPage.Number(ForageSweep.Radius(unlock))} m of it at level {unlock:0}, {SkillPage.Number(reachAt100)} m at level 100.";
        }
    }
}
