using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Farming's page in the info pane: the game's own planting stamina and scythe reach, what the player's level does for
    /// the crops they pick and plant, the star roll a crop they plant ripens with, and the perks with the level each
    /// needs. The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="FarmSkill.Share"/>, <see cref="CropPick"/>'s bonus chance, <see cref="PlantTraits.Radius"/>'s cap,
    /// <see cref="StarOdds.At"/>).
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
                page.Line("Bonus crop chance grows with level", "Bonus crop", "The game's own: a crop you pick sometimes gives a bonus.");
                page.Line("GrindstoneSkills' Farming is off on this server.");
                return;
            }
            page.About = "Grow more and better crops. Trained by planting and picking them.";
            Picking(page);
            Growing(page);
            Ripening(page);
            Care(page);
            Milestones(page);
        }

        /// <summary>What the game itself does with the level: cultivator stamina, and the harvest radius of a scythe.</summary>
        private static void Game(SkillPage page)
        {
            page.Line($"Planting stamina -{SkillPage.Percent(GameStaminaSaving * page.Factor)}", "Planting stamina",
                "The game's own: each use of the cultivator costs less stamina, down to half at level 100.");
            Attack harvest = HarvestAttack(page.Player, out string tool);
            if (harvest == null)
            {
                page.Line("Scythe reach grows with level", "Scythe", "The game's own: one swing of a scythe picks every ripe crop in a radius that grows with your level.");
                return;
            }
            float radius = Mathf.Lerp(harvest.m_harvestRadius, harvest.m_harvestRadiusMaxLevel, page.Factor);
            page.Line($"{tool} reach {SkillPage.Number(radius)} m", tool, "The game's own: one swing picks every ripe crop within this radius.");
        }

        private static void Picking(SkillPage page)
        {
            float bonus = page.Factor * Mathf.Clamp01(FarmingPerkSettings.BonusYieldAt100.Value / 100f);
            page.Line($"Bonus crop {SkillPage.Percent(bonus)}", "Bonus crop", "Chance each crop you pick gives its bonus yield on top.");
            if (FarmingPerkSettings.SeedReturnAt100.Value > 0f)
                page.Line($"Seed return {Share(FarmingPerkSettings.SeedReturnAt100.Value, page.Level)}", "Seed return",
                    "Chance a pick also gives back the seed the plant grew from, with the crop's stars.");
        }

        private static void Growing(SkillPage page)
        {
            float level = page.Level;
            if (FarmingPerkSettings.GrowthSpeedAt100.Value > 0f)
                page.Line($"Growth +{Share(FarmingPerkSettings.GrowthSpeedAt100.Value, level)}", "Growth", GrowthTip());
            if (FarmingPerkSettings.GrowSpaceAt100.Value <= 0f)
                return;
            float space = Mathf.Min(MaxSpaceReduction, FarmSkill.Share(FarmingPerkSettings.GrowSpaceAt100.Value, level));
            page.Line($"Crop spacing -{SkillPage.Percent(space)}", "Crop spacing", "Crops you plant need that much less room around them, so they can stand closer.");
        }

        private static void Ripening(SkillPage page)
        {
            bool stars = FarmingSettings.CropStars.Value;
            float giantAt100 = FarmingSettings.GiantChanceAt100.Value;
            if (!stars && giantAt100 <= 0f)
                return;
            page.Heading("Ripening");
            if (stars)
                StarLines(page);
            if (giantAt100 > 0f)
                page.Line($"Giant crop {Share(giantAt100, page.Level)}", "Giant crop", GiantTip(stars));
        }

        private static void StarLines(SkillPage page)
        {
            float[] odds = StarOdds.At(page.Level);
            page.Line($"Star odds {StarText.Colored(1)} {SkillPage.Percent(odds[1])}  {StarText.Colored(2)} {SkillPage.Percent(odds[2])}  {StarText.Colored(3)} {SkillPage.Percent(odds[3])}",
                "Star odds", "What a crop you plant rolls when it ripens, at your level; heirloom seeds, companions and compost add levels. Only kitchen crops carry stars.");
            float seed = FarmingSettings.SeedLevelsPerStar.Value;
            if (seed > 0f)
                page.Line($"Heirloom seeds +{SkillPage.Number(seed)} levels per star", "Heirloom seeds",
                    "Each star of the seed you plant counts as that many extra levels in the crop's star roll.");
            float companion = FarmingSettings.CompanionLevels.Value;
            int kinds = FarmingSettings.CompanionKinds.Value;
            if (companion > 0f && kinds > 0)
                page.Line($"Companions +{SkillPage.Number(companion)} levels per kind", "Companions",
                    $"Each other kind of crop growing or ripe within {SkillPage.Number(FarmingSettings.CompanionRadius.Value)} m when it ripens counts, up to {kinds} kinds. A seed and its crop are one kind.");
        }

        /// <summary>The perks everyone has from level 0: tending and the compost bin.</summary>
        private static void Care(SkillPage page)
        {
            if (Tending.Enabled)
                page.Perk("Tending", 0f, TendTip());
            if (CompostBin.Working)
                page.Perk("Compost Bin", 0f, CompostTip());
        }

        private static void Milestones(SkillPage page)
        {
            float off = FarmSkill.MaxLevel + 1f;
            page.Perk("Almanac", FarmingSettings.CropStars.Value ? FarmingPerkSettings.AlmanacLevel.Value : off,
                "A growing crop's hover also shows the odds of the stars it will ripen with, counting its seed, companions and compost.");
            page.Perk("Row of Three", FarmingPerkSettings.RowOfThreeLevel.Value, RowTip(3));
            page.Perk("Row of Five", FarmingPerkSettings.RowOfFiveLevel.Value, RowTip(5));
            page.Perk("Auto Replant", FarmingPerkSettings.AutoReplantLevel.Value,
                "Picking a crop puts its plant back in the same spot, paid with a seed from your inventory." + OwnSwitch(FarmingSettings.AutoReplant.Value, "Auto Replant"));
            page.Perk("Heat Tolerance", FarmingPerkSettings.HeatToleranceLevel.Value, "Crops you plant grow in the Ashlands without a shield.");
            page.Perk("Cold Tolerance", FarmingPerkSettings.ColdToleranceLevel.Value, "Crops you plant that grow in the Meadows also grow in the Mountain and Deep North.");
        }

        private static string GrowthTip()
        {
            List<string> more = new List<string>();
            if (FarmingPerkSettings.RainBonus.Value > 0f)
                more.Add($"rain adds {SkillPage.Number(FarmingPerkSettings.RainBonus.Value)}% while it falls");
            if (CompostBin.Working && CompostSettings.GrowthSpeed.Value > 0f)
                more.Add($"compost {SkillPage.Number(CompostSettings.GrowthSpeed.Value)}%");
            string tip = "Everything you plant grows this much faster, saplings too";
            return more.Count == 0 ? tip + "." : tip + "; " + string.Join(", ", more) + ".";
        }

        private static string GiantTip(bool stars)
        {
            string always = stars ? $", always {StarText.Colored(3)} for kitchen crops" : "";
            return $"A crop you plant may ripen giant: {SkillPage.Number(FarmingSettings.GiantSize.Value)}x the size and {FarmingSettings.GiantYield.Value}x the crop{always}. "
                + $"Picking one gives {SkillPage.Number(Mathf.Max(1f, FarmingExperienceSettings.GiantMultiplier.Value))}x the experience.";
        }

        private static string TendTip()
        {
            float radius = FarmingPerkSettings.TendingRadius.Value;
            string reach = radius > 0f ? $"it and every growing plant within {SkillPage.Number(radius)} m gain" : "it gains";
            return $"Press {Key("$KEY_Use", "E")} on a growing plant: {reach} {SkillPage.Number(FarmingPerkSettings.TendingBonus.Value)}% of the grow time, once per in-game day.";
        }

        private static string CompostTip()
        {
            string feed = $"+{SkillPage.Number(CompostSettings.GrowthSpeed.Value)}% growth";
            if (FarmingSettings.CropStars.Value && CompostSettings.StarLevels.Value > 0f)
                feed += $" and +{SkillPage.Number(CompostSettings.StarLevels.Value)} star levels";
            return $"A barrel in the cultivator's menu. Scraps and spare crops in it become a point of compost every {SkillPage.Duration(CompostSettings.CompostTime.Value)}, "
                + $"and each growing crop within {SkillPage.Number(CompostSettings.Radius.Value)} m takes one: {feed}.";
        }

        private static string RowTip(int width) =>
            $"Placing a seed plants a row of {width} across your view, each paying its own seed. Hold {Key("$KEY_AltPlace", "Shift")} to plant one."
            + OwnSwitch(FarmingSettings.RowPlanting.Value, "Row Planting");

        /// <summary>A note for a perk each player can switch off, when this player has.</summary>
        private static string OwnSwitch(bool on, string setting) => on ? "" : $" Off in your config ({setting}).";

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
