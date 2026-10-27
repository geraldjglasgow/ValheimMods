using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>The figures panel's parts: its lines of text and its three number boxes (longer, wider, scale).</summary>
    internal sealed class KwBpFiguresParts
    {
        public KwBpFiguresParts(RectTransform root, TMP_Text[] texts, TMP_InputField[] boxes)
        {
            Root = root;
            (DockHead, DockBody, PickHead, PickBody, Note) = (texts[0], texts[1], texts[2], texts[3], texts[4]);
            Boxes = boxes;
        }

        public RectTransform Root { get; }
        public TMP_Text DockHead { get; }
        public TMP_Text DockBody { get; }
        public TMP_Text PickHead { get; }
        public TMP_Text PickBody { get; }
        public TMP_Text Note { get; }
        public TMP_InputField[] Boxes { get; }
    }

    /// <summary>
    /// The figures panel's objects (<see cref="KwBpFiguresPanel"/>), made beside the table's window (BlueprintShelf's), as
    /// a child of it so it shows, hides and goes with it: the crafting panel's wood (its shadow behind the window's own
    /// parts), a title with the braid under it, the ship on the dock's figures, then the picked ship's: the three boxes
    /// for building it back longer, wider or scaled, its figures as it would be built, and a line for why it cannot be.
    /// Every part is a copy of the game's own (<see cref="KwFormCopies"/>, the number boxes <see cref="KwFormBoxes"/>),
    /// made while the panel is hidden so nothing of a copy wakes before it is cleaned. The window moves left so the two
    /// stand centred together when the screen has room for both.
    /// </summary>
    internal static class KwBpFiguresBuild
    {
        public const float Width = 460f, Gap = 12f;
        private const float Margin = 20f, BodyHeight = 250f, BoxWidth = 120f;
        public static readonly string[] Labels = { "Longer %", "Wider %", "Scale %" };

        public static KwBpFiguresParts? Make(RectTransform window)
        {
            RectTransform? crafting = InventoryGui.instance != null ? InventoryGui.instance.m_crafting : null;
            TMP_InputField? template = KwFormBoxes.Template();
            Transform? topic = crafting != null ? crafting.Find("topic") : null;
            if (crafting == null || topic == null || template == null)
                return null;
            RectTransform root = KwFormCopies.Rect(KwBpFiguresPanel.ObjectName, window);
            root.gameObject.SetActive(false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(window.sizeDelta.x + Gap, 0f);
            root.sizeDelta = new Vector2(Width, window.sizeDelta.y);
            Backdrop(crafting, root);
            TMP_Text title = Title(crafting, topic.GetComponent<TMP_Text>(), root);
            KwBpFiguresParts parts = new KwBpFiguresParts(root, Texts(title, root), Boxes(template, title, root));
            Centre(window);
            root.gameObject.SetActive(true);
            return parts;
        }

        // The wood under the panel; its soft shadow behind the window's own parts (first under the window), so it never
        // darkens the window beside it.
        private static void Backdrop(RectTransform crafting, RectTransform root)
        {
            Transform darken = crafting.Find("Darken");
            if (darken != null && root.parent is RectTransform window)
            {
                // copied under the hidden panel, so nothing of it wakes before it is cleaned, then put under the window
                RectTransform shadow = (RectTransform)KwFormCopies.Copy(darken, root, KwBpFiguresPanel.ObjectName + "_shadow").transform;
                shadow.SetParent(window, false);
                KwFormCopies.Place(shadow, root.anchoredPosition.x - 50f, 50f, Width + 100f, root.sizeDelta.y + 100f);
                shadow.SetAsFirstSibling();
            }
            Transform background = crafting.Find("Bkg");
            if (background != null)
                KwFormCopies.Fill((RectTransform)KwFormCopies.Copy(background, root, "Bkg").transform, 0f);
        }

        private static TMP_Text Title(RectTransform crafting, TMP_Text topic, RectTransform root)
        {
            TMP_Text title = KwFormCopies.Text(topic, root, "Title", 26f, TextAlignmentOptions.Center);
            KwFormCopies.Place(title.rectTransform, 0f, -12f, Width, 40f);
            title.text = "Ship figures";
            Transform braid = crafting.Find("BraidLineHorisontalMedium");
            if (braid != null)
                KwFormCopies.Place((RectTransform)KwFormCopies.Copy(braid, root, "Braid").transform, (Width - 300f) / 2f, -52f, 300f, 12f);
            return title;
        }

        // The dock's header and lines, the picked ship's header and lines, and the note: from the top down.
        private static TMP_Text[] Texts(TMP_Text title, RectTransform root)
        {
            float pickTop = 72f + 26f + BodyHeight + 10f;
            return new[]
            {
                Words(title, root, "DockHead", 18f, 72f, 26f, false),
                Words(title, root, "DockBody", 15f, 98f, BodyHeight, true),
                Words(title, root, "PickHead", 18f, pickTop, 26f, false),
                Words(title, root, "PickBody", 15f, pickTop + 86f, BodyHeight, true),
                Words(title, root, "Note", 15f, pickTop + 86f + BodyHeight + 4f, 44f, true),
            };
        }

        private static TMP_Text Words(TMP_Text font, RectTransform root, string name, float size, float y, float height, bool wrap)
        {
            TMP_Text text = KwFormCopies.Text(font, root, name, size, TextAlignmentOptions.TopLeft);
            text.richText = true;
            text.color = Color.white;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Ellipsis;
            // a long body (a big ship's cost, its sails' winds) is drawn a little smaller rather than cut off
            (text.enableAutoSizing, text.fontSizeMin, text.fontSizeMax) = (wrap, size * 0.8f, size);
            KwFormCopies.Place(text.rectTransform, Margin, -y, Width - 2f * Margin, height);
            return text;
        }

        // The three boxes side by side under the picked ship's header, each with its label over it.
        private static TMP_InputField[] Boxes(TMP_InputField template, TMP_Text font, RectTransform root)
        {
            float top = 72f + 26f + BodyHeight + 10f + 30f, step = (Width - 2f * Margin - BoxWidth) / 2f;
            TMP_InputField[] boxes = new TMP_InputField[Labels.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                float x = Margin + i * step;
                TMP_Text label = KwFormCopies.Text(font, root, "Label" + i, 15f, TextAlignmentOptions.Center);
                label.color = KwFormCopies.Pale;
                label.text = Labels[i];
                KwFormCopies.Place(label.rectTransform, x, -top, BoxWidth, 20f);
                boxes[i] = KwFormBoxes.Number(template, root, "Box" + i);
                KwFormCopies.Place((RectTransform)boxes[i].transform, x, -(top + 22f), BoxWidth, 30f);
            }
            return boxes;
        }

        // The window moved left by half the panel's room, so both stand centred; on a screen too narrow for both, the
        // window as far left as its edge allows.
        private static void Centre(RectTransform window)
        {
            float room = window.parent is RectTransform canvas ? canvas.rect.width : 0f;
            float both = window.sizeDelta.x + Gap + Width, shift = (Gap + Width) / 2f;
            if (room > 0f && both + 40f > room)
                shift = Mathf.Max(0f, (room - window.sizeDelta.x) / 2f - 20f);
            window.anchoredPosition = new Vector2(-shift, window.anchoredPosition.y);
        }
    }
}
