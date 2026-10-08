using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

namespace GrindstoneSkills
{
    /// <summary>
    /// The sort dropdown above the skills list: "Sort by" and the orders of <see cref="SkillOrder"/> (Default, Name,
    /// Level). The dropdown is a copy of one of the game's own settings dropdowns (Radial Size, from the settings window
    /// the in-game menu keeps as a prefab), so it looks and sounds like the game's; the copy loses the settings window's
    /// tooltip and handlers, and shrinks to the height of the "Sort by" line, its caption in the same size of text. It
    /// shows the order chosen last; choosing one saves it and lays the list out again. Measured from the game's layout
    /// (2026-10-08): the settings dropdown is 120 x 38, its caption a "Label" inside it (10 in from every edge, sized to
    /// fit), and the setting's name a second "Label", 20 high in 14 point text, right-aligned 16 left of it. Local UI
    /// only; nothing is sent.
    /// </summary>
    internal static class SortBar
    {
        /// <summary>The room the bar takes above the list: the dropdown's height and a gap.</summary>
        public const float Height = 28f;

        private const float LabelRoom = 70f;
        private const float BoxWidth = 90f;
        private const float LineHeight = 20f;
        private const float TextSize = 14f;
        private const string Preferred = "Radial Size";
        private static readonly List<string> Orders = new List<string> { "Default", "Name", "Level" };

        /// <summary>The game's settings dropdown to copy, or null when the menu has none.</summary>
        public static TMP_Dropdown Source()
        {
            GameObject settings = Menu.instance != null ? Menu.instance.m_settingsPrefab : null;
            if (settings == null)
                return null;
            TMP_Dropdown[] found = settings.GetComponentsInChildren<TMP_Dropdown>(true);
            return found.FirstOrDefault(dropdown => dropdown.name == Preferred) ?? found.FirstOrDefault();
        }

        /// <summary>Puts the bar into the room above the list (the list has already moved down by <see cref="Height"/>).</summary>
        public static void Build(SkillsDialog dialog, RectTransform frame, RectTransform list, TMP_Dropdown source)
        {
            RectTransform bar = Bar(frame, list);
            TMP_Dropdown dropdown = Object.Instantiate(source, bar, false);
            dropdown.name = "dropdown";
            Strip(dropdown);
            Place((RectTransform)dropdown.transform);
            Fit(dropdown.captionText, Label(dropdown));
            Fill(dropdown, dialog);
            bar.gameObject.SetActive(true);
        }

        /// <summary>The bar's box over the list's top edge, made inactive so the copy wakes only once it is stripped.</summary>
        private static RectTransform Bar(RectTransform frame, RectTransform list)
        {
            GameObject part = new GameObject("GrindstoneSkills_sort", typeof(RectTransform));
            part.SetActive(false);
            part.layer = frame.gameObject.layer;
            RectTransform bar = (RectTransform)part.transform;
            bar.SetParent(frame, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(list.anchoredPosition.x - list.sizeDelta.x / 2f, list.anchoredPosition.y + Height);
            bar.sizeDelta = new Vector2(list.sizeDelta.x, Height);
            return bar;
        }

        /// <summary>Drops the settings window's handlers and tooltip, which point into its prefab.</summary>
        private static void Strip(TMP_Dropdown dropdown)
        {
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            SettingsTooltip tooltip = dropdown.GetComponent<SettingsTooltip>();
            if (tooltip != null)
                Object.DestroyImmediate(tooltip);
        }

        private static void Place(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(LabelRoom, 0f);
            rect.sizeDelta = new Vector2(BoxWidth, LineHeight);
        }

        /// <summary>Renames the setting's name beside the dropdown, the text that is not its caption; null when it has none.</summary>
        private static TMP_Text Label(TMP_Dropdown dropdown)
        {
            foreach (Transform child in dropdown.transform)
            {
                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text == null || text == dropdown.captionText)
                    continue;
                text.text = "Sort by";
                return text;
            }
            return null;
        }

        /// <summary>Lets the caption fill the smaller box, in the label's size of text rather than shrunk to fit.</summary>
        private static void Fit(TMP_Text caption, TMP_Text label)
        {
            if (caption == null)
                return;
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            caption.enableAutoSizing = false;
            caption.fontSize = label != null ? label.fontSize : TextSize;
        }

        private static void Fill(TMP_Dropdown dropdown, SkillsDialog dialog)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(Orders);
            dropdown.SetValueWithoutNotify((int)SkillOrder.Current);
            dropdown.onValueChanged.AddListener(index => HookGuard.Run("skill sort", () => Chosen(dialog, (SkillSort)index)));
        }

        /// <summary>Saves the order and lays the open window out again in it (the pane keeps its skill).</summary>
        private static void Chosen(SkillsDialog dialog, SkillSort sort)
        {
            SkillOrder.Choose(sort);
            if (dialog != null && dialog.gameObject.activeInHierarchy && Player.m_localPlayer != null)
                dialog.Setup(Player.m_localPlayer);
        }
    }
}
