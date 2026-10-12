using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook's parts, made from the game's own (<see cref="CasterUiCopies"/>) the first time a wand is in hand with
    /// the inventory open: a Spellbook button under the player's inventory, and a window beside it (shown and hidden with
    /// the inventory, which already frees the cursor): the crafting panel's background and title, the wand's two slots
    /// as hotbar slots, the known spells as recipe rows, and a text for the picked slot's spell.
    /// </summary>
    public static class SpellbookUi
    {
        private const float Width = 440f;
        private const float Height = 480f;
        private const float Side = 26f;
        private const float RowsTop = -214f;
        private const float RowHeight = 34f;

        public static RectTransform Window { get; private set; }
        public static Button OpenButton { get; private set; }
        public static TMP_Text Detail { get; private set; }
        public static readonly GameObject[] Slots = new GameObject[Loadout.Slots];
        public static readonly List<GameObject> Rows = new List<GameObject>();

        private static InventoryGui gui;

        /// <summary>Makes the parts when missing; false while the game's inventory screen or hotbar is not there to copy from.</summary>
        public static bool Ensure(Action toggle, Action<int> pickSlot)
        {
            if (Window != null)
                return true;
            gui = InventoryGui.instance;
            HotkeyBar hotbar = Hud.instance != null ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            if (gui == null || gui.m_player == null || gui.m_crafting == null || gui.m_craftButton == null
                || gui.m_recipeElementPrefab == null || hotbar == null || hotbar.m_elementPrefab == null)
                return false;
            MakeButton(toggle);
            MakeWindow(hotbar, pickSlot);
            return true;
        }

        /// <summary>One known spell's row, at its place in the list, its click handing the spell on.</summary>
        public static GameObject Row(int index, Spell spell, Action<Spell> pick)
        {
            Button row = CasterUiCopies.Button(gui.m_recipeElementPrefab.transform, Window, "EE_SpellRow_" + spell.Key, null, () => pick(spell));
            CasterUiCopies.Place((RectTransform)row.transform, Side, RowsTop - index * RowHeight, Width - 2f * Side, RowHeight - 2f);
            foreach (string part in new[] { "Durability", "QualityLevel", "selected" })
                row.transform.Find(part)?.gameObject.SetActive(false);
            Rows.Add(row.gameObject);
            return row.gameObject;
        }

        private static void MakeButton(Action toggle)
        {
            OpenButton = CasterUiCopies.Button(gui.m_craftButton, gui.m_player, "EE_SpellbookButton", CasterWords.Spellbook, toggle);
            CasterUiCopies.Place((RectTransform)OpenButton.transform, 0f, -(gui.m_player.rect.height + 6f), 170f, 40f);
            OpenButton.gameObject.SetActive(false);
        }

        private static void MakeWindow(HotkeyBar hotbar, Action<int> pickSlot)
        {
            Window = CasterUiCopies.Rect("EE_Spellbook", gui.m_player.parent);
            Window.anchorMin = Window.anchorMax = Window.pivot = new Vector2(0.5f, 0.5f);
            Window.anchoredPosition = new Vector2(560f, 60f);
            Window.sizeDelta = new Vector2(Width, Height);
            Backdrop();
            Label(CasterWords.Spellbook, -14f, 30f, TextAlignmentOptions.Center);
            Label(CasterWords.Prepared, -62f, 20f, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < Loadout.Slots; i++)
                Slots[i] = Slot(hotbar, i, pickSlot);
            Label(CasterWords.Known, -178f, 20f, TextAlignmentOptions.MidlineLeft);
            Detail = CasterUiCopies.Text(gui.m_recipeDecription != null ? gui.m_recipeDecription : gui.m_recipeName, Window, "Detail", 16f, TextAlignmentOptions.TopLeft);
            Detail.textWrappingMode = TextWrappingModes.Normal;
            CasterUiCopies.Place(Detail.rectTransform, Side, -(Height - 110f), Width - 2f * Side, 96f);
            Window.gameObject.SetActive(false);
        }

        private static void Backdrop()
        {
            Transform darken = gui.m_crafting.Find("Darken");
            if (darken != null)
                CasterUiCopies.Fill((RectTransform)CasterUiCopies.Copy(darken, Window, "Darken").transform, -50f);
            Transform background = gui.m_crafting.Find("Bkg");
            if (background != null)
                CasterUiCopies.Fill((RectTransform)CasterUiCopies.Copy(background, Window, "Bkg").transform, 0f);
        }

        private static void Label(string word, float y, float size, TextAlignmentOptions align)
        {
            TMP_Text source = gui.m_crafting.Find("topic")?.GetComponent<TMP_Text>() ?? gui.m_recipeName;
            TMP_Text text = CasterUiCopies.Text(source, Window, "Label", size, align);
            text.text = Core.Language.Localize(word);
            CasterUiCopies.Place(text.rectTransform, Side, y, Width - 2f * Side, size + 16f);
        }

        private static GameObject Slot(HotkeyBar hotbar, int index, Action<int> pickSlot)
        {
            Button slot = CasterUiCopies.Button(hotbar.m_elementPrefab.transform, Window, "EE_SpellSlot" + index, null, () => pickSlot(index));
            RectTransform rect = (RectTransform)slot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Side + 36f + index * hotbar.m_elementSpace, -126f);
            foreach (string part in new[] { "durability", "equiped", "queued", "amount" })
                slot.transform.Find(part)?.gameObject.SetActive(false);
            SpellBar.Label(slot.transform.Find("binding")?.GetComponent<TMP_Text>(), index);
            return slot.gameObject;
        }

        /// <summary>Removes the spell rows (the known spells changed).</summary>
        public static void ClearRows()
        {
            foreach (GameObject row in Rows)
                Object.Destroy(row);
            Rows.Clear();
        }
    }
}
