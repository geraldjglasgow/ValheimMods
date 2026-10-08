using System.Linq;
using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

namespace Wayfare.Targeting
{
    /// <summary>The picker's destination dropdown: a copy of one of the game's own settings dropdowns (Radial Size, from
    /// the settings window the in-game menu keeps as a prefab), so it looks and sounds like the game's, as GrindstoneSkills'
    /// sort bar copies it. The copy loses the settings window's tooltip and handlers and the setting's name beside it,
    /// and takes the place and size of the text field it replaces in the text input window, its caption in the field's
    /// size of text.</summary>
    internal static class PickerDropdown
    {
        private const string Preferred = "Radial Size";
        private const float TextSize = 20f;

        /// <summary>The game's settings dropdown to copy, or null when the menu has none.</summary>
        public static TMP_Dropdown Source()
        {
            GameObject settings = Menu.instance != null ? Menu.instance.m_settingsPrefab : null;
            if (settings == null)
                return null;
            TMP_Dropdown[] found = settings.GetComponentsInChildren<TMP_Dropdown>(true);
            return found.FirstOrDefault(dropdown => dropdown.name == Preferred) ?? found.FirstOrDefault();
        }

        /// <summary>A stripped copy beside <paramref name="field"/>, in its place; the field itself stays for the caller.</summary>
        public static TMP_Dropdown Make(TMP_Dropdown source, RectTransform field)
        {
            TMP_Dropdown dropdown = Object.Instantiate(source, field.parent, false);
            dropdown.name = "Destination";
            Strip(dropdown);
            Place((RectTransform)dropdown.transform, field);
            HideName(dropdown);
            if (dropdown.captionText != null)
            {
                dropdown.captionText.enableAutoSizing = false;
                dropdown.captionText.fontSize = TextSize;
            }
            return dropdown;
        }

        /// <summary>Drops the settings window's handlers and tooltip, which point into its prefab.</summary>
        private static void Strip(TMP_Dropdown dropdown)
        {
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            SettingsTooltip tooltip = dropdown.GetComponent<SettingsTooltip>();
            if (tooltip != null)
                Object.DestroyImmediate(tooltip);
        }

        private static void Place(RectTransform rect, RectTransform field)
        {
            rect.anchorMin = field.anchorMin;
            rect.anchorMax = field.anchorMax;
            rect.pivot = field.pivot;
            rect.anchoredPosition = field.anchoredPosition;
            rect.sizeDelta = field.sizeDelta;
            rect.SetSiblingIndex(field.GetSiblingIndex());
        }

        /// <summary>Hides the setting's name beside the box, the text that is not its caption; the window's title says it.</summary>
        private static void HideName(TMP_Dropdown dropdown)
        {
            foreach (Transform child in dropdown.transform)
            {
                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text != null && text != dropdown.captionText)
                    child.gameObject.SetActive(false);
            }
        }
    }
}
