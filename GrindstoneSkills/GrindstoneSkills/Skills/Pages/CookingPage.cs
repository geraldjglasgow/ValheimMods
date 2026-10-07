using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cooking's page in the info pane: the kitchen perks with the game's own craft-time bonus, the extra food and
    /// saved ingredients, and how experience scales. The numbers come from the helpers the perks themselves use
    /// (<see cref="Perks"/>) at the player's own Cooking level, the level a cook's kitchen uses.
    /// </summary>
    public static class CookingPage
    {
        public static void Write(SkillPage page)
        {
            page.About = "A faster, more generous kitchen. Trained by cooking.";
            KitchenLines(page);
            YieldLines(page);
            ExperienceLines(page);
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
                    "Chance per craft at the cauldron, mead cauldron or prep table to get one used ingredient back.");
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

        private static string P(float share) => SkillPage.Percent(share);

        private static string N(float value) => SkillPage.Number(value);
    }
}
