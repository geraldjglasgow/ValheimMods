using GUIFramework;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The search field of Mímir's Chest: a copy of the game's build menu search field (its look, Ctrl + Backspace and
    /// Steam's on-screen keyboard), placed at the left of the toolbar row the panel grows while a Mímir's Chest is the
    /// open container (<see cref="MimirLayout"/>), with the quick filters beside it (<see cref="MimirFilterBar"/>). Typing dims every stack that does not match a moment after the last key
    /// (<see cref="MimirSearchPaint"/>) and brings the first match into view; Escape clears the text, Enter keeps it.
    /// The text is cleared whenever another chest opens. Made at the first frame a Mímir's Chest shows, since the field is
    /// copied from the HUD's build menu; a new inventory screen (a world loaded after a logout) gets a new one.
    /// </summary>
    public static class MimirSearch
    {
        private const float Height = 30f;
        private const float Left = MimirLayout.Margin;
        private static float Width => MimirLayout.RowRight - Left - MimirFilterBar.Width - 8f;
        private const float FontSize = 16f;
        private const float Debounce = 0.12f;

        private static GuiInputField field;
        private static RectTransform panel;
        private static Container shownFor;
        private static float applyAt = -1f;
        private static int scrollTopFrames;

        public static MimirQuery Query { get; private set; } = MimirQuery.All;

        /// <summary>The picked quick filter (<see cref="MimirFilterBar"/>); All when none is.</summary>
        public static MimirFilter Filter { get; private set; } = MimirFilter.All;

        /// <summary>Only starred stacks (the star button), combined with the category.</summary>
        public static bool Starred { get; private set; }

        /// <summary>Whether the open chest shows only some of its stacks: a search, a quick filter or the star.</summary>
        public static bool Narrowed => !Query.Empty || Filter != MimirFilter.All || Starred;

        /// <summary>Whether a stack passes the search, the quick filter and the star.</summary>
        public static bool Matches(ItemDrop.ItemData item) =>
            Query.Matches(item) && MimirFilters.Matches(Filter, item) && (!Starred || MimirFilters.IsStarred(item));

        public static void ToggleStarred()
        {
            Starred = !Starred;
            Version++;
            Repack();
        }

        /// <summary>Searches the chest for this text, as if typed (find in chest: Ctrl + right click on an item).</summary>
        public static void Search(string text)
        {
            if (field == null)
                return;
            field.text = text ?? "";
            applyAt = -1f;
            Set(new MimirQuery(field.text));
        }

        /// <summary>A quick filter clicked: picks it, or shows everything again when it was already picked.</summary>
        public static void Pick(MimirFilter filter)
        {
            Filter = Filter == filter ? MimirFilter.All : filter;
            Version++;
            Repack();
        }

        /// <summary>The open chest packed to the top in its order, the matches first (<see cref="MimirPack"/>).</summary>
        public static void Repack()
        {
            if (shownFor == null || shownFor.GetInventory()?.m_name != MimirPrefab.ContainerName)
                return;
            System.Func<ItemDrop.ItemData, bool> first = Narrowed ? Matches : (System.Func<ItemDrop.ItemData, bool>)null;
            MimirPack.Pack(shownFor, first, MimirSortMode.Comparer(MimirSortMode.Of(shownFor)));
        }

        /// <summary>Changes whenever the query does, so the paint knows to look at the items again.</summary>
        public static int Version { get; private set; }

        public static bool Focused => field != null && field.isFocused;

        /// <summary>Every frame the container grid draws: the field follows the open container; true while it is a Mímir's Chest.</summary>
        public static bool Sync(InventoryGui gui)
        {
            Container open = gui.m_currentContainer;
            bool mimir = open != null && open.GetInventory() != null && open.GetInventory().m_name == MimirPrefab.ContainerName;
            if (mimir && (field == null || panel != gui.m_container))
                Build(gui);
            MimirLayout.Apply(gui, mimir && field != null);
            if (field != null && field.gameObject.activeSelf != mimir)
                field.gameObject.SetActive(mimir);
            MimirFilterBar.SetActive(mimir);
            MimirSortButtons.SetActive(mimir);
            if (open != shownFor)
                Reset(open);
            Apply();
            if (mimir)
                Keep(gui, open.GetInventory());
            return mimir && field != null;
        }

        /// <summary>The filter buttons' state, and the grid at its top for the first frames after the chest opened.</summary>
        private static void Keep(InventoryGui gui, Inventory inventory)
        {
            MimirFilterBar.Show(inventory, Filter, Starred);
            MimirSortButtons.Show(shownFor);
            if (scrollTopFrames <= 0 || gui.m_containerGrid == null || gui.m_containerGrid.m_scrollbar == null)
                return;
            scrollTopFrames--;
            gui.m_containerGrid.m_scrollbar.value = 1f;
        }

        private static void Reset(Container open)
        {
            shownFor = open;
            Starred = false;
            scrollTopFrames = 3;
            if (field != null && field.text.Length > 0)
                field.text = "";
            applyAt = -1f;
            Filter = MimirFilter.All;
            Set(MimirQuery.All);
        }

        private static void Apply()
        {
            if (applyAt < 0f || Time.unscaledTime < applyAt || field == null)
                return;
            applyAt = -1f;
            Set(new MimirQuery(field.text));
        }

        private static void Set(MimirQuery query)
        {
            Query = query;
            Version++;
            Repack();
        }

        private static void Build(InventoryGui gui)
        {
            BuildUi build = Hud.instance != null ? Hud.instance.m_buildUi : null;
            TMP_InputField template = build != null ? build.m_searchField : null;
            if (template == null || gui.m_container == null)
                return;
            panel = gui.m_container;
            GameObject go = Object.Instantiate(template.gameObject, panel, false);
            go.name = "OpenKeep_MimirSearch";
            Strip(go);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            float top = -MimirLayout.TitleRow - (MimirLayout.Strip - Height) / 2f;
            rect.anchoredPosition = new Vector2(Left, top);
            rect.sizeDelta = new Vector2(Width, Height);
            field = go.GetComponent<GuiInputField>();
            Configure(field);
            Texts(field);
            MimirClear.Add(field);
            float barTop = -MimirLayout.TitleRow - (MimirLayout.Strip - MimirFilterBar.Size) / 2f;
            MimirFilterBar.Build(gui, panel, new Vector2(MimirLayout.RowRight - MimirFilterBar.Width, barTop));
            MimirSortButtons.Build(gui, panel);
        }

        /// <summary>The copy loses the build menu's layout entry and its key hint.</summary>
        private static void Strip(GameObject go)
        {
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout != null)
                Object.DestroyImmediate(layout);
            foreach (UIInputHint hint in go.GetComponentsInChildren<UIInputHint>(true))
                Object.DestroyImmediate(hint.gameObject);
        }

        /// <summary>Fresh events (the build menu's listeners stay behind), then the chest's own.</summary>
        private static void Configure(GuiInputField input)
        {
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.onSubmit = new TMP_InputField.SubmitEvent();
            input.onSelect = new TMP_InputField.SelectionEvent();
            input.onDeselect = new TMP_InputField.SelectionEvent();
            input.OnInputSubmit = new OnInputSubmitEvent();
            input.characterLimit = 40;
            input.VirtualKeyboardTitle = Language.Localize("$ok_mimir_search");
            input.SetTextWithoutNotify("");
            input.onValueChanged.AddListener(_ => applyAt = Time.unscaledTime + Debounce);
            input.onEndEdit.AddListener(_ => Ended(input));
        }

        /// <summary>Escape clears the search; either way the field lets go so the hotkeys work again.</summary>
        private static void Ended(GuiInputField input)
        {
            if (input.wasCanceled && input.text.Length > 0)
                input.text = "";
            EventSystem system = EventSystem.current;
            if (system != null && !system.alreadySelecting && system.currentSelectedGameObject == input.gameObject)
                system.SetSelectedGameObject(null);
        }

        private static void Texts(GuiInputField input)
        {
            if (input.textViewport != null)
            {
                input.textViewport.offsetMin = new Vector2(8f, 2f);
                input.textViewport.offsetMax = new Vector2(-MimirClear.Size - 4f, -2f);
            }
            if (input.textComponent != null)
                input.textComponent.fontSize = FontSize;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.fontSize = FontSize;
                placeholder.text = Language.Localize("$ok_mimir_search");
            }
        }
    }
}
