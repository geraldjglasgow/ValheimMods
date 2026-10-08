using OpenKeep.Core;

namespace OpenKeep.Recipes
{
    /// <summary>The "$ok_recipe..." words of the module, registered at Initialize. <see cref="Format"/> fills {0} placeholders.</summary>
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
            { "Ammo", "Arrows, bolts, missiles and bait." },
            { "Weapons", "Swords, axes, maces, knives, spears, atgeirs, staffs and every other weapon, torches too." },
            { "Bows", "Bows." },
            { "Armour", "Helmets, chest and leg armour, capes and utility items." },
            { "Shields", "Every kind of shield." },
            { "Tools", "The hammer, the hoe, the cultivator and other build tools." },
            { "Food", "Dishes and anything else that feeds you." },
            { "Materials", "Bars, refined materials and other crafting materials." },
        };

        private const string CategoryClick = "\n\nClick: only these recipes. Click others to add them, click a lit one to take it away; with none lit every recipe shows.";

        public static string CategoryTopic(RecipeCategory category) => "$ok_recipe_cat_" + Word(category);

        public static string CategoryTip(RecipeCategory category) => "$ok_recipe_cat_" + Word(category) + "_tip";

        private static string Word(RecipeCategory category) => category.ToString().ToLowerInvariant();

        public static void Register()
        {
            Language.Add("ok_recipe_search", "Search");
            Language.Add("ok_recipe_search_topic", "Search recipes");
            Language.Add("ok_recipe_search_tip", "Words: in the item's name.\n@word: needs a material with that word.\n-word: leave those out.\nEscape clears the search.");
            Language.Add("ok_recipe_only_topic", "Favourites only");
            Language.Add("ok_recipe_only_tip", "Click: only favourite recipes, or all again.\nShift + click: clear every favourite.");
            Language.Add("ok_recipe_view_tip", "Click: the next view. Shift + click: the one before.");
            Language.Add("ok_recipe_favourite_topic", "Favourite");
            Language.Add("ok_recipe_favourite_tip", "Middle click a recipe in the list does the same.");
            Language.Add("ok_recipe_favourite_on", "{0} is now a favourite recipe");
            Language.Add("ok_recipe_favourite_off", "{0} is no longer a favourite recipe");
            Language.Add("ok_recipe_only_on", "Favourite recipes only");
            Language.Add("ok_recipe_only_off", "All recipes");
            Language.Add("ok_recipe_clear_ask", "Clear all {0} favourite recipes?");
            Language.Add("ok_recipe_cleared", "Favourite recipes cleared");
            Language.Add("ok_recipe_none", "No favourite recipes yet: middle click a recipe to add one");
            RegisterViews();
        }

        private static void RegisterViews()
        {
            Language.Add("ok_recipe_view_list", "List");
            Language.Add("ok_recipe_view_compact", "Compact list");
            Language.Add("ok_recipe_view_small", "Small grid");
            Language.Add("ok_recipe_view_medium", "Medium grid");
            Language.Add("ok_recipe_view_large", "Large grid");
            for (int i = 0; i < RecipeCategories.All.Length; i++)
            {
                string word = Word(RecipeCategories.All[i]);
                Language.Add("ok_recipe_cat_" + word, Categories[i, 0]);
                Language.Add("ok_recipe_cat_" + word + "_tip", Categories[i, 1] + CategoryClick);
            }
        }

        /// <summary>Localizes a word and fills its {0} placeholder; a broken translation shows unfilled rather than throwing.</summary>
        public static string Format(string word, params object[] args)
        {
            string text = Language.Localize(word);
            try
            {
                return string.Format(text, args);
            }
            catch (System.FormatException)
            {
                return text;
            }
        }
    }
}
