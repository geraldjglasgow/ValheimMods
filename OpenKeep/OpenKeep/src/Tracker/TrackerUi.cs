using System;
using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The tracker's building blocks: nodes, texts in the tracker's font (created asleep so they never look for
    /// TextMeshPro's missing default font), icons, rows and columns laid out by Unity's layout groups, and small text
    /// buttons that light up in the game's orange under the pointer. Only buttons and the title catch the pointer.
    /// </summary>
    public static class TrackerUi
    {
        private static readonly Color Hover = new Color(1f, 0.713f, 0.361f, 1f);

        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static TMP_Text Text(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = TrackerStyle.Font;
            go.SetActive(true);
            text.fontSize = size;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = Color.white;
            return text;
        }

        public static Image Icon(Transform parent, Sprite sprite, float size)
        {
            RectTransform rect = Node("icon", parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Fixed(rect.gameObject, size, size);
            return image;
        }

        /// <summary>A square button showing one character; the click runs guarded.</summary>
        public static Button TextButton(Transform parent, string name, string label, float size, Action click)
        {
            RectTransform rect = Node(name, parent);
            Image hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = rect.gameObject.AddComponent<Button>();
            TMP_Text text = Text(rect, "label", size, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform);
            text.text = label;
            button.targetGraphic = text;
            ColorBlock colours = button.colors;
            colours.highlightedColor = Hover;
            colours.pressedColor = Hover;
            button.colors = colours;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Guard.Run("tracker " + name, click));
            Fixed(rect.gameObject, size, size);
            return button;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float spacing)
        {
            HorizontalLayoutGroup row = Node(name, parent).gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        public static VerticalLayoutGroup Column(GameObject go, float spacing)
        {
            VerticalLayoutGroup column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = spacing;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return column;
        }

        public static void Fixed(GameObject go, float width, float height)
        {
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        /// <summary>A text that takes the row's spare width, at least that much.</summary>
        public static void Flexible(Component text, float minWidth)
        {
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.minWidth = minWidth;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
