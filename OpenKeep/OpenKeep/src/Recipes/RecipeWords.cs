using System;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The "$ok_recipe..." words of the module (their English in translations/English.yml), as $tokens the game's
    /// tooltips and messages localize; <c>Lang.Format</c> fills a word's $1.
    /// </summary>
    public static class RecipeWords
    {
        public const string Search = "$ok_recipe_search";
        public const string SearchTopic = "$ok_recipe_search_topic";
        public const string SearchTip = "$ok_recipe_search_tip";
        public const string OnlyTopic = "$ok_recipe_only_topic";
        public const string OnlyTip = "$ok_recipe_only_tip";
        public const string ViewTip = "$ok_recipe_view_tip";
        public const string FavouriteTopic = "$ok_recipe_favourite_topic";
        public const string FavouriteTip = "$ok_recipe_favourite_tip";
        public const string FavouriteOn = "$ok_recipe_favourite_on";
        public const string FavouriteOff = "$ok_recipe_favourite_off";
        public const string OnlyOn = "$ok_recipe_only_on";
        public const string OnlyOff = "$ok_recipe_only_off";
        public const string ClearAsk = "$ok_recipe_clear_ask";
        public const string Cleared = "$ok_recipe_cleared";
        public const string NoFavourites = "$ok_recipe_none";

        private static readonly string[] Views =
        {
            "$ok_recipe_view_list", "$ok_recipe_view_compact", "$ok_recipe_view_small", "$ok_recipe_view_medium", "$ok_recipe_view_large",
        };

        public static string View(RecipeView view) => Views[(int)view];

        /// <summary>Each category's name and what it holds, in <see cref="RecipeCategories.All"/>'s order.</summary>
        private static readonly string[,] Categories =
        {
            { "$ok_recipe_cat_ammo", "$ok_recipe_cat_ammo_tip" },
            { "$ok_recipe_cat_weapons", "$ok_recipe_cat_weapons_tip" },
            { "$ok_recipe_cat_bows", "$ok_recipe_cat_bows_tip" },
            { "$ok_recipe_cat_armour", "$ok_recipe_cat_armour_tip" },
            { "$ok_recipe_cat_shields", "$ok_recipe_cat_shields_tip" },
            { "$ok_recipe_cat_tools", "$ok_recipe_cat_tools_tip" },
            { "$ok_recipe_cat_food", "$ok_recipe_cat_food_tip" },
            { "$ok_recipe_cat_materials", "$ok_recipe_cat_materials_tip" },
        };

        /// <summary>The paragraph under every category's tip: how the buttons combine.</summary>
        private const string CategoryClick = "$ok_recipe_cat_click";

        public static string CategoryTopic(RecipeCategory category) => Categories[Index(category), 0];

        public static string CategoryTip(RecipeCategory category) => Categories[Index(category), 1] + "\n\n" + CategoryClick;

        private static int Index(RecipeCategory category) => Array.IndexOf(RecipeCategories.All, category);
    }
}
