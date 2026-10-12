using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Copies of the game's own UI parts for the Spellbook (the crafting panel's background and title font, the recipe
    /// row, the craft button, the hotbar slot), so it looks like the game's and like whatever a UI mod made of it. A copy
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

        /// <summary>A copy of a game button (or any element with a Button) with a fresh click and a label, when it has one.</summary>
        public static Button Button(Component source, Transform parent, string name, string word, Action click)
        {
            ButtonTextColor colours = source.GetComponent<ButtonTextColor>();
            GameObject go = Copy(source, parent, name);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null && word != null)
            {
                label.color = colours != null ? colours.m_defaultColor : label.color;
                label.text = Core.Language.Localize(word);
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
        public static void Fill(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
