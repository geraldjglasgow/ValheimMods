using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Pickaxes' page in the info pane: the game's own damage and swing stamina at the player's level
    /// (Skills.GetRandomSkillRange, Attack.GetAttackStamina), then what the Pickaxes module adds (seams and clean strikes,
    /// splash, extra ore, wear, finds, experience) and its three milestones, each left out while its setting turns it off.
    /// The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="PickSkill.Share"/>, <see cref="PickSkill.Between"/>, <see cref="CleanStrikes.Multiplier"/>,
    /// <see cref="Splash.Amount"/>, <see cref="MineFinds.Chance"/>, <see cref="MineXp.BiomeStep"/>). The words are the
    /// <c>gs_page_pickaxes_*</c> and perk name <c>gs_perk_*</c> keys (translations/English.yml); a line's hovered
    /// word has its own key and goes into the line's sentence as <c>$1</c>.
    /// </summary>
    public static class PickaxesPage
    {
        /// <summary>The game's cut in attack stamina at level 100: Attack.GetAttackStamina takes 33% times the skill factor off.</summary>
        private const float StaminaCut = 0.33f;

        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_pickaxes_about");
            GameLines(page);
            if (!PickSkill.Active)
            {
                page.Line(Lang.Get("gs_page_pickaxes_off"));
                return;
            }
            SeamLines(page);
            SplashLine(page);
            PerkLines(page);
            FindsLine(page);
            ExperienceLine(page);
            Milestones(page);
        }

        /// <summary>Seams open at all: "0 with the next setting at 0 turns seams off".</summary>
        private static bool SeamsOn => SeamSettings.ChanceAt0.Value > 0f || SeamSettings.ChanceAt100.Value > 0f;

        private static void GameLines(SkillPage page)
        {
            page.Player.GetSkills().GetRandomSkillRange(out float min, out float max, page.Type);
            string damage = Lang.Get("gs_page_pickaxes_damage");
            page.Line(Lang.Format("gs_page_pickaxes_damage_line", damage, SkillPage.Range(min, max)), damage, Lang.Get("gs_page_pickaxes_damage_tip"));
            string stamina = Lang.Get("gs_page_pickaxes_stamina");
            page.Line(Lang.Format("gs_page_pickaxes_stamina_line", stamina, SkillPage.Percent(StaminaCut * page.Factor)), stamina,
                Lang.Format("gs_page_pickaxes_stamina_tip", SkillPage.Percent(StaminaCut)));
        }

        private static void SeamLines(SkillPage page)
        {
            if (!SeamsOn)
                return;
            float level = page.Level;
            float chance = PickSkill.Between(SeamSettings.ChanceAt0.Value, SeamSettings.ChanceAt100.Value, level) / 100f;
            float window = Mathf.Max(0.1f, PickSkill.Between(SeamSettings.WindowAt0.Value, SeamSettings.WindowAt100.Value, level));
            string seams = Lang.Get("gs_page_pickaxes_seams");
            page.Line(Lang.Format("gs_page_pickaxes_seams_line", seams, SkillPage.Percent(chance), SkillPage.Duration(window)), seams,
                Lang.Get("gs_page_pickaxes_seams_tip"));
            string clean = Lang.Get("gs_page_pickaxes_clean");
            page.Line(Lang.Format("gs_page_pickaxes_clean_line", clean, SkillPage.Number(CleanStrikes.Multiplier(1, level))), clean, CleanStrikeTip());
        }

        private static string CleanStrikeTip()
        {
            float experience = Mathf.Max(0f, PickaxeExperienceSettings.CleanStrike.Value) * MineXp.Multiplier;
            return experience > 0f
                ? Lang.Format("gs_page_pickaxes_clean_tip_xp", SkillPage.Number(experience))
                : Lang.Get("gs_page_pickaxes_clean_tip");
        }

        private static void SplashLine(SkillPage page)
        {
            float at100 = Mathf.Max(0f, PickaxePerkSettings.SplashDamage.Value);
            if (at100 <= 0f)
                return;
            float amount = Splash.Amount(new Miner(0L, page.Level, ZDOID.None));
            string splash = Lang.Get("gs_page_pickaxes_splash");
            page.Line(Lang.Format("gs_page_pickaxes_splash_line", splash, SkillPage.Number(amount)), splash,
                Lang.Format("gs_page_pickaxes_splash_tip", SkillPage.Number(at100)));
        }

        private static void PerkLines(SkillPage page)
        {
            float level = page.Level;
            float ore = PickaxePerkSettings.ExtraOreChance.Value;
            if (ore > 0f)
            {
                string extra = Lang.Get("gs_page_pickaxes_ore");
                page.Line(Lang.Format("gs_page_pickaxes_ore_line", extra, Share(ore, level)), extra,
                    Lang.Format("gs_page_pickaxes_ore_tip", Share(ore, PickSkill.MaxLevel)));
            }
            float wear = PickaxePerkSettings.WearReduction.Value;
            if (wear <= 0f)
                return;
            string pickWear = Lang.Get("gs_page_pickaxes_wear");
            page.Line(Lang.Format("gs_page_pickaxes_wear_line", pickWear, Share(wear, level)), pickWear,
                Lang.Format("gs_page_pickaxes_wear_tip", Share(wear, PickSkill.MaxLevel)));
        }

        private static void FindsLine(SkillPage page)
        {
            if (MineFindSettings.ChanceAt0.Value <= 0f && MineFindSettings.ChanceAt100.Value <= 0f)
                return;
            string finds = Lang.Get("gs_page_pickaxes_finds");
            page.Line(Lang.Format("gs_page_pickaxes_finds_line", finds, SkillPage.Percent(MineFinds.Chance(page.Level))), finds,
                Lang.Format("gs_page_pickaxes_finds_tip", SkillPage.Percent(MineFinds.Chance(PickSkill.MaxLevel))));
        }

        private static void ExperienceLine(SkillPage page)
        {
            float step = Mathf.Max(0f, PickaxeExperienceSettings.BiomeStep.Value);
            float ore = 1f + Mathf.Max(0f, PickaxeExperienceSettings.OreBonus.Value) / 100f;
            string term = Lang.Get("gs_page_pickaxes_xp");
            page.Line(Lang.Format("gs_page_pickaxes_xp_line", term, SkillPage.Number(step), SkillPage.Number(ore)), term, ExperienceTip(step));
        }

        /// <summary>Experience's tip: one sentence, then one more for each setting that adds to what rock teaches.</summary>
        private static string ExperienceTip(float step)
        {
            string forest = SkillPage.Number(1f + step / 100f * MineXp.BiomeStep(Heightmap.Biome.BlackForest));
            string ashlands = SkillPage.Number(1f + step / 100f * MineXp.BiomeStep(Heightmap.Biome.AshLands));
            string tip = Lang.Format("gs_page_pickaxes_xp_tip", forest, ashlands);
            float discovery = Mathf.Max(0f, PickaxeExperienceSettings.Discovery.Value);
            if (discovery > 0f)
                tip += " " + Lang.Format("gs_page_pickaxes_xp_tip_discovery", SkillPage.Number(discovery));
            if (MineXp.Multiplier != 1f)
                tip += " " + Lang.Format("gs_page_pickaxes_xp_tip_server", SkillPage.Number(MineXp.Multiplier));
            return tip;
        }

        private static void Milestones(SkillPage page)
        {
            page.Perk(Lang.Get("gs_perk_readtherock"), VeinSettings.ReadTheRockLevel.Value, ReadTheRockTip());
            page.Perk(Lang.Get("gs_perk_echo"), VeinSettings.EchoLevel.Value,
                Lang.Format("gs_page_pickaxes_echo_tip", SkillPage.Number(VeinSettings.EchoRadius.Value), SkillPage.Duration(VeinSettings.EchoCooldown.Value)));
            if (SeamsOn)
                page.Perk(Lang.Get("gs_perk_unbroken"), SeamSettings.UnbrokenLevel.Value, UnbrokenTip());
        }

        private static string ReadTheRockTip() =>
            Lang.Format("gs_page_pickaxes_readtherock_tip", Setting(VeinSettings.Chance1.Value), Setting(VeinSettings.Chance2.Value),
                Setting(VeinSettings.Chance3.Value), Setting(VeinSettings.BonusPerStar.Value));

        private static string UnbrokenTip()
        {
            float level = SeamSettings.UnbrokenLevel.Value;
            float first = CleanStrikes.Multiplier(1, level);
            float second = CleanStrikes.Multiplier(2, level);
            float top = CleanStrikes.Multiplier(Mathf.Max(0, SeamSettings.UnbrokenMaxLinks.Value) + 1, level);
            return Lang.Format("gs_page_pickaxes_unbroken_tip", SkillPage.Number(second), SkillPage.Number(second - first), SkillPage.Number(top));
        }

        /// <summary>A perk's share at this level as a percent, held to 0..1 as the perks hold it.</summary>
        private static string Share(float percentAt100, float level) => SkillPage.Percent(Mathf.Clamp01(PickSkill.Share(percentAt100, level)));

        /// <summary>A percent setting as it reads: 25 is "25%".</summary>
        private static string Setting(float percent) => SkillPage.Percent(Mathf.Max(0f, percent) / 100f);
    }
}
