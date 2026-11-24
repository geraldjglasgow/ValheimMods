using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Copies of the game's own UI parts for the Spellbook (the crafting panel's background, darkening and title, the
    /// recipe list's panel, the recipe row, the item name and description fonts, the craft button, the hotbar slot), so it
    /// looks like the game's and like whatever a UI mod made of it. A copy
    /// loses what ties it to the game's panel (gamepad hooks and hints, tooltips, localizers, layout groups) and a UI mod's
    /// skin (PackPanel's timber child), so that mod skins the copy itself. The same rules as OpenKeep's
    /// <c>Blueprints/Bench/BenchCopies</c>; the two are to become one library.
    /// </summary>
    public static class CasterUiCopies
    {
        private const string PackPanelSkin = "PackPanel_timberwood";

        private static readonly HashSet<string> Foreign = new HashSet<string>
        {
            "UIGroupHandler", "UIGamePad", "UIInputHint", "EventTrigger", "Localize", "UIInputHandler", "UITooltip",
            "ScrollRectEnsureVisible", "GridLayoutGroup", "VerticalLayoutGroup", "HorizontalLayoutGroup", "ContentSizeFitter",
        };

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static GameObject Copy(Component source, Transform parent, string name)
        {
            GameObject copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            foreach (Component part in copy.GetComponentsInChildren<Component>(true))
            {
                if (part != null && Foreign.Contains(part.GetType().Name))
                    Object.DestroyImmediate(part);
            }
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
            {
                if (part != null && part != copy.transform && (part.name.StartsWith("gamepad_hint", StringComparison.Ordinal) || part.name == PackPanelSkin))
                    Object.DestroyImmediate(part.gameObject);
            }
            copy.SetActive(true);
            return copy;
        }

        /// <summary>A copy of a game text (font, material, outline) emptied, at a size and alignment.</summary>
        public static TMP_Text Text(TMP_Text source, Transform parent, string name, float size, TextAlignmentOptions align)
        {
            GameObject go = Copy(source, parent, name);
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = "";
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A copy of a game button (or any element with a Button) with a fresh click and a label text, when it has one.</summary>
        public static Button Button(Component source, Transform parent, string name, string text, Action click)
        {
            ButtonTextColor colours = source.GetComponent<ButtonTextColor>();
            GameObject go = Copy(source, parent, name);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null && text != null)
            {
                label.color = colours != null ? colours.m_defaultColor : label.color;
                label.text = text;
            }
            Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => CasterSafe.Run("spellbook " + name, click));
            button.interactable = true;
            return button;
        }

        /// <summary>Anchored at the parent's top left corner, at x, y (down is negative) from there, that size.</summary>
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>Fills the parent, inset (positive) or outset (negative) by a margin on every side.</summary>
        public static void Fill(RectTransform rect, float margin) =>
            Stretch(rect, Vector2.zero, Vector2.one, margin, margin, margin, margin);

        /// <summary>
        /// Between two corners given as fractions of the parent, then inset by left, bottom, right and top (each counted
        /// inward from its own edge; negative reaches out past it).
        /// </summary>
        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max, float left, float bottom, float right, float top)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>A new image drawn as a game image is (sprite, type, colour, material), without its children or parts; none drawn without one.</summary>
        public static Image Panel(Image source, Transform parent, string name)
        {
            Image image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.enabled = source != null;
            if (source == null)
                return image;
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = source.color;
            image.material = source.material;
            image.fillCenter = source.fillCenter;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            return image;
        }

        /// <summary>Shrinks to fit its rect, down to a share of its size, wrapped or on one line; cut with an ellipsis past that.</summary>
        public static void Shrink(TMP_Text text, float least, bool wrap)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = text.fontSize * least;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
