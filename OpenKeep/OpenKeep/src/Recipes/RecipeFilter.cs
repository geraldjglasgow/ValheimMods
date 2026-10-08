using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// After the game has built the recipe list (Craft or Upgrade tab, the game's own sort order applied): the rows the
    /// search, the categories or favourites only hide are destroyed and dropped from the game's list, favourites move to the top in
    /// the game's order, and the rows are laid out for the view. The game picks the selection afterwards from what is
    /// left (the first row when the selected recipe went), so crafting only ever sees listed recipes.
    /// </summary>
    public static class RecipeFilter
    {
        public static void Apply(InventoryGui gui)
        {
            List<InventoryGui.RecipeDataPair> list = gui.m_availableRecipes;
            RecipeQuery query = SearchBar.Query;
            bool only = RecipeFavourites.OnlyFavourites;
            if (!query.Empty || only || RecipeCategories.Filtering)
                Drop(list, query, only);
            if (RecipeFavourites.Enabled && RecipeListSettings.FavouritesFirst.Value)
                FavouritesFirst(list);
            RecipeLayout.Arrange(gui);
            RecipeRows.Decorate(gui);
        }

        private static void Drop(List<InventoryGui.RecipeDataPair> list, RecipeQuery query, bool only)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                InventoryGui.RecipeDataPair pair = list[i];
                if ((only && !RecipeFavourites.Is(pair.Recipe)) || !RecipeCategories.Shows(pair.Recipe)
                    || !query.Matches(pair.Recipe, Quality(pair.ItemData)))
                {
                    // Destroyed before this frame renders, as the game destroys its own rows.
                    if (pair.InterfaceElement != null)
                        Object.Destroy(pair.InterfaceElement);
                    list.RemoveAt(i);
                }
            }
        }

        /// <summary>A stable sort: favourites first, each group in the order the game sorted it.</summary>
        private static void FavouritesFirst(List<InventoryGui.RecipeDataPair> list)
        {
            List<InventoryGui.RecipeDataPair> sorted = list.OrderBy(pair => RecipeFavourites.Is(pair.Recipe) ? 0 : 1).ToList();
            list.Clear();
            list.AddRange(sorted);
        }

        /// <summary>The quality a row crafts: 1 for a new item, the next level for an upgrade of an item.</summary>
        public static int Quality(ItemDrop.ItemData item) => item == null ? 1 : item.m_quality + 1;
    }
}
