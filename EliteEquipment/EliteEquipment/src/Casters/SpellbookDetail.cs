using System;
using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook's right column, laid out as the crafting panel shows an item: the picked page's spell with a large
    /// icon (a copy of the recipe icon), its name in the item name's font beside it, a line under the name with its kind
    /// and eitr cost, its description wrapped inside the column (shrunk when long), and a Take out button (a copy of the
    /// craft button) along the column's bottom. All hidden when there is no page to show.
    /// </summary>
    public static class SpellbookDetail
    {
        private const float Inset = 12f;
        private const float IconSize = 64f;
        private const float TextLeft = Inset + IconSize + 12f;
        private const float ButtonHeight = 46f;
        private static readonly Color Grey = new Color(0.66f, 0.66f, 0.66f, 1f);

        private static RectTransform column;
        private static Image icon;
        private static TMP_Text name;
        private static TMP_Text kind;
        private static TMP_Text description;
        private static Button takeOut;

        /// <summary>Makes the column and its parts, from the header down to bottom units above the window's bottom edge.</summary>
        public static void Make(RectTransform window, float bottom, Action takeOutClick)
        {
            InventoryGui gui = SpellbookUi.Gui;
            column = SpellbookLayout.Column(window, "EE_SpellbookDetail", SpellbookLayout.Split, 1f, bottom);
            icon = CasterUiCopies.Copy(gui.m_recipeIcon, column, "Icon").GetComponent<Image>();
            CasterUiCopies.Place(icon.rectTransform, Inset, -Inset, IconSize, IconSize);
            icon.preserveAspect = true;
            name = Line(gui.m_recipeName, "Name", 10f, 36f, 28f);
            kind = Line(gui.m_recipeDecription, "Kind", 48f, 26f, 18f);
            kind.color = Grey;
            description = CasterUiCopies.Text(gui.m_recipeDecription, column, "Description", 18f, TextAlignmentOptions.TopLeft);
            float above = Inset + ButtonHeight + 10f;
            CasterUiCopies.Stretch(description.rectTransform, Vector2.zero, Vector2.one, Inset + 2f, above, Inset + 2f, Inset + IconSize + 14f);
            CasterUiCopies.Shrink(description, 0.65f, true);
            takeOut = TakeOutButton(takeOutClick);
        }

        /// <summary>Shows a page's spell, or hides every part for none.</summary>
        public static void Show(Spell spell, Player player)
        {
            bool any = spell != null;
            icon.gameObject.SetActive(any);
            name.gameObject.SetActive(any);
            kind.gameObject.SetActive(any);
            description.gameObject.SetActive(any);
            takeOut.gameObject.SetActive(any);
            if (!any)
                return;
            icon.sprite = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            name.text = Lang.Get(spell.Token);
            kind.text = Lang.Get(spell.KindWord)
                + "    <color=orange>" + Cost(player, spell) + "</color>";
            description.text = Lang.Get(spell.DescriptionToken);
        }

        /// <summary>A spell's eitr cost for this player, in words ("8 eitr").</summary>
        public static string Cost(Player player, Spell spell) =>
            Lang.Format("ee_spellbook_cost", Mathf.CeilToInt(SpellCost.Of(player, spell)));

        /// <summary>A line of text right of the icon, top units under the column's top, that tall, shrunk to fit on one line.</summary>
        private static TMP_Text Line(TMP_Text source, string label, float top, float height, float size)
        {
            TMP_Text text = CasterUiCopies.Text(source, column, label, size, TextAlignmentOptions.MidlineLeft);
            CasterUiCopies.Stretch(text.rectTransform, new Vector2(0f, 1f), Vector2.one, TextLeft, -(top + height), Inset, top);
            CasterUiCopies.Shrink(text, 0.55f, false);
            return text;
        }

        private static Button TakeOutButton(Action click)
        {
            Button button = CasterUiCopies.Button(SpellbookUi.Gui.m_craftButton, column, "EE_TakeOut", Lang.Get("ee_spellbook_takeout"), click);
            var rect = (RectTransform)button.transform;
            CasterUiCopies.Stretch(rect, new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), 0f, Inset, 0f, -(Inset + ButtonHeight));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                SpellbookUi.Label(label, "ee_spellbook_takeout");
                CasterUiCopies.Shrink(label, 0.45f, false);
            }
            return button;
        }
    }
}
