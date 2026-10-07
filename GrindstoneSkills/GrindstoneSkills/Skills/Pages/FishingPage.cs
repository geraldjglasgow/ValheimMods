using System.Collections.Generic;
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
                page.Line("GrindstoneSkills' Fishing is off on this server.");
                return;
            }
            page.About = "Hook more fish and land bigger ones. Trained by fighting and landing them.";
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
            page.Line($"Reel speed {speed:0.#} m/s", "Reel speed",
                "The game's own: how fast the line comes in while you reel (hold block). Half as fast while the fish thrashes.");
            float saved = 1f - Mathf.Lerp(1f, prefab.m_pullStaminaUseMaxSkillMultiplier, factor);
            float hold = Mathf.Lerp(prefab.m_hookedStaminaPerSec, prefab.m_hookedStaminaPerSecMaxSkill, factor);
            page.Line($"Reel stamina -{SkillPage.Percent(saved)}", "Reel stamina",
                $"The game's own: reeling costs that much less. A fish on the line also drains {hold:0.##} stamina per second.");
        }

        private static void Fight(SkillPage page)
        {
            float level = page.Level;
            page.Line($"Bite chance +{Share(FishingBiteSettings.BiteChanceAt100.Value, level)}", "Bite chance", BiteTip());
            page.Heading("The fight");
            page.Line($"Strike window {Strike.Window(level):0.##} s", "Strike window",
                "Time after a nibble in which reeling sets the hook. The game's own is 0.5 s.");
            if (FishingFightSettings.TensionEnabled.Value)
                page.Line($"Line tension {SkillPage.Percent(Tension.Change(true, true, level))}/s", "Line tension", TensionTip());
        }

        private static void Catch(SkillPage page)
        {
            float level = page.Level;
            page.Heading("The catch");
            if (FishingBigFishSettings.BigOneChanceAt100.Value > 0f)
                page.Line($"Big one {Share(FishingBigFishSettings.BigOneChanceAt100.Value, level)}", "Big one", BigOneTip());
            string bonus = BonusChance(level);
            if (bonus.Length > 0)
                page.Line($"Bonus item {bonus}", "Bonus item", "Chance a landed fish also brings its own bonus item. A legendary fish always does.");
            if (FishingCatchSettings.BaitSaverAt100.Value > 0f)
                page.Line($"Bait saver {Share(FishingCatchSettings.BaitSaverAt100.Value, level)}", "Bait saver",
                    "Chance a landed fish gives your bait back.");
            float snag = Snags.Chance(level);
            if (snag > 0f)
                page.Line($"Snag {SkillPage.Percent(snag)}", "Snag", SnagTip());
            if (FishingCatchSettings.CastDistanceAt100.Value > 0f || FishingCatchSettings.LineLengthAt100.Value > 0f)
                page.Line($"Cast +{Share(FishingCatchSettings.CastDistanceAt100.Value, level)}, line +{Share(FishingCatchSettings.LineLengthAt100.Value, level)}",
                    "line", LineTip(level));
        }

        private static void Perks(SkillPage page)
        {
            float perfect = FishingFightSettings.PerfectStrikeWindow.Value;
            if (perfect > 0f)
                page.Perk("Perfect Strike", 0f, $"Start reeling within {perfect:0.##} s of a nibble, when you were not reeling already, and the fish skips its first thrash.");
            page.Perk("Angler's Log", 0f, LogTip());
            page.Perk("Species Sense", FishingBiteSettings.SpeciesSenseLevel.Value,
                "A nibble names the fish, and a cast that lands tells the fishing conditions: dawn, dusk, rain, night.");
            page.Perk("Size Sense", FishingBiteSettings.SizeSenseLevel.Value,
                "A nibble also tells the fish's level, and so does looking at a fish in the water.");
            page.Perk("Double Bonus", FishingCatchSettings.DoubleBonusLevel.Value, "A landed fish's bonus roll can bring two items instead of one.");
            float legendary = FishingBigFishSettings.LegendaryChance.Value > 0f ? FishingBigFishSettings.LegendaryLevel.Value : FishSkill.MaxLevel + 1f;
            page.Perk("Legendary Fish", legendary, LegendaryTip());
            page.Perk("Grace", FishingFightSettings.GraceLevel.Value,
                $"Once per fish, running out of stamina does not lose it at once: for {SkillPage.Duration(FishingFightSettings.GraceSeconds.Value)} it takes line while your stamina comes back. Still empty after that, it gets away.");
            page.Perk("Water Sense", FishingBiteSettings.WaterSenseLevel.Value,
                "A cast that lands tells which fish in reach take your bait, and whether a legendary one lurks.");
        }

        private static string BiteTip()
        {
            string tip = $"More fish go for your float. Bites also rise {SkillPage.Number(FishingBiteSettings.DawnDuskBonus.Value)}% at dawn and dusk";
            string rain = $"{SkillPage.Number(FishingBiteSettings.RainBonus.Value)}% in rain";
            string chum = ChumNames();
            if (chum.Length == 0)
                return tip + $" and {rain}.";
            return tip + $", {rain}, and {SkillPage.Number(FishingBiteSettings.ChumBonus.Value)}% within {SkillPage.Number(FishingBiteSettings.ChumRadius.Value)} m of chum ({chum}) floating in the water.";
        }

        private static string TensionTip()
        {
            string ease = SkillPage.Percent(FishSkill.Percent(FishingFightSettings.TensionEase.Value));
            string tip = $"Reeling while the fish thrashes fills the bar under the crosshair at this rate, and a full bar snaps the line. Letting go eases it {ease}/s.";
            int thrashes = FishingFightSettings.ThrashesToTire.Value;
            return thrashes > 0
                ? tip + $" After {thrashes} thrashes the fish is spent and comes in {SkillPage.Number(FishingFightSettings.SpentReelSpeed.Value)}% faster."
                : tip;
        }

        private static string BigOneTip() =>
            $"Chance a hooked fish grows a level, rolled again up to level {FishInfo.MaxNaturalLevel}: +{SkillPage.Number(FishingBigFishSettings.NightBonus.Value)}% at night.";

        private static string SnagTip() =>
            $"Once per cast, after {SkillPage.Duration(FishingCatchSettings.SnagWait.Value)} in the water without a bite, the line can catch a find from the biome's snag list. "
            + "It reels in slow and heavy, and gives no experience.";

        private static string LineTip(float level)
        {
            FishingFloat prefab = FloatPrefab();
            if (prefab == null)
                return "Casts fly farther, and the line snaps further from the rod.";
            float reach = prefab.m_maxDistance * (1f + FishSkill.Share(FishingCatchSettings.LineLengthAt100.Value, level));
            return $"Casts fly farther, and the line snaps only once the float is {SkillPage.Number(reach)} m from the rod.";
        }

        private static string LogTip()
        {
            string tip = "Every species and size you land is logged with your heaviest catch. Type /fishlog in chat, or read a fish's tooltip.";
            return FishingExperienceSettings.Discovery.Value > 0f || FishingExperienceSettings.NewSize.Value > 0f
                ? tip + " Each new species or size gives bonus experience."
                : tip;
        }

        private static string LegendaryTip()
        {
            int thrashes = FishingBigFishSettings.LegendaryThrashes.Value;
            string tires = thrashes > 0 ? $"tire after {thrashes} thrashes" : "never tire";
            return $"{SkillPage.Number(FishingBigFishSettings.LegendaryChance.Value)}% of spawned fish are legendary: level {FishInfo.LegendaryLevel}, glowing, and they {tires}. "
                + "From this level they take your bait, and they always bring a bonus item.";
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
            return Mathf.Approximately(low, high) ? SkillPage.Percent(low) : $"{SkillPage.Percent(low)}-{SkillPage.Percent(high)}";
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

        private static string Share(float percentAt100, float level) => SkillPage.Percent(FishSkill.Share(percentAt100, level));
    }
}
