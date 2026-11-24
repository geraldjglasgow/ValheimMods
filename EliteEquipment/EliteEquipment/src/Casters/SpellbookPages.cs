using System;
using System.Collections.Generic;
using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook's left column: the held book's pages as copies of the game's recipe-list row, one under another at
    /// the recipe list's own spacing (the spell's icon, its name shrunk to fit, its eitr cost at the right), the picked
    /// page lit by the row's own "selected" frame; a line in the column instead when there is no book or the book is empty.
    /// Rows are made only when the pages change (<see cref="Clear"/>), and otherwise only given their words and frame.
    /// </summary>
    public static class SpellbookPages
    {
        private const float Inset = 4f;
        private const float NameLeft = 37f;
        private const float CostWidth = 62f;
        private const string CostName = "EE_Cost";
        private static readonly Color Grey = new Color(0.66f, 0.66f, 0.66f, 1f);

        private static RectTransform column;
        private static TMP_Text note;
        private static Action<Spell> pick;
        private static readonly List<GameObject> rows = new List<GameObject>();

        /// <summary>Makes the column and its note, from the header down to bottom units above the window's bottom edge.</summary>
        public static void Make(RectTransform window, float bottom, Action<Spell> pickPage)
        {
            pick = pickPage;
            rows.Clear();
            column = SpellbookLayout.Column(window, "EE_SpellbookPages", 0f, SpellbookLayout.Split, bottom);
            note = CasterUiCopies.Text(SpellbookUi.Gui.m_recipeDecription, column, "Note", 18f, TextAlignmentOptions.Center);
            CasterUiCopies.Stretch(note.rectTransform, Vector2.zero, Vector2.one, 14f, 14f, 14f, 14f);
            CasterUiCopies.Shrink(note, 0.6f, true);
        }

        /// <summary>The pages, the picked one lit; the note's words instead of rows when given.</summary>
        public static void Show(List<Spell> pages, Spell picked, Player player, string empty)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                GameObject row = i < rows.Count ? rows[i] : Row(i, pages[i]);
                Paint(row.transform, pages[i], pages[i] == picked, player);
            }
            note.gameObject.SetActive(empty != null);
            note.text = empty ?? "";
        }

        /// <summary>Removes the rows (the book or its pages changed); the next <see cref="Show"/> makes them again.</summary>
        public static void Clear()
        {
            foreach (GameObject row in rows)
            {
                if (row != null)
                    Object.Destroy(row);
            }
            rows.Clear();
        }

        private static GameObject Row(int index, Spell spell)
        {
            InventoryGui gui = SpellbookUi.Gui;
            Button row = CasterUiCopies.Button(gui.m_recipeElementPrefab.transform, column, "EE_SpellRow_" + spell.Key, null, () => pick(spell));
            float height = ((RectTransform)gui.m_recipeElementPrefab.transform).sizeDelta.y;
            SpellbookLayout.Band((RectTransform)row.transform, Inset + index * gui.m_recipeListSpace, height, Inset);
            foreach (string part in new[] { "Durability", "QualityLevel" })
                row.transform.Find(part)?.gameObject.SetActive(false);
            Fit(row.transform);
            rows.Add(row.gameObject);
            return row.gameObject;
        }

        /// <summary>The name across the row up to the cost, and the cost (a copy of the name's text) at its right end.</summary>
        private static void Fit(Transform row)
        {
            TMP_Text name = row.Find("name")?.GetComponent<TMP_Text>();
            if (name == null)
                return;
            CasterUiCopies.Stretch(name.rectTransform, Vector2.zero, Vector2.one, NameLeft, 0f, CostWidth, 0f);
            CasterUiCopies.Shrink(name, 0.6f, false);
            TMP_Text cost = CasterUiCopies.Text(name, row, CostName, name.fontSize * 0.8f, TextAlignmentOptions.MidlineRight);
            CasterUiCopies.Stretch(cost.rectTransform, new Vector2(1f, 0f), Vector2.one, -CostWidth, 0f, 6f, 0f);
            CasterUiCopies.Shrink(cost, 0.6f, false);
            cost.color = Grey;
        }

        private static void Paint(Transform row, Spell spell, bool picked, Player player)
        {
            Image icon = row.Find("icon")?.GetComponent<Image>();
            if (icon != null)
                icon.sprite = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            TMP_Text name = row.Find("name")?.GetComponent<TMP_Text>();
            if (name != null)
                (name.text, name.color) = (Lang.Get(spell.Token), Color.white);
            TMP_Text cost = row.Find(CostName)?.GetComponent<TMP_Text>();
            if (cost != null)
                cost.text = SpellbookDetail.Cost(player, spell);
            row.Find("selected")?.gameObject.SetActive(picked);
        }
    }
}
