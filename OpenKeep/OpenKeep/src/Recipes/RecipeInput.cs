using OpenKeep.Core;
using OpenKeep.Salvage;
using OpenKeep.Store;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The keyboard in the crafting panel, every frame the inventory shows: the Search Key puts the cursor into the
    /// search field, Store's Favourite Item Key over a recipe row makes it a favourite or not (over an inventory slot
    /// the key keeps its Store meaning), and the search follows the typed text once typing pauses.
    /// </summary>
    public static class RecipeInput
    {
        public static void Poll(InventoryGui gui)
        {
            if (!InventoryGui.IsVisible() || Player.m_localPlayer == null)
                return;
            SearchBar.Tick(gui);
            if (SalvageTab.Active || UnifiedPopup.IsVisible())
                return;
            if (Keys.Pressed(RecipeListSettings.SearchKey))
                SearchBar.Focus();
            else if (RecipeFavourites.Enabled && Keys.Pressed(StoreSettings.FavouriteItemKey))
                FavouriteHovered();
        }

        private static void FavouriteHovered()
        {
            Recipe recipe = RecipeRows.Hovered;
            if (recipe != null)
                RecipeFavourites.Toggle(recipe);
        }
    }
}
