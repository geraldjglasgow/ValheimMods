using EliteCreaturesReborn.Recap.Window;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>What the right side of the altar window is made of, for the view to drive.</summary>
    internal sealed class AltarPane
    {
        public TMP_Text Title = null!;
        public TMP_Text Boss = null!;
        public TMP_Text StarsTitle = null!;
        public RectTransform StarRow = null!;
        public TMP_Text AspectTitle = null!;
        public TMP_Text Description = null!;
        public TMP_Text Fight = null!;

        /// <summary>The inventory's selected-slot frame, laid round the chosen star button; null when the game's
        /// inventory slot has none (the chosen label is then coloured instead).</summary>
        public Image? StarFrame;

        /// <summary>The orange of the list's "selected" bar, which the frame is tinted to.</summary>
        public Color StarTint = new Color(1f, 0.64f, 0f, 1f);
    }

    /// <summary>
    /// Builds the altar window from a copy of the game's own compendium window, as the death recap is built
    /// (<see cref="WindowBuilder"/>): the game's wood frame, list, buttons and fonts. One compact panel: the aspect list
    /// on the left, and on the right an inset like the list's holding the boss's name, a row of small star buttons
    /// (copies of the compendium's Close button), what the chosen aspect does and what the chosen fight pays; the Close
    /// button sits under the inset. Every part has its own space, so nothing covers anything else. Coordinates are from
    /// the frame's top-left corner.
    /// </summary>
    internal static class AltarWindowBuilder
    {
        public const float Width = 820f;
        public const float Height = 540f;
        private const float Top = 56f;
        private const float Bottom = 520f;
        private const float InsetLeft = 286f;
        private const float InsetWide = 514f;
        private const float InsetBottom = 466f;
        private const float Pad = 14f;

        /// <summary>The left edge and width of the inset's contents.</summary>
        public const float Left = InsetLeft + Pad;
        public const float Wide = InsetWide - 2f * Pad;

        public static AltarWindowView? Build()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_textsDialog == null)
            {
                return null;
            }
            RectTransform root = WindowBuilder.CanvasRoot(gui, "ECR_AltarChoice");
            WindowParts? parts = WindowBuilder.Copy(gui.m_textsDialog, root.gameObject, "ecr_altar");
            if (parts == null)
            {
                Object.Destroy(root.gameObject);
                return null;
            }
            Arrange(parts);
            AltarWindowView view = root.gameObject.AddComponent<AltarWindowView>();
            view.Init(Pane(parts), new ElementList(parts.ListRoot, parts.Element, parts.ListScroll, 28f, 31f), parts.Close);
            root.gameObject.SetActive(false);
            return view;
        }

        // The frame shrinks to the picker's size; the aspect list fills the left, the inset the right, the close button
        // sits under the inset, and the compendium's right text area is not used. The words are written as it opens.
        private static void Arrange(WindowParts parts)
        {
            parts.Frame.sizeDelta = new Vector2(Width, Height);
            UiParts.Place(parts.List, 20f, Top, InsetLeft - 36f, Bottom - Top);
            Image inset = UiParts.Image(parts.Inset, parts.Frame, "ecr_altar_inset");
            UiParts.Place(inset.rectTransform, InsetLeft, Top, InsetWide, InsetBottom - Top);
            inset.transform.SetSiblingIndex(parts.List.GetSiblingIndex() + 1);
            UiParts.Place((RectTransform)parts.Close.transform, InsetLeft + InsetWide - 160f, InsetBottom + 10f, 160f, 44f);
            UiParts.Rewire(parts.Close, AltarWindow.Close);
            UiParts.Rewire(parts.Backdrop, AltarWindow.Close);
            parts.Area.gameObject.SetActive(false);
        }

        private static AltarPane Pane(WindowParts parts)
        {
            AltarPane pane = new AltarPane
            {
                Title = parts.Topic,
                Boss = Line(parts, "ecr_altar_boss", Top + 8f, 32f, 24f, false),
                StarsTitle = Line(parts, "ecr_altar_stars", Top + 44f, 24f, 18f, false),
                AspectTitle = Line(parts, "ecr_altar_aspect", Top + 114f, 26f, 20f, false),
                Description = Line(parts, "ecr_altar_text", Top + 142f, 150f, 16f, true),
                Fight = Line(parts, "ecr_altar_fight", Top + 300f, InsetBottom - Top - 308f, 16f, true),
            };
            StarLook(pane, parts);
            pane.StarRow = UiParts.Node("ecr_altar_star_row", parts.Frame);
            UiParts.Place(pane.StarRow, Left, Top + 70f, Wide, 32f);
            return pane;
        }

        // The chosen star button's frame: the inventory slot's own, in the orange of the list's "selected" bar.
        private static void StarLook(AltarPane pane, WindowParts parts)
        {
            InventoryGrid grid = InventoryGui.instance.m_playerGrid;
            GameObject? slot = grid != null ? grid.m_elementPrefab : null;
            pane.StarFrame = slot != null ? Utils.FindChild(slot.transform, "selected")?.GetComponent<Image>() : null;
            Image? bar = Utils.FindChild(parts.Element.transform, "selected")?.GetComponent<Image>();
            if (bar != null)
            {
                pane.StarTint = bar.color;
            }
        }

        private static TMP_Text Line(WindowParts parts, string name, float y, float h, float size, bool wrap)
        {
            TextAlignmentOptions align = wrap ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            TMP_Text text = UiParts.Text(parts.Text, parts.Frame, name, size, align);
            UiParts.Place(text.rectTransform, Left, y, Wide, h);
            text.enableWordWrapping = wrap;
            return text;
        }
    }
}
