using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Fishing's page in the info pane: the game's own reel speed and stamina (read from the fishing float prefab, whose
    /// values the game serializes rather than codes), then the bites, the fight and the catch at the player's level, and
    /// the angler's perks with the level each needs. The numbers are the synced settings scaled by the level exactly as
    /// the features scale them (<see cref="FishSkill.Share"/>, <see cref="Strike.Window"/>, <see cref="Tension.Change"/>,
    /// <see cref="Snags.Chance"/>, and <see cref="BonusItems"/>'s blend from each fish's own chance).
    /// </summary>
    public static class FishingPage
    {
        private static ZNetScene searched;
        private static FishingFloat floatPrefab;

        public static void Write(SkillPage page)
        {
            Game(page);
            if (!FishSkill.Active)
            {
                page.Line(Lang.Get("gs_page_fishing_off"));
                return;
            }
            page.About = Lang.Get("gs_page_fishing_about");
            Fight(page);
            Catch(page);
            Perks(page);
        }

        /// <summary>What the game itself does with the level: FishingFloat.FixedUpdate lerps these by the skill factor.</summary>
        private static void Game(SkillPage page)
        {
            FishingFloat prefab = FloatPrefab();
            if (prefab == null)
                return;
            float factor = page.Factor;
            float speed = Mathf.Lerp(prefab.m_pullLineSpeed, prefab.m_pullLineSpeedMaxSkill, factor);
            Line(page, "gs_page_fishing_reel_speed", "gs_page_fishing_reel_speed_line", Lang.Get("gs_page_fishing_reel_speed_tip"),
                speed.ToString("0.#"));
            float saved = 1f - Mathf.Lerp(1f, prefab.m_pullStaminaUseMaxSkillMultiplier, factor);
            float hold = Mathf.Lerp(prefab.m_hookedStaminaPerSec, prefab.m_hookedStaminaPerSecMaxSkill, factor);
            Line(page, "gs_page_fishing_reel_stamina", "gs_page_fishing_reel_stamina_line",
                Lang.Format("gs_page_fishing_reel_stamina_tip", hold.ToString("0.##")), SkillPage.Percent(saved));
        }

        private static void Fight(SkillPage page)
        {
            float level = page.Level;
            Line(page, "gs_page_fishing_bite", "gs_page_fishing_bite_line", BiteTip(), Share(FishingBiteSettings.BiteChanceAt100.Value, level));
            page.Heading(Lang.Get("gs_page_fishing_heading_fight"));
            Line(page, "gs_page_fishing_strike", "gs_page_fishing_strike_line", Lang.Get("gs_page_fishing_strike_tip"),
                Strike.Window(level).ToString("0.##"));
            if (FishingFightSettings.TensionEnabled.Value)
                Line(page, "gs_page_fishing_tension", "gs_page_fishing_tension_line", TensionTip(),
                    SkillPage.Percent(Tension.Change(true, true, level)));
        }

        private static void Catch(SkillPage page)
        {
            float level = page.Level;
            page.Heading(Lang.Get("gs_page_fishing_heading_catch"));
            if (FishingBigFishSettings.BigOneChanceAt100.Value > 0f)
                Line(page, "gs_page_fishing_big_one", "gs_page_fishing_big_one_line", BigOneTip(),
                    Share(FishingBigFishSettings.BigOneChanceAt100.Value, level));
            string bonus = BonusChance(level);
            if (bonus.Length > 0)
                Line(page, "gs_page_fishing_bonus", "gs_page_fishing_bonus_line", Lang.Get("gs_page_fishing_bonus_tip"), bonus);
            if (FishingCatchSettings.BaitSaverAt100.Value > 0f)
                Line(page, "gs_page_fishing_bait", "gs_page_fishing_bait_line", Lang.Get("gs_page_fishing_bait_tip"),
                    Share(FishingCatchSettings.BaitSaverAt100.Value, level));
            float snag = Snags.Chance(level);
            if (snag > 0f)
                Line(page, "gs_page_fishing_snag", "gs_page_fishing_snag_line", SnagTip(), SkillPage.Percent(snag));
            if (FishingCatchSettings.CastDistanceAt100.Value > 0f || FishingCatchSettings.LineLengthAt100.Value > 0f)
                Line(page, "gs_page_fishing_line", "gs_page_fishing_cast_line", LineTip(level),
                    Share(FishingCatchSettings.CastDistanceAt100.Value, level), Share(FishingCatchSettings.LineLengthAt100.Value, level));
        }

        private static void Perks(SkillPage page)
        {
            float perfect = FishingFightSettings.PerfectStrikeWindow.Value;
            if (perfect > 0f)
                page.Perk(Lang.Get("gs_perk_perfectstrike"), 0f, Lang.Format("gs_page_fishing_perfect_tip", perfect.ToString("0.##")));
            page.Perk(Lang.Get("gs_perk_anglerslog"), 0f, LogTip());
            page.Perk(Lang.Get("gs_perk_speciessense"), FishingBiteSettings.SpeciesSenseLevel.Value, Lang.Get("gs_page_fishing_species_tip"));
            page.Perk(Lang.Get("gs_perk_sizesense"), FishingBiteSettings.SizeSenseLevel.Value, Lang.Get("gs_page_fishing_size_tip"));
            page.Perk(Lang.Get("gs_perk_doublebonus"), FishingCatchSettings.DoubleBonusLevel.Value, Lang.Get("gs_page_fishing_double_tip"));
            float legendary = FishingBigFishSettings.LegendaryChance.Value > 0f ? FishingBigFishSettings.LegendaryLevel.Value : FishSkill.MaxLevel + 1f;
            page.Perk(Lang.Get("gs_perk_legendaryfish"), legendary, LegendaryTip());
            page.Perk(Lang.Get("gs_perk_grace"), FishingFightSettings.GraceLevel.Value,
                Lang.Format("gs_page_fishing_grace_tip", SkillPage.Duration(FishingFightSettings.GraceSeconds.Value)));
            page.Perk(Lang.Get("gs_perk_watersense"), FishingBiteSettings.WaterSenseLevel.Value, Lang.Get("gs_page_fishing_water_tip"));
        }

        private static string BiteTip()
        {
            string dawn = SkillPage.Number(FishingBiteSettings.DawnDuskBonus.Value);
            string rain = SkillPage.Number(FishingBiteSettings.RainBonus.Value);
            string chum = ChumNames();
            if (chum.Length == 0)
                return Lang.Format("gs_page_fishing_bite_tip", dawn, rain);
            return Lang.Format("gs_page_fishing_bite_tip_chum", dawn, rain, SkillPage.Number(FishingBiteSettings.ChumBonus.Value),
                SkillPage.Number(FishingBiteSettings.ChumRadius.Value), chum);
        }

        private static string TensionTip()
        {
            string ease = SkillPage.Percent(FishSkill.Percent(FishingFightSettings.TensionEase.Value));
            string tip = Lang.Format("gs_page_fishing_tension_tip", ease);
            int thrashes = FishingFightSettings.ThrashesToTire.Value;
            return thrashes > 0
                ? tip + " " + Lang.Format("gs_page_fishing_tension_tip_spent", thrashes, SkillPage.Number(FishingFightSettings.SpentReelSpeed.Value))
                : tip;
        }

        private static string BigOneTip() =>
            Lang.Format("gs_page_fishing_big_one_tip", FishInfo.MaxNaturalLevel, SkillPage.Number(FishingBigFishSettings.NightBonus.Value));

        private static string SnagTip() =>
            Lang.Format("gs_page_fishing_snag_tip", SkillPage.Duration(FishingCatchSettings.SnagWait.Value));

        private static string LineTip(float level)
        {
            FishingFloat prefab = FloatPrefab();
            if (prefab == null)
                return Lang.Get("gs_page_fishing_line_tip");
            float reach = prefab.m_maxDistance * (1f + FishSkill.Share(FishingCatchSettings.LineLengthAt100.Value, level));
            return Lang.Format("gs_page_fishing_line_tip_reach", SkillPage.Number(reach));
        }

        /// <summary>The Angler's Log tip; /fishlog is a chat command, so it stays as typed in every language.</summary>
        private static string LogTip()
        {
            string tip = Lang.Format("gs_page_fishing_log_tip", "/fishlog");
            return FishingExperienceSettings.Discovery.Value > 0f || FishingExperienceSettings.NewSize.Value > 0f
                ? tip + " " + Lang.Get("gs_page_fishing_log_tip_bonus")
                : tip;
        }

        private static string LegendaryTip()
        {
            int thrashes = FishingBigFishSettings.LegendaryThrashes.Value;
            string chance = SkillPage.Number(FishingBigFishSettings.LegendaryChance.Value);
            string first = thrashes > 0
                ? Lang.Format("gs_page_fishing_legendary_tip_tire", chance, FishInfo.LegendaryLevel, thrashes)
                : Lang.Format("gs_page_fishing_legendary_tip_never", chance, FishInfo.LegendaryLevel);
            return first + " " + Lang.Get("gs_page_fishing_legendary_tip_bait");
        }

        /// <summary>
        /// A landed fish's bonus-item chance at this level, as <see cref="BonusItems.Prepare"/> raises each species' own:
        /// from the fish's chance at 0 to Bonus Item Chance At 100, never below its own. A range when species differ; ""
        /// when no fish has a bonus table.
        /// </summary>
        private static string BonusChance(float level)
        {
            float target = FishSkill.Percent(FishingCatchSettings.BonusItemChanceAt100.Value);
            float low = 1f;
            float high = 0f;
            foreach (GameObject prefab in FishInfo.Species())
            {
                DropTable table = prefab.GetComponent<Fish>().m_extraDrops;
                if (table == null || table.IsEmpty())
                    continue;
                float chance = Mathf.Max(table.m_dropChance, Mathf.Lerp(table.m_dropChance, target, FishSkill.Factor(level)));
                low = Mathf.Min(low, chance);
                high = Mathf.Max(high, chance);
            }
            if (high < low)
                return "";
            return Mathf.Approximately(low, high)
                ? SkillPage.Percent(low)
                : Lang.Format("gs_page_fishing_bonus_range", SkillPage.Percent(low), SkillPage.Percent(high));
        }

        /// <summary>The Chum Items setting as item names players read ("Entrails, Blood bag"); "" when chum is off.</summary>
        private static string ChumNames()
        {
            List<string> names = new List<string>();
            foreach (string part in (FishingBiteSettings.ChumItems.Value ?? "").Split(','))
            {
                if (part.Trim().Length > 0)
                    names.Add(ItemName(part.Trim()));
            }
            return string.Join(", ", names);
        }

        private static string ItemName(string prefabName)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return item != null ? FishInfo.Localize(item.m_itemData.m_shared.m_name) : prefabName;
        }

        /// <summary>
        /// The game's fishing float prefab, looked up once per scene: its reel speed, stamina and line length are fields the
        /// prefab serializes, so they are read rather than written down here. Null when there is none.
        /// </summary>
        private static FishingFloat FloatPrefab()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || scene == searched)
                return floatPrefab;
            searched = scene;
            floatPrefab = null;
            foreach (GameObject prefab in scene.m_prefabs)
            {
                floatPrefab = prefab != null ? prefab.GetComponent<FishingFloat>() : null;
                if (floatPrefab != null)
                    break;
            }
            return floatPrefab;
        }

        /// <summary>
        /// A line that explains its term: the term is its own word, filled into the line's sentence as $1 before the
        /// values, so the hover finds it in the line in every language.
        /// </summary>
        private static void Line(SkillPage page, string termKey, string lineKey, string tip, params object[] values)
        {
            string term = Lang.Get(termKey);
            object[] filled = new object[values.Length + 1];
            filled[0] = term;
            values.CopyTo(filled, 1);
            page.Line(Lang.Format(lineKey, filled), term, tip);
        }

        private static string Share(float percentAt100, float level) => SkillPage.Percent(FishSkill.Share(percentAt100, level));
    }
}
