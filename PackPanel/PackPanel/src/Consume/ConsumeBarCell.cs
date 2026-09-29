using PackPanel.Core;
using PackPanel.Panels;
using PackPanel.Slots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Consume
{
    /// <summary>
    /// One cell of the Food and Mead bar (<see cref="ConsumeBar"/>): a copy of the game's own HUD food square (its dark
    /// square, the icon filling it and the small text in its corner, here the stack's count), placed along the bar. A
    /// slot's cell shows the item in that slot, dimmed while its key would skip it (<see cref="SlotMeals.CanTakeNow"/>),
    /// or the slot's own icon faintly while the slot is empty. A key cap is the same square with the key's name in the
    /// yellow of the game's key hints, as wide as the name needs.
    /// </summary>
    public sealed class ConsumeBarCell
    {
        public const float Size = 42f;
        private const float KeyFont = 20f;
        private const float KeyPad = 12f;
        private static readonly Color Ready = Color.white;
        private static readonly Color Skipped = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color Empty = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color KeyColour = new Color(1f, 0.92f, 0.016f, 1f);   // Unity's yellow, the game's key hints

        private readonly Image icon;
        private readonly TMP_Text count;
        private readonly SlotKind kind;
        private readonly int number;

        private ConsumeBarCell(RectTransform rect, SlotKind kind, int number)
        {
            icon = IconOf(rect);
            count = rect.GetComponentInChildren<TMP_Text>(true);
            if (count != null)
                count.textWrappingMode = TextWrappingModes.NoWrap;
            this.kind = kind;
            this.number = number;
        }

        /// <summary>The cell of slot <paramref name="number"/> of a kind, its left edge at <paramref name="x"/>.</summary>
        public static ConsumeBarCell Slot(RectTransform bar, GameObject template, SlotKind kind, int number, float x)
        {
            RectTransform rect = Copy(bar, template, "PackPanel_" + kind.ToString().ToLowerInvariant() + number, x);
            return new ConsumeBarCell(rect, kind, number);
        }

        /// <summary>A key cap with its left edge at <paramref name="x"/>; its width.</summary>
        public static float KeyCap(RectTransform bar, GameObject template, string key, float x)
        {
            RectTransform rect = Copy(bar, template, "PackPanel_key", x);
            Image image = IconOf(rect);
            if (image != null)
                image.enabled = false;
            TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return Size;
            float width = Mathf.Max(Size, KeyLabel(label, key) + KeyPad);
            rect.sizeDelta = new Vector2(width, Size);
            return width;
        }

        /// <summary>Shows what the slot holds now; the player is the local one, for whether the key would take it.</summary>
        public void Fill(Player player)
        {
            ItemDrop.ItemData item = InventoryState.ItemIn(kind, number);
            if (icon != null)
            {
                Sprite sprite = item != null ? item.GetIcon() : SlotIcons.For(kind);
                if (icon.sprite != sprite)
                    icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.color = item == null ? Empty : SlotMeals.CanTakeNow(player, item) ? Ready : Skipped;
            }
            if (count != null)
                count.text = item != null && item.m_shared.m_maxStackSize > 1 ? item.m_stack.ToString() : "";
        }

        /// <summary>The corner text stretched over the square, centred, in yellow; the width the name needs.</summary>
        private static float KeyLabel(TMP_Text label, string key)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            label.enableAutoSizing = false;
            label.fontSize = KeyFont;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = KeyColour;
            label.text = key;
            return label.GetPreferredValues(key).x;
        }

        /// <summary>A live copy of the game's food square: shown, its children shown, at x along the bar's bottom.</summary>
        private static RectTransform Copy(RectTransform bar, GameObject template, string name, float x)
        {
            GameObject go = Object.Instantiate(template, bar, false);
            go.name = name;
            go.SetActive(true);
            RectTransform rect = (RectTransform)go.transform;
            foreach (Transform child in rect)
                child.gameObject.SetActive(true);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(Size, Size);
            rect.anchoredPosition = new Vector2(x, 0f);
            return rect;
        }

        /// <summary>The square's icon: its first image below the square itself.</summary>
        private static Image IconOf(RectTransform rect)
        {
            foreach (Image image in rect.GetComponentsInChildren<Image>(true))
            {
                if (image.transform != rect)
                    return image;
            }
            return null;
        }
    }
}
