using System;
using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Copies of the crafting panel's own buttons for OpenKeep's parts of it, so they look and sound like the game's.
    /// A copy gets a fresh click handler and loses the original's gamepad key and hint (OpenKeep reads its gamepad
    /// shortcuts itself, see <see cref="RecipeGamepad"/>), so a gamepad press never clicks it by surprise.
    /// </summary>
    public static class PanelButton
    {
        public static Button Clone(Button template, Transform parent, string name, Action click)
        {
            GameObject go = Object.Instantiate(template.gameObject, parent, false);
            go.name = name;
            foreach (UIGamePad pad in go.GetComponentsInChildren<UIGamePad>(true))
            {
                // A hint outside the copy is the game's own (Instantiate only remaps references inside the copy).
                if (pad.m_hint != null && pad.m_hint.transform.IsChildOf(go.transform))
                    Object.DestroyImmediate(pad.m_hint);
                Object.DestroyImmediate(pad);
            }
            foreach (UIInputHint hint in go.GetComponentsInChildren<UIInputHint>(true))
                Object.DestroyImmediate(hint.gameObject);
            // A copy made before the scene's Localize starts would keep the original's $word: Localize would cache it
            // and write it back on every language or input change. An empty text is never cached.
            foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
                text.text = "";
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Guard.Run(name, click));
            button.interactable = true;
            go.SetActive(true);
            return button;
        }

        /// <summary>The button's text, or null when it has none.</summary>
        public static TMP_Text Label(Button button) => button.GetComponentInChildren<TMP_Text>(true);

        /// <summary>An icon over the button, inset from its edge, coloured by the caller; its text is emptied.</summary>
        public static Image Icon(Button button, Sprite sprite, float inset)
        {
            TMP_Text label = Label(button);
            if (label != null)
                label.text = "";
            GameObject go = new GameObject("OpenKeep_icon", typeof(RectTransform), typeof(Image));
            go.layer = button.gameObject.layer;
            go.transform.SetParent(button.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Top left anchored (the parent's top left corner), at x, y from there, that size.</summary>
        public static void PlaceTopLeft(RectTransform rect, float x, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        public static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}
