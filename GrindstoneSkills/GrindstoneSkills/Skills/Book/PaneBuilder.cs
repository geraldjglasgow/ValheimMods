using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrindstoneSkills
{
    /// <summary>
    /// Builds the info pane beside the skills list (<see cref="BookLayout"/> makes the room): a dark box like the list's,
    /// a header with the skill's icon, its name in the entries' gold font and a level line, a thin rule, and under it the
    /// page text in a scroll view with its own scrollbar (<see cref="PaneScrollbar"/>). Fonts and the box sprite are
    /// taken from the window's own parts, so the pane reads like the list. The page text carries PlateColumn's word
    /// tips (<see cref="LinkTips"/>). Local UI only.
    /// </summary>
    internal static class PaneBuilder
    {
        private const float Padding = 12f;
        private const float IconSize = 44f;
        private const float HeaderHeight = 66f;
        private const float BarWidth = 8f;
        private static readonly Color RuleColour = new Color(0.85f, 0.76f, 0.63f, 0.35f);

        /// <summary>Builds the pane beside the list, reaching <paramref name="raised"/> above the list's top (the sort bar's room).</summary>
        public static BookPane Build(SkillsDialog dialog, RectTransform frame, RectTransform list, float raised)
        {
            RectTransform pane = Box(frame, list, raised);
            BookPane book = pane.gameObject.AddComponent<BookPane>();
            book.Dialog = dialog;
            book.Icon = Icon(pane);
            Transform entry = dialog.m_elementPrefab.transform;
            book.Title = Title(pane, Utils.FindChild(entry, "name").GetComponent<TMP_Text>());
            TMP_Text bodyFont = Utils.FindChild(entry, "leveltext").GetComponent<TMP_Text>();
            book.Level = LevelLine(pane, bodyFont);
            Rule(pane);
            book.Scroll = Scroll(pane, dialog.skillListScrollRect, bodyFont.font, out TMP_Text body);
            book.Body = Body(body, bodyFont);
            book.Scroll.verticalScrollbar = PaneScrollbar.Build(pane, dialog.scrollbar, BarWidth, Padding, HeaderHeight);
            book.Tips = InventoryGui.instance != null ? LinkTips.On(InventoryGui.instance, book.Body) : null;
            return book;
        }

        private static RectTransform Box(RectTransform frame, RectTransform list, float raised)
        {
            RectTransform pane = Part(BookLayout.PaneName, frame);
            pane.anchorMin = pane.anchorMax = new Vector2(0.5f, 1f);
            pane.pivot = new Vector2(0f, 1f);
            pane.anchoredPosition = new Vector2(list.anchoredPosition.x + list.sizeDelta.x / 2f + BookLayout.Gap, list.anchoredPosition.y + raised);
            pane.sizeDelta = new Vector2(BookLayout.PaneWidth, list.sizeDelta.y + raised);
            Image image = pane.gameObject.AddComponent<Image>();
            Image source = list.GetComponent<Image>();
            if (source != null)
            {
                image.sprite = source.sprite;
                image.type = source.type;
                image.color = source.color;
            }
            return pane;
        }

        private static Image Icon(RectTransform pane)
        {
            RectTransform icon = Part("icon", pane);
            TopLeft(icon, Padding, Padding, IconSize, IconSize);
            Image image = icon.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Title(RectTransform pane, TMP_Text source)
        {
            TMP_Text title = TextLike(source, Part("title", pane));
            TopStretch(title.rectTransform, Padding + IconSize + 10f, 10f, 28f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 25f;
            title.alignment = TextAlignmentOptions.Left;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            return title;
        }

        private static TMP_Text LevelLine(RectTransform pane, TMP_Text source)
        {
            TMP_Text level = TextLike(source, Part("level", pane));
            TopStretch(level.rectTransform, Padding + IconSize + 10f, 40f, 20f);
            level.fontSize = 15f;
            level.color = Skin.LabelColour;
            level.alignment = TextAlignmentOptions.Left;
            level.textWrappingMode = TextWrappingModes.NoWrap;
            return level;
        }

        private static void Rule(RectTransform pane)
        {
            RectTransform rule = Part("rule", pane);
            TopStretch(rule, Padding, HeaderHeight - 4f, 1f);
            rule.offsetMax = new Vector2(-Padding, rule.offsetMax.y);
            Image line = rule.gameObject.AddComponent<Image>();
            line.color = RuleColour;
            line.raycastTarget = false;
        }

        private static ScrollRect Scroll(RectTransform pane, ScrollRect like, TMP_FontAsset font, out TMP_Text body)
        {
            RectTransform area = Part("scroll", pane);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Padding, Padding);
            area.offsetMax = new Vector2(-(Padding + BarWidth + 6f), -(HeaderHeight + 2f));
            RectTransform viewport = Part("viewport", area);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform content = Part("text", viewport);
            body = NewText(content, font);
            ScrollRect scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = like != null ? like.scrollSensitivity : 30f;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static TMP_Text Body(TMP_Text body, TMP_Text source)
        {
            Copy(source, body);
            body.fontSize = 15f;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.paragraphSpacing = 2f;
            RectTransform rect = body.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            ContentSizeFitter fit = body.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return body;
        }

        private static TMP_Text TextLike(TMP_Text source, RectTransform part)
        {
            TMP_Text text = NewText(part, source.font);
            Copy(source, text);
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A text on the part that wakes with its font already set, so it never looks for TextMeshPro's missing default.</summary>
        private static TMP_Text NewText(RectTransform part, TMP_FontAsset font)
        {
            part.gameObject.SetActive(false);
            TMP_Text text = part.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            part.gameObject.SetActive(true);
            return text;
        }

        private static void Copy(TMP_Text source, TMP_Text text)
        {
            text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial;
            text.fontSize = source.fontSize;
            text.fontStyle = source.fontStyle;
            text.color = source.color;
            text.richText = true;
        }

        private static RectTransform Part(string name, Transform parent)
        {
            GameObject part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            return (RectTransform)part.transform;
        }

        private static void TopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>A strip across the pane's top: <paramref name="left"/> in from its left edge, <see cref="Padding"/> from its right.</summary>
        private static void TopStretch(RectTransform rect, float left, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-Padding, -top);
        }
    }
}
