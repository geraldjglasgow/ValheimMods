using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Husbandry's page in the info pane: taming, breeding and yield at the player's level, and the named perks with the
    /// level each needs. The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="HusbandrySkill.Share"/>, <see cref="BreedingPace.ExtraRoom"/>). Some features follow the player's own
    /// level (butchering, honey, pack leader, calm), others the best keeper's near the animal; the tips say which. A
    /// feature whose setting is 0 is left out, as are Taming Levels while empty.
    /// </summary>
    public static class HusbandryPage
    {
        private const float MaxToughness = 0.9f;

        public static void Write(SkillPage page)
        {
            page.About = Lang.Get("gs_page_husbandry_about");
            if (!HusbandrySkill.Active)
            {
                page.Line(Lang.Get("gs_page_husbandry_off"));
                return;
            }
            Taming(page);
            Breeding(page);
            Yield(page);
            NamedPerks(page);
        }

        private static void Taming(SkillPage page)
        {
            float speed = HusbandrySkill.Share(HusbandryTamingSettings.TamingSpeed.Value, page.Level);
            float fed = HusbandrySkill.Share(HusbandryTamingSettings.FedDuration.Value, page.Level);
            page.Heading(Lang.Get("gs_page_husbandry_heading_taming"));
            Tameable boar = PrefabPart<Tameable>("Boar");
            Line(page, "gs_page_husbandry_taming", "gs_page_husbandry_taming_line", TamingTip(boar, speed), SkillPage.Percent(speed));
            Gates(page);
            Line(page, "gs_page_husbandry_fed", "gs_page_husbandry_fed_line", FedTip(boar, fed), SkillPage.Percent(fed));
            Pack(page);
        }

        /// <summary>The taming speed tip, with a boar's taming time before and after as the example once the prefabs exist.</summary>
        private static string TamingTip(Tameable boar, float speed)
        {
            if (boar == null)
                return Lang.Get("gs_page_husbandry_taming_tip");
            return Lang.Format("gs_page_husbandry_taming_tip_boar", SkillPage.Duration(boar.m_tamingTime),
                SkillPage.Duration(boar.m_tamingTime / (1f + speed)));
        }

        /// <summary>The fed time tip, with a boar's fed time before and after as the example once the prefabs exist.</summary>
        private static string FedTip(Tameable boar, float fed)
        {
            if (boar == null)
                return Lang.Format("gs_page_husbandry_fed_tip", KeeperRange());
            return Lang.Format("gs_page_husbandry_fed_tip_boar", KeeperRange(), SkillPage.Duration(boar.m_fedDuration),
                SkillPage.Duration(boar.m_fedDuration * (1f + fed)));
        }

        /// <summary>Taming Levels, while it lists any creature: how many of them the player's level passes.</summary>
        private static void Gates(SkillPage page)
        {
            Dictionary<string, float> gates = TamingLevels.Parse(HusbandryTamingSettings.TamingLevels.Value ?? "");
            if (gates.Count == 0)
                return;
            int passed = 0;
            List<string> listed = new List<string>();
            foreach (KeyValuePair<string, float> gate in gates)
            {
                listed.Add(Lang.Format("gs_page_husbandry_gate", gate.Key, gate.Value.ToString("0")));
                if (page.Level >= gate.Value)
                    passed++;
            }
            Line(page, "gs_page_husbandry_gates", "gs_page_husbandry_gates_line",
                Lang.Format("gs_page_husbandry_gates_tip", string.Join(", ", listed)), passed, gates.Count);
        }

        private static void Pack(SkillPage page)
        {
            float damage = HusbandryCompanionSettings.PackDamage.Value;
            float toughness = HusbandryCompanionSettings.PackToughness.Value;
            if (damage <= 0f && toughness <= 0f)
                return;
            float taken = Mathf.Min(HusbandrySkill.Share(toughness, page.Level), MaxToughness);
            Line(page, "gs_page_husbandry_pack", "gs_page_husbandry_pack_line", Lang.Get("gs_page_husbandry_pack_tip"),
                Percent(damage, page.Level), SkillPage.Percent(taken));
        }

        private static void Breeding(SkillPage page)
        {
            float level = page.Level;
            page.Heading(Lang.Get("gs_page_husbandry_heading_breeding"));
            Line(page, "gs_page_husbandry_breeding", "gs_page_husbandry_breeding_line",
                Lang.Format("gs_page_husbandry_breeding_tip", KeeperRange()), Percent(HusbandryBreedingSettings.BreedingSpeed.Value, level));
            if (HusbandryBreedingSettings.HerdSize.Value > 0)
                Line(page, "gs_page_husbandry_herd", "gs_page_husbandry_herd_line", Lang.Get("gs_page_husbandry_herd_tip"),
                    BreedingPace.ExtraRoom(level));
            Offspring(page);
            if (HusbandryBreedingSettings.Twins.Value > 0f)
                Line(page, "gs_page_husbandry_twins", "gs_page_husbandry_twins_line", Lang.Get("gs_page_husbandry_twins_tip"),
                    Percent(HusbandryBreedingSettings.Twins.Value, level));
            Growth(page);
        }

        /// <summary>Better offspring: one star over the parent up to Max Offspring Level, or Elite Creatures Reborn's cap.</summary>
        private static void Offspring(SkillPage page)
        {
            float chance = HusbandryBreedingSettings.BetterOffspring.Value;
            int stars = HusbandryBreedingSettings.MaxOffspringLevel.Value - 1;
            bool ecr = OffspringStar.EcrInstalled;
            if (chance <= 0f || (!ecr && stars < 1))
                return;
            string tip = ecr
                ? Lang.Get("gs_page_husbandry_offspring_tip_ecr")
                : Lang.Format(stars == 1 ? "gs_page_husbandry_offspring_tip_one" : "gs_page_husbandry_offspring_tip_many", stars);
            Line(page, "gs_page_husbandry_offspring", "gs_page_husbandry_offspring_line", tip, Percent(chance, page.Level));
        }

        private static void Growth(SkillPage page)
        {
            if (HusbandryBreedingSettings.GrowthSpeed.Value <= 0f)
                return;
            float speed = HusbandrySkill.Share(HusbandryBreedingSettings.GrowthSpeed.Value, page.Level);
            Growup piglet = PrefabPart<Growup>("Boar_piggy");
            string tip = piglet != null
                ? Lang.Format("gs_page_husbandry_growth_tip_piglet", KeeperRange(), SkillPage.Duration(piglet.m_growTime),
                    SkillPage.Duration(piglet.m_growTime / (1f + speed)))
                : Lang.Format("gs_page_husbandry_growth_tip", KeeperRange());
            Line(page, "gs_page_husbandry_growth", "gs_page_husbandry_growth_line", tip, SkillPage.Percent(speed));
        }

        private static void Yield(SkillPage page)
        {
            float level = page.Level;
            float produce = HusbandryYieldSettings.ProduceChance.Value;
            page.Heading(Lang.Get("gs_page_husbandry_heading_yield"));
            if (HusbandryYieldSettings.ButcherYield.Value > 0f)
                Line(page, "gs_page_husbandry_butcher", "gs_page_husbandry_butcher_line", Lang.Get("gs_page_husbandry_butcher_tip"),
                    Percent(HusbandryYieldSettings.ButcherYield.Value, level));
            if (produce > 0f)
                Line(page, "gs_page_husbandry_produce", "gs_page_husbandry_produce_line", Lang.Format("gs_page_husbandry_produce_tip", KeeperRange()),
                    Percent(produce, level), SkillPage.Duration(HusbandryYieldSettings.ProduceInterval.Value));
            if (HusbandryYieldSettings.ExtraHoney.Value > 0f)
                Line(page, "gs_page_husbandry_honey", "gs_page_husbandry_honey_line", Lang.Get("gs_page_husbandry_honey_tip"),
                    Percent(HusbandryYieldSettings.ExtraHoney.Value, level));
        }

        private static void NamedPerks(SkillPage page)
        {
            PettingPerk(page);
            page.Perk(Lang.Get("gs_perk_starredeggs"), 0f, Lang.Get("gs_page_husbandry_eggs_tip"));
            page.Perk(Lang.Get("gs_perk_animallore"), HusbandrySettings.LoreLevel.Value, Lang.Get("gs_page_husbandry_lore_tip"));
            page.Perk(Lang.Get("gs_perk_animalfeeder"), HusbandryCompanionSettings.FeederLevel.Value, FeederTip());
            page.Perk(Lang.Get("gs_perk_calm"), HusbandryTamingSettings.CalmLevel.Value,
                Lang.Format("gs_page_husbandry_calm_tip", SkillPage.Duration(HusbandryTamingSettings.CalmBreakTime.Value)));
        }

        private static void PettingPerk(SkillPage page)
        {
            float duration = HusbandryBreedingSettings.ContentDuration.Value;
            if (duration <= 0f)
                return;
            float bonus = HusbandryBreedingSettings.ContentBonus.Value;
            string tip = bonus > 0f
                ? Lang.Format("gs_page_husbandry_petting_tip_breeds", SkillPage.Duration(duration), SkillPage.Number(bonus))
                : Lang.Format("gs_page_husbandry_petting_tip", SkillPage.Duration(duration));
            page.Perk(Lang.Get("gs_perk_petting"), 0f, tip);
        }

        private static string FeederTip()
        {
            string cost = FeederCost();
            string build = cost.Length > 0
                ? Lang.Format("gs_page_husbandry_feeder_build_cost", cost)
                : Lang.Get("gs_page_husbandry_feeder_build");
            return build + " " + Lang.Format("gs_page_husbandry_feeder_walk", SkillPage.Number(HusbandryCompanionSettings.FeederRange.Value));
        }

        /// <summary>The feeder's cost as the hammer has it ("10 Wood, 4 Leather scraps"); empty before the piece exists.</summary>
        private static string FeederCost()
        {
            Piece piece = FeederPrefab.Piece;
            List<string> parts = new List<string>();
            if (piece == null || piece.m_resources == null)
                return "";
            foreach (Piece.Requirement requirement in piece.m_resources)
            {
                if (requirement != null && requirement.m_resItem != null)
                    parts.Add(Lang.Format("gs_page_husbandry_feeder_cost_item", requirement.m_amount,
                        Localize(requirement.m_resItem.m_itemData.m_shared.m_name)));
            }
            return string.Join(", ", parts);
        }

        private static string Localize(string token) =>
            Localization.instance != null ? Localization.instance.Localize(token) : token;

        /// <summary>A component of one of the game's prefabs, for an example with its real numbers; null before the prefabs load.</summary>
        private static T PrefabPart<T>(string prefab) where T : Component
        {
            GameObject found = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            return found != null ? found.GetComponent<T>() : null;
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

        /// <summary>The keeper range as a number; the sentences around it carry the unit.</summary>
        private static string KeeperRange() => SkillPage.Number(HusbandrySettings.KeeperRange.Value);

        private static string Percent(float percentAt100, float level) => SkillPage.Percent(HusbandrySkill.Share(percentAt100, level));
    }
}
