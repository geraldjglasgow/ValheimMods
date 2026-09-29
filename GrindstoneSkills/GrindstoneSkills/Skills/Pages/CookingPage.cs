using System;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cooking's page in the info pane: the star odds at the player's level, the kitchen perks with the game's own
    /// craft-time bonus, how experience scales, and the star, ingredient and trash-filter features. The numbers come
    /// from the helpers the features themselves use (<see cref="StarOdds.At"/>, <see cref="Perks"/>,
    /// <see cref="StarBonus"/>) at the player's own Cooking level, the level a cook's dishes and kitchen use.
    /// </summary>
    public static class CookingPage
    {
        private const string Star = FilterText.Star;

        public static void Write(SkillPage page)
        {
            page.About = "Better dishes and a faster kitchen. Trained by cooking.";
            float[] odds = StarOdds.At(CookLevel.Effective(page.Level, 0f));
            page.Line($"Star odds: 1{Star} {P(odds[1])}, 2{Star} {P(odds[2])}, 3{Star} {P(odds[3])}");
            KitchenLines(page);
            YieldLines(page);
            ExperienceLines(page);
            Features(page);
        }

        private static void KitchenLines(SkillPage page)
        {
            float level = page.Level;
            page.Heading("Kitchen");
            if (KitchenSettings.CookingSpeed.Value > 0f)
                page.Line($"Cooking speed +{P(Perks.CookingSpeed(level) - 1f)}", "Cooking speed",
                    "On cooking stations and the oven, for the food you put on.");
            if (KitchenSettings.ExtraBurnTime.Value > 0f)
                page.Line($"Burn delay +{P(Perks.ExtraBurnTime(level))}", "Burn delay",
                    "A done dish takes that much longer to burn. The game burns it one cook time after it is done.");
            CraftTime(page);
            if (KitchenSettings.FermentingSpeed.Value > 0f)
                page.Line($"Fermenting speed +{P(Perks.FermentingSpeed(level) - 1f)}", "Fermenting speed",
                    "For the mead bases you put in a fermenter.");
        }

        /// <summary>The game's own Cooking bonus: kitchen crafting takes 1 - factor x m_craftDurationSkillMaxDecrease of its time.</summary>
        private static void CraftTime(SkillPage page)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_craftDurationSkillMaxDecrease <= 0f)
                return;
            page.Line($"Craft time -{P(page.Factor * gui.m_craftDurationSkillMaxDecrease)}", "Craft time",
                "At the cauldron, mead cauldron and prep table. The game's own Cooking bonus.");
        }

        private static void YieldLines(SkillPage page)
        {
            float level = page.Level;
            if (KitchenSettings.ExtraFoodChance.Value > 0f)
                page.Line($"Extra food: {P(Perks.ExtraFoodChance(level))} chance of +{Perks.ExtraFoodAmount}", "Extra food",
                    "At every kitchen: cooking stations, oven, cauldron, mead cauldron and prep table. Replaces the game's own bonus food there.");
            if (KitchenSettings.IngredientSaveChance.Value > 0f)
                page.Line($"Ingredient saved: {P(Perks.IngredientSaveChance(level))}", "Ingredient saved",
                    "Chance per craft at the cauldron, mead cauldron or prep table to get one used ingredient back, stars and all.");
        }

        private static void ExperienceLines(SkillPage page)
        {
            float tierMax = Mathf.Max(1f, ExperienceSettings.TierMaximum.Value);
            bool tier = ExperienceSettings.TierScaling.Value && tierMax > 1f;
            float discovery = Mathf.Max(1f, ExperienceSettings.DiscoveryMultiplier.Value);
            float multiplier = XpScaling.Multiplier;
            if (!tier && discovery <= 1f && multiplier == 1f)
                return;
            page.Heading("Experience");
            if (tier)
                page.Line($"Rich dishes: up to x{N(tierMax)}", "Rich dishes", TierTip(tierMax));
            if (discovery > 1f)
                page.Line($"New dish: x{N(discovery)}", "New dish", "The first time you make each dish.");
            if (multiplier != 1f)
                page.Line($"Kitchen experience x{N(multiplier)}");
        }

        private static string TierTip(float tierMax)
        {
            float reference = Mathf.Max(1f, ExperienceSettings.TierReferenceValue.Value);
            return $"Experience grows with a dish's health + stamina + eitr: x1 up to {N(reference)}, x{N(tierMax)} from {N(reference * tierMax)}.";
        }

        private static void Features(SkillPage page)
        {
            page.Perk("Starred dishes", 0f, DishTip());
            float perStar = Mathf.Max(0f, OddsSettings.IngredientLevelsPerStar.Value);
            if (perStar > 0f)
                page.Perk("Starred ingredients", 0f, IngredientTip(page.Level, perStar));
            if (KitchenSettings.TrashFilter.Value)
                page.Perk("Trash filter", 0f, FilterTip());
        }

        private static string DishTip() =>
            $"A dish rolls 0 to 3 stars as it finishes, from the cook's level; meads take their base's stars. " +
            $"Eaten, 1{Star}/2{Star}/3{Star} give {PerStar(StarBonus.Food)} health, stamina and eitr and last {PerStar(StarBonus.Duration)} longer.";

        /// <summary>A bonus for 1, 2 and 3 stars: "+10%/+20%/+35%".</summary>
        private static string PerStar(Func<int, float> bonus)
        {
            string[] parts = new string[Stars.Max];
            for (int stars = 1; stars <= Stars.Max; stars++)
                parts[stars - 1] = "+" + P(bonus(stars));
            return string.Join("/", parts);
        }

        private static string IngredientTip(float level, float perStar)
        {
            float best = StarOdds.At(CookLevel.Effective(level, Stars.Max))[Stars.Max];
            return $"Each star your ingredients carry on average adds {N(perStar)} levels to the dish's roll, even past 100. " +
                $"With 3{Star} ingredients your 3{Star} odds are {P(best)}.";
        }

        /// <summary>The tip box localizes its text, so the key names follow the player's bindings, as the kitchen's hover does.</summary>
        private static string FilterTip()
        {
            string key = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? "$KEY_AltKeys + $KEY_Use" : "$KEY_AltPlace + $KEY_Use";
            return $"Set on each kitchen with {key}: it keeps {FilterText.Keeps(0)}, {FilterText.Keeps(1)}, {FilterText.Keeps(2)} " +
                $"or {FilterText.Keeps(Stars.Max)}. Dishes below it are thrown away as they finish.";
        }

        private static string P(float share) => SkillPage.Percent(share);

        private static string N(float value) => SkillPage.Number(value);
    }
}
