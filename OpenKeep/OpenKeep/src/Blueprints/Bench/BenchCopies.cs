using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Copies of the game's own UI parts for the bench window, so it looks and sounds like the game's (and like whatever
    /// a UI mod made of it): the crafting panel's wood background, title font, list and buttons, the recipe row. A copy
    /// loses every component that would tie it to the game's panel (gamepad hooks and hints, tooltips, localizers that
    /// would write their words back, layout groups another mod added); a UI mod's skin is shed, so that mod skins the copy
    /// itself when it is shown (PackPanel's timber look does, by the game's wood panel sprite; found by its published
    /// object name only). Made under the window's inactive canvas, so nothing of a copy wakes before it is cleaned.
    /// </summary>
    public static class BenchCopies
    {
        // PackPanel's timber background, a child of each wood panel image it skins.
        private const string PackPanelSkin = "PackPanel_timberwood";

        private static readonly HashSet<string> Foreign = new HashSet<string>
        {
            "UIGroupHandler", "UIGamePad", "UIInputHint", "EventTrigger", "Localize", "UIInputHandler", "UITooltip",
            "ScrollRectEnsureVisible", "GridLayoutGroup", "VerticalLayoutGroup", "HorizontalLayoutGroup", "ContentSizeFitter",
        };

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static GameObject Copy(Component source, Transform parent, string name)
        {
            GameObject copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            StripForeign(copy);
            ShedSkins(copy);
            copy.SetActive(true);
            return copy;
        }

        private static void StripForeign(GameObject copy)
        {
            foreach (Component part in copy.GetComponentsInChildren<Component>(true))
            {
                if (part != null && Foreign.Contains(part.GetType().Name))
                    Object.DestroyImmediate(part);
            }
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
            {
                if (part != null && part != copy.transform && part.name.StartsWith("gamepad_hint", StringComparison.Ordinal))
                    Object.DestroyImmediate(part.gameObject);
            }
        }

        /// <summary>The dead skin goes and the wood image gets the game's look back (PackPanel keeps the sprite, only hides it).</summary>
        private static void ShedSkins(GameObject copy)
        {
            foreach (Image image in copy.GetComponentsInChildren<Image>(true))
            {
                Transform skin = image.transform.Find(PackPanelSkin);
                if (skin == null)
                    continue;
                Object.DestroyImmediate(skin.gameObject);
                image.color = Color.white;
                image.fillCenter = true;
                image.material = null;
            }
        }

        /// <summary>A copy of a game text (its font, material and outline) emptied, one line, at a size and alignment.</summary>
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
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// A copy of a game button with a fresh click. Its label takes the original's enabled colour before the copy wakes
        /// (the game's ButtonTextColor keeps the colour its label has when it wakes as the enabled one, and the original may
        /// be greyed right now).
        /// </summary>
        public static Button Button(Button source, Transform parent, string name, string word, Action click)
        {
            ButtonTextColor colours = source.GetComponent<ButtonTextColor>();
            GameObject go = Copy(source, parent, name);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = colours != null ? colours.m_defaultColor : label.color;
                label.text = Core.Language.Localize(word);
                label.enableAutoSizing = false;
                label.fontSize = 17f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => BlueprintSafe.Run("OpenKeep blueprint bench " + name, click));
            button.interactable = true;
            return button;
        }

        /// <summary>Anchored at the parent's top left corner, at x, y (y down is negative) from there, that size.</summary>
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
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
