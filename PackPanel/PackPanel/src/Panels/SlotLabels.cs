using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Slots;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// What an empty slot shows (the user's mockup): a bronze-tinted icon of what belongs there (<see cref="SlotIcons"/>)
    /// with the slot's name under it in the game's serif (the font of its buttons), light and easy to read. An item in
    /// the slot covers both: they show only while the slot is empty (<c>InventoryElement.m_used</c>, which the game sets
    /// every frame). No outline: on the game's font material an outline swallowed the letters. Children of the element,
    /// so they go where the element goes and die with it when the grid rebuilds. Slot Labels off hides them.
    /// </summary>
    public static class SlotLabels
    {
        private const string Name = "PackPanel_hint";
        private const float IconSize = 34f;
        private const float IconTop = 7f;
        private static readonly Color CaptionColour = new Color(0.96f, 0.89f, 0.74f, 1f);

        private static readonly List<InventoryElement> captioned = new List<InventoryElement>();

        public static string TextFor(Slot slot) => Language.Localize(Words.Caption(slot.Kind));

        /// <summary>The grid was placed again: the hints to keep up to date are the ones set from now on.</summary>
        public static void Clear() => captioned.Clear();

        public static void Set(InventoryElement element, Slot slot, TMP_FontAsset font)
        {
            Transform hint = element.transform.Find(Name) ?? Create(element, font);
            hint.GetComponentInChildren<TMP_Text>(true).text = TextFor(slot);
            Image icon = hint.Find("icon").GetComponent<Image>();
            icon.sprite = SlotIcons.For(slot.Kind);
            icon.color = SlotIcons.TintFor(slot.Kind);
            icon.enabled = icon.sprite != null;
            captioned.Add(element);
        }

        /// <summary>Every frame the grid is drawn: a hint shows while its slot is empty.</summary>
        public static void Refresh()
        {
            bool on = InventorySettings.SlotLabels.Value;
            foreach (InventoryElement element in captioned)
            {
                Transform hint = element != null ? element.transform.Find(Name) : null;
                if (hint == null)
                    continue;
                bool show = on && !element.m_used;
                if (hint.gameObject.activeSelf != show)
                    hint.gameObject.SetActive(show);
            }
        }

        private static Transform Create(InventoryElement element, TMP_FontAsset font)
        {
            RectTransform hint = Child(element.transform, Name);
            hint.anchorMin = Vector2.zero;
            hint.anchorMax = Vector2.one;
            hint.offsetMin = hint.offsetMax = Vector2.zero;
            hint.SetSiblingIndex(element.m_icon.transform.GetSiblingIndex());
            Icon(Child(hint, "icon"));
            Caption(Child(hint, "caption"), font ?? element.m_amount.font);
            return hint;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Icon(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = new Vector2(0f, -IconTop);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = SlotIcons.Hint;
        }

        private static void Caption(RectTransform rect, TMP_FontAsset font)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-4f, 17f);
            rect.anchoredPosition = new Vector2(0f, 3f);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 14f;
            label.color = CaptionColour;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
        }
    }
}
