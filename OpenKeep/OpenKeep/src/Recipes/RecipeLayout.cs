using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Places the rows the game built for the chosen view. List keeps the game's 30 unit rows; CompactList packs rows
    /// 22 apart; a grid turns every row into a square tile (<see cref="RecipeTile"/>), as many to a row as the view
    /// says, sized to the list's width. The list's scroll height follows, never below the game's own.
    /// </summary>
    public static class RecipeLayout
    {
        private const float CompactStep = 22f;
        private const float FallbackWidth = 187f;

        public static int Columns => ColumnsOf(RecipeListSettings.View.Value);

        public static bool IsGrid => Columns > 1;

        public static int ColumnsOf(RecipeView view)
        {
            switch (view)
            {
                case RecipeView.SmallGrid: return 5;
                case RecipeView.MediumGrid: return 4;
                case RecipeView.LargeGrid: return 3;
                default: return 1;
            }
        }

        public static void Arrange(InventoryGui gui)
        {
            RecipeView view = RecipeListSettings.View.Value;
            int columns = ColumnsOf(view);
            float step = Step(gui, view, columns);
            List<InventoryGui.RecipeDataPair> list = gui.m_availableRecipes;
            for (int i = 0; i < list.Count; i++)
                Place(list[i], i, view, columns, step);
            int rows = (list.Count + columns - 1) / columns;
            float height = Mathf.Max(gui.m_recipeListBaseSize, rows * step);
            gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>The distance from one row (or tile) to the next.</summary>
        private static float Step(InventoryGui gui, RecipeView view, int columns)
        {
            if (columns > 1)
            {
                float width = gui.m_recipeListRoot.rect.width;
                return Mathf.Floor((width > 1f ? width : FallbackWidth) / columns);
            }
            return view == RecipeView.CompactList ? CompactStep : gui.m_recipeListSpace;
        }

        private static void Place(InventoryGui.RecipeDataPair pair, int index, RecipeView view, int columns, float step)
        {
            if (pair.InterfaceElement == null)
                return;
            RectTransform rect = (RectTransform)pair.InterfaceElement.transform;
            if (columns > 1)
            {
                rect.anchoredPosition = new Vector2(index % columns * step, -(index / columns) * step);
                RecipeTile.Shape(pair, step);
                return;
            }
            rect.anchoredPosition = new Vector2(0f, -index * step);
            if (view == RecipeView.CompactList)
                RecipeTile.Compact(rect, step);
        }
    }
}
