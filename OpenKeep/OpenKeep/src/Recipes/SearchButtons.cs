using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The two buttons at the search row's right end, copies of the game's quality - button: the favourites only star
    /// (orange while on; Shift + click asks to clear every favourite) and the view button (its icon shows the view;
    /// click for the next, Shift + click for the one before). The view is saved in the cfg, the favourites only state
    /// with the character.
    /// </summary>
    public static class SearchButtons
    {
        private const float Gap = 3f;
        private const float IconInset = 6f;

        private static Button only;
        private static Image onlyIcon;
        private static Image viewIcon;
        private static UITooltip viewTip;

        /// <summary>The width both buttons take at the row's end, gaps included.</summary>
        public static float Width => 2f * (30f + Gap);

        /// <summary>At the row's right end, anchored there, so they stay at the end however wide the row gets.</summary>
        public static void Create(InventoryGui gui, RectTransform row, float size)
        {
            only = null;
            Button template = gui != null ? gui.m_qualityLevelDown : null;
            if (template == null)
                return;
            only = PanelButton.Clone(template, row, "OpenKeep_RecipeOnly", Only);
            PlaceRight((RectTransform)only.transform, size + Gap, size);
            onlyIcon = PanelButton.Icon(only, RecipeStar.Sprite, IconInset);
            RecipeTips.Set(only.gameObject, RecipeWords.OnlyTopic, RecipeWords.OnlyTip);
            Button view = PanelButton.Clone(template, row, "OpenKeep_RecipeView", NextView);
            PlaceRight((RectTransform)view.transform, 0f, size);
            viewIcon = PanelButton.Icon(view, RecipeSprites.Of(RecipeListSettings.View.Value), IconInset);
            viewTip = RecipeTips.Set(view.gameObject, "", RecipeWords.ViewTip);
            Refresh();
        }

        /// <summary>After every panel update while the row shows: the star's state and the view's icon and name.</summary>
        public static void Refresh()
        {
            if (only == null)
                return;
            only.gameObject.SetActive(RecipeFavourites.Enabled);
            onlyIcon.sprite = RecipeStar.Sprite;
            onlyIcon.color = RecipeFavourites.OnlyFavourites ? RecipeStar.On : RecipeStar.Off;
            RecipeView view = RecipeListSettings.View.Value;
            viewIcon.sprite = RecipeSprites.Of(view);
            if (viewTip != null)
                viewTip.m_topic = RecipeWords.View(view);
        }

        /// <summary>Top right anchored, <paramref name="fromRight"/> in from the row's right edge.</summary>
        private static void PlaceRight(RectTransform rect, float fromRight, float size)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-fromRight, 0f);
            rect.sizeDelta = new Vector2(size, size);
            rect.localScale = Vector3.one;
        }

        private static void Only()
        {
            if (PanelButton.ShiftHeld)
                RecipeFavourites.AskClear();
            else
                RecipeFavourites.ToggleOnly();
        }

        private static void NextView() => StepView(PanelButton.ShiftHeld ? -1 : 1);

        /// <summary>The cfg is written; the setting's change rebuilds the list.</summary>
        public static void StepView(int direction)
        {
            int count = System.Enum.GetValues(typeof(RecipeView)).Length;
            int next = ((int)RecipeListSettings.View.Value + direction + count) % count;
            RecipeListSettings.View.Value = (RecipeView)next;
        }
    }
}
