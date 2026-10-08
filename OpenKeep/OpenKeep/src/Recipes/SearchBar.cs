using GUIFramework;
using OpenKeep.Core;
using OpenKeep.Salvage;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The search row above the recipe list: a copy of the game's build menu search field (its look, Ctrl + Backspace,
    /// and Steam's on-screen keyboard in Big Picture and on the Steam Deck), then the favourites only star and the view
    /// button (<see cref="SearchButtons"/>), and under it the category row (<see cref="CategoryBar"/>). Each shows on the
    /// Craft and Upgrade tabs while its setting is on, and the list gives up their height meanwhile; on the Salvage tab
    /// the list is the game's height again. The row follows the
    /// list's current size, so a mod that makes the panel larger (PackPanel) widens the field too; a list another mod
    /// has set again since this shrank it is taken as its new full size. Built at the first
    /// panel update after the HUD exists, since the field is copied from the HUD's build menu. Typing rebuilds the list
    /// a moment after the last key, scrolled to the top; Escape clears the text, Enter keeps it.
    /// </summary>
    public static class SearchBar
    {
        private const float RowHeight = 30f;
        private const float RowGap = 4f;
        private const float Debounce = 0.12f;
        private const float FontSize = 16f;

        private static RectTransform row;
        private static GuiInputField field;
        private static RectTransform list;
        private static Vector2 setPosition;
        private static Vector2 setSize;
        private static Vector2 taken;
        private static bool shrunk;
        private static bool failed;
        private static RecipeQuery query = RecipeQuery.All;
        private static float rebuildAt = -1f;

        public static RecipeQuery Query => Shown ? query : RecipeQuery.All;

        public static bool Shown => row != null && row.gameObject.activeSelf;

        public static bool Focused => field != null && field.isFocused;

        /// <summary>Before and after every panel update: built once, shown or hidden for the tab and the setting, the list sized to match.</summary>
        public static void Prepare(InventoryGui gui)
        {
            if (row == null && !failed)
                Build(gui);
            if (row == null)
                return;
            bool show = RecipeListSettings.Search.Value && !SalvageTab.Active;
            bool categories = RecipeCategories.Enabled && !SalvageTab.Active;
            if (row.gameObject.activeSelf != show)
                row.gameObject.SetActive(show);
            CategoryBar.Show(categories);
            Shrink(gui, show, categories);
            if (show)
                SearchButtons.Refresh();
            if (categories)
                CategoryBar.Refresh();
        }

        /// <summary>
        /// At InventoryGui.Awake: a new inventory (a world loaded after a logout) has a new list, so everything about
        /// the old one is forgotten and the row is built again at its first update.
        /// </summary>
        public static void Reset()
        {
            row = null;
            field = null;
            list = null;
            shrunk = false;
            failed = false;
            query = RecipeQuery.All;
            rebuildAt = -1f;
            CategoryBar.Reset();
            RecipeCategories.Reset();
        }

        /// <summary>Puts the cursor into the field, or opens Steam's keyboard where the game would.</summary>
        public static void Focus()
        {
            if (Shown && field != null && field.interactable)
                field.OpenKeyboard();
        }

        /// <summary>Every frame the inventory shows: the list follows the text once typing pauses.</summary>
        public static void Tick(InventoryGui gui)
        {
            if (rebuildAt < 0f || Time.unscaledTime < rebuildAt || field == null)
                return;
            rebuildAt = -1f;
            query = RecipeQuery.Parse(field.text);
            ScrollRect scroll = gui.m_recipeListRoot.GetComponentInParent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
            RecipeFavourites.Rebuild();
        }

        /// <summary>The inventory closed: the text goes when Clear Search On Close says so.</summary>
        public static void Closed()
        {
            if (field == null || !RecipeListSettings.ClearSearchOnClose.Value || field.text.Length == 0)
                return;
            field.SetTextWithoutNotify("");
            query = RecipeQuery.All;
            rebuildAt = -1f;
        }

        private static void Build(InventoryGui gui)
        {
            TMP_InputField template = Template();
            if (template == null)
            {
                failed = Hud.instance != null;
                if (failed)
                    Plugin.Log.LogWarning("OpenKeep: the build menu's search field was not found; the recipe search row is off");
                return;
            }
            list = RecipeListPanel(gui);
            if (list == null)
            {
                failed = true;
                Plugin.Log.LogWarning("OpenKeep: the crafting panel's recipe list was not found; the recipe search row is off");
                return;
            }
            MakeRow();
        }

        private static void MakeRow()
        {
            GameObject go = new GameObject("OpenKeep_RecipeSearch", typeof(RectTransform));
            go.layer = list.gameObject.layer;
            go.transform.SetParent(list.parent, false);
            go.transform.SetSiblingIndex(list.GetSiblingIndex() + 1);
            row = (RectTransform)go.transform;
            PanelButton.PlaceTopLeft(row, list.anchoredPosition.x, list.anchoredPosition.y, new Vector2(list.sizeDelta.x, RowHeight));
            field = MakeField(Template());
            SearchButtons.Create(InventoryGui.instance, row, RowHeight);
            go.SetActive(false);
            CategoryBar.Create(InventoryGui.instance, list.parent, row.GetSiblingIndex() + 1);
        }

        /// <summary>Stretched across the row, less the two buttons at its right end.</summary>
        private static GuiInputField MakeField(TMP_InputField template)
        {
            GameObject go = Object.Instantiate(template.gameObject, row, false);
            go.name = "OpenKeep_RecipeSearchField";
            go.SetActive(true);
            Strip(go);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(-SearchButtons.Width, 0f);
            GuiInputField input = go.GetComponent<GuiInputField>();
            Configure(input);
            Texts(input);
            RecipeTips.Set(go, RecipeWords.SearchTopic, RecipeWords.SearchTip);
            return input;
        }

        /// <summary>The copy loses the build menu's layout entry and its key hint (the F and stick glyphs are the build menu's keys).</summary>
        private static void Strip(GameObject go)
        {
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout != null)
                Object.DestroyImmediate(layout);
            foreach (UIInputHint hint in go.GetComponentsInChildren<UIInputHint>(true))
                Object.DestroyImmediate(hint.gameObject);
        }

        /// <summary>Fresh events (the build menu's listeners stay behind), then OpenKeep's own.</summary>
        private static void Configure(GuiInputField input)
        {
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.onSubmit = new TMP_InputField.SubmitEvent();
            input.onSelect = new TMP_InputField.SelectionEvent();
            input.onDeselect = new TMP_InputField.SelectionEvent();
            input.OnInputSubmit = new OnInputSubmitEvent();
            input.characterLimit = 40;
            input.VirtualKeyboardTitle = RecipeWords.SearchTopic;
            input.SetTextWithoutNotify("");
            input.onValueChanged.AddListener(_ => rebuildAt = Time.unscaledTime + Debounce);
            input.onEndEdit.AddListener(_ => Ended(input));
        }

        /// <summary>Escape (a cancelled edit) clears the search; either way the field lets go so the hotkeys work again.</summary>
        private static void Ended(GuiInputField input)
        {
            if (input.wasCanceled && input.text.Length > 0)
                input.text = "";
            EventSystem system = EventSystem.current;
            if (system != null && !system.alreadySelecting && system.currentSelectedGameObject == input.gameObject)
                system.SetSelectedGameObject(null);
        }

        /// <summary>A lower field than the build menu's: smaller text, a wider text area, OpenKeep's placeholder.</summary>
        private static void Texts(GuiInputField input)
        {
            if (input.textViewport != null)
            {
                input.textViewport.offsetMin = new Vector2(8f, 2f);
                input.textViewport.offsetMax = new Vector2(-8f, -2f);
            }
            if (input.textComponent != null)
                input.textComponent.fontSize = FontSize;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.fontSize = FontSize;
                placeholder.text = Language.Localize(RecipeWords.Search);
            }
        }

        /// <summary>
        /// The list without the shown rows' height, from its full size: what this left it at plus what it took, or, when
        /// another mod has set it since (PackPanel's larger panel), what it is now. The rows sit on its top, the search
        /// row first.
        /// </summary>
        private static void Shrink(InventoryGui gui, bool search, bool categories)
        {
            Vector2 position = list.anchoredPosition;
            Vector2 size = list.sizeDelta;
            bool ours = shrunk && position == setPosition && size == setSize;
            if (ours)
            {
                position += taken;
                size += taken;
            }
            float y = position.y;
            PanelButton.PlaceTopLeft(row, position.x, y, new Vector2(size.x, RowHeight));
            if (search)
                y -= RowHeight + RowGap;
            CategoryBar.Place(position.x, y, size.x);
            if (categories && CategoryBar.Height > 0f)
                y -= CategoryBar.Height + RowGap;
            taken = new Vector2(0f, position.y - y);
            shrunk = taken.y > 0f;
            if (shrunk)
                Write(gui, setPosition = position - taken, setSize = size - taken);
            else if (ours)
                Write(gui, position, size);
        }

        private static void Write(InventoryGui gui, Vector2 position, Vector2 size)
        {
            list.anchoredPosition = position;
            list.sizeDelta = size;
            gui.m_recipeListBaseSize = size.y;
        }

        private static TMP_InputField Template()
        {
            BuildUi build = Hud.instance != null ? Hud.instance.m_buildUi : null;
            return build != null ? build.m_searchField : null;
        }

        /// <summary>The list's panel (RecipeList: the scroll view and its bar), a direct child of the crafting panel, top left anchored.</summary>
        private static RectTransform RecipeListPanel(InventoryGui gui)
        {
            Transform view = gui.m_recipeListRoot != null ? gui.m_recipeListRoot.parent : null;
            RectTransform panel = view != null ? view.parent as RectTransform : null;
            if (panel == null || panel.parent != gui.m_crafting)
                return null;
            bool topLeft = panel.anchorMin == new Vector2(0f, 1f) && panel.anchorMax == panel.anchorMin && panel.pivot == panel.anchorMin;
            return topLeft ? panel : null;
        }
    }
}
