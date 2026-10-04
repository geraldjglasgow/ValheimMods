using OpenKeep.Tracker;
using PatchGuard;
using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// What every listed recipe row gets besides its place: the favourite star, a middle click that makes it a
    /// favourite, a right click that tracks or untracks it, the hover that Stow's Favourite Item Key acts on, and in a
    /// grid a tooltip with the name (the tile hides it). The rows are new on every rebuild, so nothing is undone.
    /// </summary>
    public static class RecipeRows
    {
        private static GameObject hoveredRow;
        private static Recipe hoveredRecipe;

        /// <summary>The recipe whose row the pointer is over now, or null.</summary>
        public static Recipe Hovered
        {
            get
            {
                if (hoveredRow == null || !hoveredRow.activeInHierarchy)
                    return null;
                RectTransform rect = (RectTransform)hoveredRow.transform;
                return RectTransformUtility.RectangleContainsScreenPoint(rect, ZInput.pointerPosition) ? hoveredRecipe : null;
            }
        }

        public static void Decorate(InventoryGui gui)
        {
            hoveredRow = null;
            bool tile = RecipeLayout.IsGrid;
            foreach (InventoryGui.RecipeDataPair pair in gui.m_availableRecipes)
            {
                GameObject row = pair.InterfaceElement;
                if (row == null || pair.Recipe == null)
                    continue;
                Hook(row, pair.Recipe, RecipeFilter.Quality(pair.ItemData));
                if (RecipeFavourites.Is(pair.Recipe))
                    RecipeStar.Add(row, tile);
                if (tile)
                    RecipeTips.Set(row, pair.Recipe.m_item.m_itemData.m_shared.m_name, AmountText(pair.Recipe));
            }
        }

        private static void Hook(GameObject row, Recipe recipe, int quality)
        {
            UIInputHandler input = row.GetComponent<UIInputHandler>();
            if (input == null)
                input = row.AddComponent<UIInputHandler>();
            input.m_onMiddleClick = _ => Guard.Run("recipe favourite", () => RecipeFavourites.Toggle(recipe));
            input.m_onRightClick = _ => Guard.Run("recipe track", () => TrackerList.Toggle(recipe, quality));
            input.m_onPointerEnter = _ => Enter(row, recipe);
            input.m_onPointerExit = _ => Exit(row);
        }

        private static void Enter(GameObject row, Recipe recipe)
        {
            hoveredRow = row;
            hoveredRecipe = recipe;
        }

        private static void Exit(GameObject row)
        {
            if (hoveredRow == row)
                hoveredRow = null;
        }

        /// <summary>The tooltip's line under the name: how many one craft makes, when more than one.</summary>
        private static string AmountText(Recipe recipe) => recipe.m_amount > 1 ? "x" + recipe.m_amount : "";
    }
}
