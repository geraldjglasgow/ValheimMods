using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Farming's page in the info pane: the game's own planting stamina and scythe reach, what the player's level does for
    /// the crops they pick and plant, the giant crop chance, and the perks with the level each needs. The numbers are the
    /// synced settings scaled by the level exactly as the features scale them (<see cref="FarmSkill.Share"/>,
    /// <see cref="CropPick"/>'s bonus chance, <see cref="PlantTraits.Radius"/>'s cap). The words are the
    /// <c>gs_page_farming_*</c> and perk name <c>gs_perk_*</c> keys (translations/English.yml); a line's hovered
    /// word has its own key and goes into the line's sentence as <c>$1</c>.
    /// </summary>
    public static class FarmingPage
    {
        /// <summary>The build stamina the game saves at level 100 (Player.GetBuildStamina).</summary>
        private const float GameStaminaSaving = 0.5f;

        /// <summary>The most a crop's grow space shrinks, as <see cref="PlantTraits"/> caps it.</summary>
        private const float MaxSpaceReduction = 0.8f;

        public static void Write(SkillPage page)
        {
            Game(page);
            if (!FarmSkill.Active)
            {
                string bonus = Lang.Get("gs_page_farming_bonus");
                page.Line(Lang.Format("gs_page_farming_bonus_game_line", bonus), bonus, Lang.Get("gs_page_farming_bonus_game_tip"));
                page.Line(Lang.Get("gs_page_farming_off"));
                return;
            }
            page.About = Lang.Get("gs_page_farming_about");
            Picking(page);
            Growing(page);
            Ripening(page);
            Care(page);
            Milestones(page);
        }

        /// <summary>What the game itself does with the level: cultivator stamina, and the harvest radius of a scythe.</summary>
        private static void Game(SkillPage page)
        {
            string stamina = Lang.Get("gs_page_farming_stamina");
            page.Line(Lang.Format("gs_page_farming_stamina_line", stamina, SkillPage.Percent(GameStaminaSaving * page.Factor)), stamina,
                Lang.Get("gs_page_farming_stamina_tip"));
            Attack harvest = HarvestAttack(page.Player, out string tool);
            if (harvest == null)
            {
                string scythe = Lang.Get("gs_page_farming_scythe");
                page.Line(Lang.Format("gs_page_farming_scythe_line", scythe), scythe, Lang.Get("gs_page_farming_scythe_tip"));
                return;
            }
            float radius = Mathf.Lerp(harvest.m_harvestRadius, harvest.m_harvestRadiusMaxLevel, page.Factor);
            page.Line(Lang.Format("gs_page_farming_reach_line", tool, SkillPage.Number(radius)), tool, Lang.Get("gs_page_farming_reach_tip"));
        }

        private static void Picking(SkillPage page)
        {
            float bonus = page.Factor * Mathf.Clamp01(FarmingPerkSettings.BonusYieldAt100.Value / 100f);
            string crop = Lang.Get("gs_page_farming_bonus");
            page.Line(Lang.Format("gs_page_farming_bonus_line", crop, SkillPage.Percent(bonus)), crop, Lang.Get("gs_page_farming_bonus_tip"));
            if (FarmingPerkSettings.SeedReturnAt100.Value <= 0f)
                return;
            string seed = Lang.Get("gs_page_farming_seed");
            page.Line(Lang.Format("gs_page_farming_seed_line", seed, Share(FarmingPerkSettings.SeedReturnAt100.Value, page.Level)), seed,
                Lang.Get("gs_page_farming_seed_tip"));
        }

        private static void Growing(SkillPage page)
        {
            float level = page.Level;
            if (FarmingPerkSettings.GrowthSpeedAt100.Value > 0f)
            {
                string growth = Lang.Get("gs_page_farming_growth");
                page.Line(Lang.Format("gs_page_farming_growth_line", growth, Share(FarmingPerkSettings.GrowthSpeedAt100.Value, level)), growth, GrowthTip());
            }
            if (FarmingPerkSettings.GrowSpaceAt100.Value <= 0f)
                return;
            float space = Mathf.Min(MaxSpaceReduction, FarmSkill.Share(FarmingPerkSettings.GrowSpaceAt100.Value, level));
            string spacing = Lang.Get("gs_page_farming_spacing");
            page.Line(Lang.Format("gs_page_farming_spacing_line", spacing, SkillPage.Percent(space)), spacing, Lang.Get("gs_page_farming_spacing_tip"));
        }

        private static void Ripening(SkillPage page)
        {
            float giantAt100 = FarmingSettings.GiantChanceAt100.Value;
            if (giantAt100 <= 0f)
                return;
            page.Heading(Lang.Get("gs_page_farming_ripening"));
            string giant = Lang.Get("gs_page_farming_giant");
            page.Line(Lang.Format("gs_page_farming_giant_line", giant, Share(giantAt100, page.Level)), giant, GiantTip());
        }

        /// <summary>The perks everyone has from level 0: tending and the compost bin.</summary>
        private static void Care(SkillPage page)
        {
            if (Tending.Enabled)
                page.Perk(Lang.Get("gs_perk_tending"), 0f, TendTip());
            if (CompostBin.Working)
                page.Perk(Lang.Get("gs_perk_compostbin"), 0f, CompostTip());
        }

        private static void Milestones(SkillPage page)
        {
            page.Perk(Lang.Get("gs_perk_rowofthree"), FarmingPerkSettings.RowOfThreeLevel.Value, RowTip(3));
            page.Perk(Lang.Get("gs_perk_rowoffive"), FarmingPerkSettings.RowOfFiveLevel.Value, RowTip(5));
            page.Perk(Lang.Get("gs_perk_autoreplant"), FarmingPerkSettings.AutoReplantLevel.Value,
                Lang.Get("gs_page_farming_replant_tip") + OwnSwitch(FarmingSettings.AutoReplant.Value, "Auto Replant"));
            page.Perk(Lang.Get("gs_perk_heattolerance"), FarmingPerkSettings.HeatToleranceLevel.Value, Lang.Get("gs_page_farming_heat_tip"));
            page.Perk(Lang.Get("gs_perk_coldtolerance"), FarmingPerkSettings.ColdToleranceLevel.Value, Lang.Get("gs_page_farming_cold_tip"));
        }

        /// <summary>Growth's tip, with rain's and the compost's bonus when they add one: a whole sentence per case.</summary>
        private static string GrowthTip()
        {
            bool rain = FarmingPerkSettings.RainBonus.Value > 0f;
            bool compost = CompostBin.Working && CompostSettings.GrowthSpeed.Value > 0f;
            string rainBonus = SkillPage.Number(FarmingPerkSettings.RainBonus.Value);
            string compostBonus = SkillPage.Number(CompostSettings.GrowthSpeed.Value);
            if (rain && compost)
                return Lang.Format("gs_page_farming_growth_tip_both", rainBonus, compostBonus);
            if (rain)
                return Lang.Format("gs_page_farming_growth_tip_rain", rainBonus);
            return compost ? Lang.Format("gs_page_farming_growth_tip_compost", compostBonus) : Lang.Get("gs_page_farming_growth_tip");
        }

        private static string GiantTip() =>
            Lang.Format("gs_page_farming_giant_tip", SkillPage.Number(FarmingSettings.GiantSize.Value), FarmingSettings.GiantYield.Value,
                SkillPage.Number(Mathf.Max(1f, FarmingExperienceSettings.GiantMultiplier.Value)));

        private static string TendTip()
        {
            float radius = FarmingPerkSettings.TendingRadius.Value;
            string key = Key("$KEY_Use", "E");
            string bonus = SkillPage.Number(FarmingPerkSettings.TendingBonus.Value);
            return radius > 0f
                ? Lang.Format("gs_page_farming_tending_tip_area", key, SkillPage.Number(radius), bonus)
                : Lang.Format("gs_page_farming_tending_tip", key, bonus);
        }

        private static string CompostTip() =>
            Lang.Format("gs_page_farming_compost_tip", SkillPage.Duration(CompostSettings.CompostTime.Value),
                SkillPage.Number(CompostSettings.Radius.Value), SkillPage.Number(CompostSettings.GrowthSpeed.Value));

        private static string RowTip(int width) =>
            Lang.Format("gs_page_farming_row_tip", width, Key("$KEY_AltPlace", "Shift"))
            + OwnSwitch(FarmingSettings.RowPlanting.Value, "Row Planting");

        /// <summary>
        /// A note for a perk each player can switch off, when this player has: its own sentence after the tip. The
        /// setting's name stays as the .cfg writes it.
        /// </summary>
        private static string OwnSwitch(bool on, string setting) => on ? "" : " " + Lang.Format("gs_page_farming_own_off", setting);

        /// <summary>The key bound to one of the game's buttons, as the game names it in hints.</summary>
        private static string Key(string token, string fallback) =>
            Localization.instance != null ? Localization.instance.Localize(token) : fallback;

        /// <summary>
        /// The harvesting attack (Attack.m_harvest) of a tool the player carries, a scythe, with its name; null without one.
        /// Its radius at level 0 and 100 are the item's own values.
        /// </summary>
        private static Attack HarvestAttack(Player player, out string name)
        {
            name = "";
            if (player == null)
                return null;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                Attack attack = Harvests(item.m_shared.m_attack) ? item.m_shared.m_attack
                    : Harvests(item.m_shared.m_secondaryAttack) ? item.m_shared.m_secondaryAttack : null;
                if (attack == null)
                    continue;
                name = Localization.instance != null ? Localization.instance.Localize(item.m_shared.m_name) : item.m_shared.m_name;
                return attack;
            }
            return null;
        }

        private static bool Harvests(Attack attack) => attack != null && attack.m_harvest;

        private static string Share(float percentAt100, float level) => SkillPage.Percent(FarmSkill.Share(percentAt100, level));
    }
}
