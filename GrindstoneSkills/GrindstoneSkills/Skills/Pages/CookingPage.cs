using Translations;
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
            page.About = Lang.Get("gs_page_cooking_about");
            KitchenLines(page);
            YieldLines(page);
            ExperienceLines(page);
        }

        private static void KitchenLines(SkillPage page)
        {
            float level = page.Level;
            page.Heading(Lang.Get("gs_page_cooking_heading_kitchen"));
            if (KitchenSettings.CookingSpeed.Value > 0f)
                Line(page, "gs_page_cooking_speed", "gs_page_cooking_speed_line", Lang.Get("gs_page_cooking_speed_tip"),
                    P(Perks.CookingSpeed(level) - 1f));
            if (KitchenSettings.ExtraBurnTime.Value > 0f)
                Line(page, "gs_page_cooking_burn", "gs_page_cooking_burn_line", Lang.Get("gs_page_cooking_burn_tip"),
                    P(Perks.ExtraBurnTime(level)));
            CraftTime(page);
            if (KitchenSettings.FermentingSpeed.Value > 0f)
                Line(page, "gs_page_cooking_ferment", "gs_page_cooking_ferment_line", Lang.Get("gs_page_cooking_ferment_tip"),
                    P(Perks.FermentingSpeed(level) - 1f));
        }

        /// <summary>The game's own Cooking bonus: kitchen crafting takes 1 - factor x m_craftDurationSkillMaxDecrease of its time.</summary>
        private static void CraftTime(SkillPage page)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_craftDurationSkillMaxDecrease <= 0f)
                return;
            Line(page, "gs_page_cooking_craft", "gs_page_cooking_craft_line", Lang.Get("gs_page_cooking_craft_tip"),
                P(page.Factor * gui.m_craftDurationSkillMaxDecrease));
        }

        private static void YieldLines(SkillPage page)
        {
            float level = page.Level;
            if (KitchenSettings.ExtraFoodChance.Value > 0f)
                Line(page, "gs_page_cooking_extra_food", "gs_page_cooking_extra_food_line", Lang.Get("gs_page_cooking_extra_food_tip"),
                    P(Perks.ExtraFoodChance(level)), Perks.ExtraFoodAmount);
            if (KitchenSettings.IngredientSaveChance.Value > 0f)
                Line(page, "gs_page_cooking_ingredient", "gs_page_cooking_ingredient_line", Lang.Get("gs_page_cooking_ingredient_tip"),
                    P(Perks.IngredientSaveChance(level)));
        }

        private static void ExperienceLines(SkillPage page)
        {
            float tierMax = Mathf.Max(1f, ExperienceSettings.TierMaximum.Value);
            bool tier = ExperienceSettings.TierScaling.Value && tierMax > 1f;
            float discovery = Mathf.Max(1f, ExperienceSettings.DiscoveryMultiplier.Value);
            float multiplier = XpScaling.Multiplier;
            if (!tier && discovery <= 1f && multiplier == 1f)
                return;
            page.Heading(Lang.Get("gs_page_cooking_heading_experience"));
            if (tier)
                Line(page, "gs_page_cooking_rich", "gs_page_cooking_rich_line", TierTip(tierMax), N(tierMax));
            if (discovery > 1f)
                Line(page, "gs_page_cooking_new_dish", "gs_page_cooking_new_dish_line", Lang.Get("gs_page_cooking_new_dish_tip"), N(discovery));
            if (multiplier != 1f)
                page.Line(Lang.Format("gs_page_cooking_experience_line", N(multiplier)));
        }

        private static string TierTip(float tierMax)
        {
            float reference = Mathf.Max(1f, ExperienceSettings.TierReferenceValue.Value);
            return Lang.Format("gs_page_cooking_rich_tip", N(reference), N(tierMax), N(reference * tierMax));
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

        private static string P(float share) => SkillPage.Percent(share);

        private static string N(float value) => SkillPage.Number(value);
    }
}
