using System;
using System.Collections.Generic;
using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook's parts, made from the game's own (<see cref="CasterUiCopies"/>) the first time a spellbook is in hand with the inventory open: a Spellbook button under the player's inventory, and the window, which takes
    /// the crafting panel's place while it is open (<see cref="SpellbookCover"/> hides that panel): copies of the panel's
    /// darkening, background and title, the held book's name and page count under the title, a separator (the crafting
    /// tabs' own), then the pages (<see cref="SpellbookPages"/>) and the picked page (<see cref="SpellbookDetail"/>),
    /// placed by <see cref="SpellbookLayout"/>. The labels' words are put in
    /// again when the language changes (<see cref="Relabel"/>).
    /// </summary>
    public static class SpellbookUi
    {
        public static RectTransform Window { get; private set; }
        public static Button OpenButton { get; private set; }
        public static InventoryGui Gui { get; private set; }

        private static TMP_Text bookName;
        private static TMP_Text count;
        private static readonly List<KeyValuePair<TMP_Text, string>> labels = new List<KeyValuePair<TMP_Text, string>>();
        private static int labelled = -1;

        /// <summary>Makes the parts when missing; false while the game's inventory screen or hotbar is not there to copy from.</summary>
        public static bool Ensure(Action toggle, Action<Spell> pickPage, Action takeOut)
        {
            if (Window != null)
                return true;
            Gui = InventoryGui.instance;
            if (!Ready(Gui))
                return false;
            labels.Clear();
            labelled = -1;
            MakeButton(toggle);
            MakeWindow();
            SpellbookPages.Make(Window, SpellbookLayout.Pad, pickPage);
            SpellbookDetail.Make(Window, SpellbookLayout.Pad, takeOut);
            Window.gameObject.SetActive(false);
            return true;
        }

        /// <summary>The held book's name and its page count under the title; both empty without a book.</summary>
        public static void ShowHeader(ItemDrop.ItemData book, int pages)
        {
            bookName.text = book != null ? Localization.instance.Localize(book.m_shared.m_name) : "";
            count.text = book != null ? Lang.Format("ee_spellbook_pages", pages, BookPages.Room(book)) : "";
        }

        /// <summary>Gives a text a word now and again whenever the language changes.</summary>
        public static void Label(TMP_Text text, string key)
        {
            text.text = Lang.Get(key);
            labels.Add(new KeyValuePair<TMP_Text, string>(text, key));
        }

        /// <summary>Puts the labels' words in again when the language changed since they were set.</summary>
        public static void Relabel()
        {
            if (labelled == Lang.Version)
                return;
            labelled = Lang.Version;
            foreach (KeyValuePair<TMP_Text, string> label in labels)
            {
                if (label.Key != null)
                    label.Key.text = Lang.Get(label.Value);
            }
        }

        private static bool Ready(InventoryGui gui) =>
            gui != null && gui.m_player != null && gui.m_crafting != null && gui.m_craftButton != null
            && gui.m_recipeElementPrefab != null && gui.m_recipeName != null && gui.m_recipeDecription != null
            && gui.m_recipeIcon != null;

        private static void MakeButton(Action toggle)
        {
            OpenButton = CasterUiCopies.Button(Gui.m_craftButton, Gui.m_player, "EE_SpellbookButton", Lang.Get("ee_spellbook"), toggle);
            Label(OpenButton.GetComponentInChildren<TMP_Text>(true), "ee_spellbook");
            CasterUiCopies.Place((RectTransform)OpenButton.transform, 0f, -(Gui.m_player.rect.height + 6f), 170f, 40f);
            OpenButton.gameObject.SetActive(false);
        }

        /// <summary>The window beside the crafting panel (just above it among its siblings), with its rect, backdrop and header.</summary>
        private static void MakeWindow()
        {
            RectTransform crafting = Gui.m_crafting;
            Window = CasterUiCopies.Rect("EE_Spellbook", crafting.parent);
            Window.SetSiblingIndex(crafting.GetSiblingIndex() + 1);
            SpellbookLayout.Fit(Window, crafting);
            foreach (string part in new[] { "Darken", "Bkg" })
            {
                Transform source = crafting.Find(part);
                if (source != null)
                    CasterUiCopies.Copy(source, Window, part);
            }
            Title(crafting.Find("topic")?.GetComponent<TMP_Text>());
            bookName = Header(Gui.m_recipeName, SpellbookLayout.NameTop, SpellbookLayout.NameHeight, 24f);
            count = Header(Gui.m_recipeDecription, SpellbookLayout.CountTop, SpellbookLayout.CountHeight, 16f);
            Separator(crafting.Find("TabsButtons/TabBorder"));
        }

        /// <summary>A copy of the crafting panel's title in its own place (the item name's font across the top without it).</summary>
        private static void Title(TMP_Text topic)
        {
            TMP_Text title = CasterUiCopies.Text(topic ?? Gui.m_recipeName, Window, "Title", topic != null ? topic.fontSize : 30f,
                topic != null ? topic.alignment : TextAlignmentOptions.Center);
            if (topic == null)
                SpellbookLayout.Band(title.rectTransform, 16f, 34f);
            CasterUiCopies.Shrink(title, 0.6f, false);
            Label(title, "ee_spellbook");
        }

        private static TMP_Text Header(TMP_Text source, float top, float height, float size)
        {
            TMP_Text text = CasterUiCopies.Text(source, Window, "Header", size, TextAlignmentOptions.Center);
            SpellbookLayout.Band(text.rectTransform, top, height);
            CasterUiCopies.Shrink(text, 0.6f, false);
            return text;
        }

        /// <summary>A copy of the crafting tabs' separator line across the window under the header, at its own thickness.</summary>
        private static void Separator(Transform source)
        {
            if (source == null)
                return;
            var line = (RectTransform)CasterUiCopies.Copy(source, Window, "Separator").transform;
            float thickness = Mathf.Clamp(((RectTransform)source).rect.height, 2f, 12f);
            SpellbookLayout.Band(line, SpellbookLayout.SeparatorTop, thickness, SpellbookLayout.Pad + 8f);
        }
    }
}
