using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Husbandry's page in the info pane: taming, breeding and yield at the player's level, and the named perks with the
    /// level each needs. The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="HusbandrySkill.Share"/>, <see cref="BreedingPace.ExtraRoom"/>). Some features follow the player's own
    /// level (butchering, honey, pack leader, calm), others the best keeper's near the animal; the tips say which. A
    /// feature whose setting is 0 is left out, as are Taming Levels while empty and Prime Cuts while off.
    /// </summary>
    public static class HusbandryPage
    {
        private const float MaxToughness = 0.9f;

        public static void Write(SkillPage page)
        {
            page.About = "Tame faster and raise better animals. Trained by taming, feeding and breeding.";
            if (!HusbandrySkill.Active)
            {
                page.Line("Turned off on this server.");
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
            page.Heading("Taming");
            Tameable boar = PrefabPart<Tameable>("Boar");
            string example = boar != null ? $": a boar's {SkillPage.Duration(boar.m_tamingTime)} become {SkillPage.Duration(boar.m_tamingTime / (1f + speed))}" : "";
            page.Line($"Taming speed +{SkillPage.Percent(speed)}", "Taming speed",
                $"The best level within the creature's taming range counts{example}. Stacks with the game's taming boost.");
            Gates(page);
            string fedExample = boar != null ? $": a boar's {SkillPage.Duration(boar.m_fedDuration)} become {SkillPage.Duration(boar.m_fedDuration * (1f + fed))}" : "";
            page.Line($"Fed time +{SkillPage.Percent(fed)}", "Fed time",
                $"For animals that eat within {KeeperRange()} of you{fedExample}. They tame, breed and heal only while fed.");
            Pack(page);
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
                listed.Add($"{gate.Key} {gate.Value:0}");
                if (page.Level >= gate.Value)
                    passed++;
            }
            page.Line($"Level gates: you pass {passed} of {gates.Count}", "Level gates",
                $"These make taming progress only with a player of that level within their taming range: {string.Join(", ", listed)}.");
        }

        private static void Pack(SkillPage page)
        {
            float damage = HusbandryCompanionSettings.PackDamage.Value;
            float toughness = HusbandryCompanionSettings.PackToughness.Value;
            if (damage <= 0f && toughness <= 0f)
                return;
            float taken = Mathf.Min(HusbandrySkill.Share(toughness, page.Level), MaxToughness);
            page.Line($"Pack leader: damage +{Percent(damage, page.Level)}, taken -{SkillPage.Percent(taken)}", "Pack leader",
                "A tamed wolf that follows you deals that much more damage and takes that much less.");
        }

        private static void Breeding(SkillPage page)
        {
            float level = page.Level;
            page.Heading("Breeding");
            page.Line($"Breeding speed +{Percent(HusbandryBreedingSettings.BreedingSpeed.Value, level)}", "Breeding speed",
                $"Shorter pregnancies and fewer skipped love checks for animals within {KeeperRange()} of you. The best level near them counts.");
            if (HusbandryBreedingSettings.HerdSize.Value > 0)
                page.Line($"Herd size +{BreedingPace.ExtraRoom(level)}", "Herd size",
                    "More animals of a kind may crowd together before breeding stops. The game allows 4 to 10 by kind.");
            Offspring(page);
            if (HusbandryBreedingSettings.Twins.Value > 0f)
                page.Line($"Twins {Percent(HusbandryBreedingSettings.Twins.Value, level)}", "Twins",
                    "Chance that a birth is followed by a second one (a second egg for hens) half a minute later. A twin never has twins.");
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
                ? "Chance that a newborn gets one star more than Elite Creatures Reborn rolls, up to one above the stronger parent."
                : $"Chance that a newborn or a laid egg is one star above its parent, up to {stars} {(stars == 1 ? "star" : "stars")}.";
            page.Line($"Better offspring {Percent(chance, page.Level)}", "Better offspring", tip);
        }

        private static void Growth(SkillPage page)
        {
            if (HusbandryBreedingSettings.GrowthSpeed.Value <= 0f)
                return;
            float speed = HusbandrySkill.Share(HusbandryBreedingSettings.GrowthSpeed.Value, page.Level);
            Growup piglet = PrefabPart<Growup>("Boar_piggy");
            string example = piglet != null ? $": a piglet's {SkillPage.Duration(piglet.m_growTime)} become {SkillPage.Duration(piglet.m_growTime / (1f + speed))}" : "";
            page.Line($"Growth speed +{SkillPage.Percent(speed)}", "Growth speed",
                $"Tamed young grow up and warm eggs hatch sooner within {KeeperRange()} of you{example}.");
        }

        private static void Yield(SkillPage page)
        {
            float level = page.Level;
            float produce = HusbandryYieldSettings.ProduceChance.Value;
            page.Heading("Yield");
            if (HusbandryYieldSettings.ButcherYield.Value > 0f)
                page.Line($"Butcher yield +{Percent(HusbandryYieldSettings.ButcherYield.Value, level)}", "Butcher yield",
                    "More meat, hides and feathers from tamed animals you kill, never trophies. A fraction is a chance of one more.");
            if (produce > 0f)
                page.Line($"Produce {Percent(produce, level)} every {SkillPage.Duration(HusbandryYieldSettings.ProduceInterval.Value)}", "Produce",
                    $"Chance that a fed tamed animal within {KeeperRange()} of you drops feathers, scraps, pelts or hides without being killed. The best level near it counts.");
            if (HusbandryYieldSettings.ExtraHoney.Value > 0f)
                page.Line($"Extra honey {Percent(HusbandryYieldSettings.ExtraHoney.Value, level)} per honey", "Extra honey",
                    "Chance of one more for each honey you take from a beehive.");
        }

        private static void NamedPerks(SkillPage page)
        {
            PettingPerk(page);
            page.Perk("Starred Eggs", 0f,
                "Eggs keep their hen's stars: they show them, stack apart, hatch starred chicks and raise a dish's odds in Cooking.");
            if (HusbandryYieldSettings.PrimeCuts.Value)
                page.Perk("Prime Cuts", 0f,
                    $"Meat from a starred tamed animal keeps its stars (up to {Stars.Max}) and raises a dish's odds in Cooking.");
            page.Perk("Animal Lore", HusbandrySettings.LoreLevel.Value,
                "Hovering an animal or an egg shows its timers: fed, taming, love or pregnancy, herd room, contentment, growing up, hatching.");
            page.Perk(FeederPrefab.DisplayName, HusbandryCompanionSettings.FeederLevel.Value, FeederTip());
            page.Perk("Calm", HusbandryTamingSettings.CalmLevel.Value,
                $"A creature you have started taming neither flees from nor attacks you, so taming goes on beside you. Hurt it and it fights you for {SkillPage.Duration(HusbandryTamingSettings.CalmBreakTime.Value)}.");
        }

        private static void PettingPerk(SkillPage page)
        {
            float duration = HusbandryBreedingSettings.ContentDuration.Value;
            if (duration <= 0f)
                return;
            float bonus = HusbandryBreedingSettings.ContentBonus.Value;
            string breeds = bonus > 0f ? $" and breeds {SkillPage.Number(bonus)}% faster" : "";
            page.Perk("Petting", 0f,
                $"Use a tamed animal you cannot command (not a wolf): it is content for {SkillPage.Duration(duration)}{breeds}. Only petting one that is not content yet trains the skill.");
        }

        private static string FeederTip()
        {
            string cost = FeederCost();
            string build = cost.Length > 0 ? $"Hammer, Misc, at a workbench: {cost}." : "Hammer, Misc, at a workbench.";
            return $"{build} Hungry tamed animals, and animals being tamed, within {SkillPage.Number(HusbandryCompanionSettings.FeederRange.Value)} m walk to it and eat its food.";
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
                    parts.Add($"{requirement.m_amount} {Localize(requirement.m_resItem.m_itemData.m_shared.m_name)}");
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

        private static string KeeperRange() => $"{SkillPage.Number(HusbandrySettings.KeeperRange.Value)} m";

        private static string Percent(float percentAt100, float level) => SkillPage.Percent(HusbandrySkill.Share(percentAt100, level));
    }
}
