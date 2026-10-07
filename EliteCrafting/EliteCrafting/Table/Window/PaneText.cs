using EliteCrafting.Text;
using TMPro;
using UnityEngine;

namespace EliteCrafting.Tables.Window
{
    /// <summary>Fills the description panel's fixed parts: icon, name, text, row labels, the main and the small button.</summary>
    internal static class PaneText
    {
        public static void Header(PanelParts parts, Sprite? icon, string name)
        {
            if (parts.Icon != null)
            {
                parts.Icon.gameObject.SetActive(icon != null);
                parts.Icon.sprite = icon;
            }
            if (parts.Name != null)
            {
                parts.Name.text = name;
            }
        }

        public static void Body(PanelParts parts, string text)
        {
            if (parts.Text != null)
            {
                parts.Text.text = text;
            }
        }

        /// <summary>The two row labels; a null key hides that label (its row is hidden by the tab).</summary>
        public static void Labels(PanelParts parts, string? rune, string? essence)
        {
            Label(parts.RuneLabel, rune);
            Label(parts.EssenceLabel, essence);
        }

        public static void Button(PanelParts parts, string key, bool interactable)
        {
            if (parts.Action == null)
            {
                return;
            }
            parts.Action.interactable = interactable;
            TMP_Text? label = parts.LabelOf(parts.Action);
            if (label != null)
            {
                label.text = Words.Localize(key);
            }
        }

        /// <summary>The small button under the name; a null key hides it.</summary>
        public static void Extra(PanelParts parts, string? key, bool interactable)
        {
            if (parts.Extra == null)
            {
                return;
            }
            parts.Extra.gameObject.SetActive(key != null);
            parts.Extra.interactable = interactable;
            TMP_Text? label = parts.LabelOf(parts.Extra);
            if (label != null && key != null)
            {
                label.text = Words.Localize(key);
            }
        }

        private static void Label(TMP_Text? label, string? key)
        {
            if (label == null)
            {
                return;
            }
            label.gameObject.SetActive(key != null);
            label.text = key != null ? Words.Localize(key) : "";
        }
    }
}
