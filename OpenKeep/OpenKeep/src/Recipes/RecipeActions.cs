using OpenKeep.Core;
using OpenKeep.Salvage;
using OpenKeep.Tracker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Two buttons under the selected recipe's name, copies of the game's Style button beside which they sit: Track
    /// (Untrack once tracked) and a favourite star (orange while a favourite). They start where the Style button
    /// starts when the recipe has no styles, else right of it. Shown on the Craft and Upgrade tabs while a recipe is
    /// selected and the Recipe Tracker or Favourites is on.
    /// </summary>
    public static class RecipeActions
    {
        private const float Gap = 6f;
        private const float StarInset = 4f;

        private static RectTransform style;
        private static Button track;
        private static TMP_Text trackLabel;
        private static string trackWord;
        private static string trackText = "";
        private static Button star;
        private static Image starIcon;

        /// <summary>At InventoryGui.Awake; nothing when the game's Style button is missing.</summary>
        public static void Create(InventoryGui gui)
        {
            track = null;
            Button template = gui.m_variantButton;
            if (template == null)
                return;
            style = (RectTransform)template.transform;
            track = PanelButton.Clone(template, style.parent, "OpenKeep_TrackButton", () => Track(gui));
            trackLabel = PanelButton.Label(track);
            trackWord = null;
            star = PanelButton.Clone(template, style.parent, "OpenKeep_FavouriteButton", () => Favourite(gui));
            ((RectTransform)star.transform).sizeDelta = new Vector2(style.sizeDelta.y, style.sizeDelta.y);
            starIcon = PanelButton.Icon(star, RecipeStar.Sprite, StarInset);
            RecipeTips.Set(star.gameObject, RecipeWords.FavouriteTopic, RecipeWords.FavouriteTip);
            track.gameObject.SetActive(false);
            star.gameObject.SetActive(false);
        }

        /// <summary>After every UpdateRecipe: shown for the selected recipe, labelled and placed.</summary>
        public static void Refresh(InventoryGui gui)
        {
            if (track == null)
                return;
            Recipe recipe = gui.m_selectedRecipe.Recipe;
            bool shown = recipe != null && !SalvageTab.Active;
            bool showTrack = shown && TrackerSettings.Enabled.Value;
            bool showStar = shown && RecipeFavourites.Enabled;
            Show(track, showTrack);
            Show(star, showStar);
            if (!shown)
                return;
            float x = Left();
            if (showTrack)
                x = PlaceTrack(recipe, RecipeFilter.Quality(gui.m_selectedRecipe.ItemData), x);
            if (showStar)
                PlaceStar(recipe, x);
        }

        /// <summary>
        /// The label is localized only when it changes between Track and Untrack (this runs every frame), and written
        /// again whenever something else (the game's relocalizing) changed it.
        /// </summary>
        private static float PlaceTrack(Recipe recipe, int quality, float x)
        {
            string word = TrackerList.IsTracked(recipe, quality) ? TrackerWords.Untrack : TrackerWords.Track;
            if (word != trackWord)
            {
                trackWord = word;
                trackText = Language.Localize(word);
            }
            if (trackLabel != null && trackLabel.text != trackText)
                trackLabel.text = trackText;
            return Place((RectTransform)track.transform, x);
        }

        private static void PlaceStar(Recipe recipe, float x)
        {
            starIcon.sprite = RecipeStar.Sprite;
            starIcon.color = RecipeFavourites.Is(recipe) ? RecipeStar.On : RecipeStar.Off;
            Place((RectTransform)star.transform, x);
        }

        /// <summary>The Style button's left edge, or the place right of it while the game shows it.</summary>
        private static float Left()
        {
            float left = style.anchoredPosition.x - style.rect.width * style.pivot.x;
            return style.gameObject.activeSelf ? left + style.rect.width + Gap : left;
        }

        /// <summary>At x (its left edge), level with the Style button; returns where the next one starts.</summary>
        private static float Place(RectTransform rect, float x)
        {
            float width = rect.rect.width;
            rect.anchorMin = style.anchorMin;
            rect.anchorMax = style.anchorMax;
            rect.anchoredPosition = new Vector2(x + width * rect.pivot.x, style.anchoredPosition.y);
            return x + width + Gap;
        }

        private static void Show(Button button, bool show)
        {
            if (button.gameObject.activeSelf != show)
                button.gameObject.SetActive(show);
        }

        private static void Track(InventoryGui gui)
        {
            Recipe recipe = gui.m_selectedRecipe.Recipe;
            if (recipe != null)
                TrackerList.Toggle(recipe, RecipeFilter.Quality(gui.m_selectedRecipe.ItemData));
        }

        private static void Favourite(InventoryGui gui)
        {
            Recipe recipe = gui.m_selectedRecipe.Recipe;
            if (recipe != null)
                RecipeFavourites.Toggle(recipe);
        }
    }
}
