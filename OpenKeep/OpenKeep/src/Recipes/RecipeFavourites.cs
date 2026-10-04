using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The character's favourite recipes, saved with it in its custom data: <c>OpenKeep.favouriteRecipes</c> (recipe
    /// keys) and the favourites only view, <c>OpenKeep.favouriteRecipesOnly</c>. The set is read from the character once
    /// and kept until it changes here or another character loads. Every change rebuilds the open crafting panel, so stars, order and the favourites only list follow at once.
    /// </summary>
    public static class RecipeFavourites
    {
        private const string SetKey = "favouriteRecipes";
        private const string OnlyKey = "favouriteRecipesOnly";

        private static HashSet<string> cached;
        private static Player cachedFor;

        public static bool Enabled => RecipeListSettings.Favourites.Value;

        public static bool Is(Recipe recipe) => Enabled && recipe != null && Keys().Contains(RecipeKeys.Of(recipe));

        public static bool OnlyFavourites => Enabled && CharacterData.GetFlag(OnlyKey);

        public static int Count => Keys().Count;

        /// <summary>Makes the recipe a favourite or not, says so, and rebuilds the panel.</summary>
        public static void Toggle(Recipe recipe)
        {
            if (!Enabled || recipe == null || Player.m_localPlayer == null)
                return;
            HashSet<string> keys = new HashSet<string>(Keys());
            string key = RecipeKeys.Of(recipe);
            bool on = keys.Add(key);
            if (!on)
                keys.Remove(key);
            Write(keys);
            string name = Language.Localize(recipe.m_item.m_itemData.m_shared.m_name);
            Messages.Center(RecipeWords.Format(on ? RecipeWords.FavouriteOn : RecipeWords.FavouriteOff, name));
        }

        /// <summary>Favourites only on or off; refused, with a hint, while there is no favourite to show.</summary>
        public static void ToggleOnly()
        {
            if (!Enabled || Player.m_localPlayer == null)
                return;
            bool on = !CharacterData.GetFlag(OnlyKey);
            if (on && Count == 0)
            {
                Messages.Center(RecipeWords.NoFavourites);
                return;
            }
            CharacterData.SetFlag(OnlyKey, on);
            Messages.Center(on ? RecipeWords.OnlyOn : RecipeWords.OnlyOff);
            Rebuild();
        }

        /// <summary>Asks with the game's yes/no popup (gamepad works), then clears every favourite and favourites only.</summary>
        public static void AskClear()
        {
            int count = Count;
            if (!Enabled || count == 0)
            {
                Messages.Center(RecipeWords.NoFavourites);
                return;
            }
            if (!UnifiedPopup.IsAvailable())
                return;
            string text = RecipeWords.Format(RecipeWords.ClearAsk, count);
            UnifiedPopup.Push(new YesNoPopup(RecipeWords.OnlyTopic, text, Clear, UnifiedPopup.Pop));
        }

        private static void Clear()
        {
            UnifiedPopup.Pop();
            CharacterData.SetFlag(OnlyKey, false);
            Write(new HashSet<string>());
            Messages.Center(RecipeWords.Cleared);
        }

        private static void Write(HashSet<string> keys)
        {
            CharacterData.SetSet(SetKey, keys);
            if (keys.Count == 0)
                CharacterData.SetFlag(OnlyKey, false);
            cached = null;
            Rebuild();
        }

        /// <summary>Read from the character once, again after every change here or when another character loads.</summary>
        private static HashSet<string> Keys()
        {
            Player player = Player.m_localPlayer;
            if (cached == null || cachedFor != player)
            {
                cachedFor = player;
                cached = CharacterData.GetSet(SetKey);
            }
            return cached;
        }

        /// <summary>The open panel's list again (the game's own rebuild, so every hook on it runs).</summary>
        public static void Rebuild()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui != null && InventoryGui.IsVisible() && Player.m_localPlayer != null)
                gui.UpdateCraftingPanel();
        }
    }
}
