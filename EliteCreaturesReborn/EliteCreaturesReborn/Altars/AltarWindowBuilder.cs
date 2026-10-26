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
        public TMP_Text ExtrasTitle = null!;
        public RectTransform ExtraRow = null!;
        public TMP_Text Fight = null!;

        /// <summary>The frame, the aspect list, the inset and the Close button, which grow while the extras show.</summary>
        public RectTransform Frame = null!;
        public RectTransform List = null!;
        public RectTransform Inset = null!;
        public RectTransform Close = null!;

        /// <summary>The inventory's selected-slot frame, laid round a chosen button; null when the game's inventory slot
        /// has none (the chosen label is then coloured instead).</summary>
        public Image? StarFrame;

        /// <summary>The orange of the list's "selected" bar, which the frame is tinted to.</summary>
        public Color StarTint = new Color(1f, 0.64f, 0f, 1f);
    }

    /// <summary>
    /// Builds the altar window from a copy of the game's own compendium window, as the death recap is built
    /// (<see cref="WindowBuilder"/>): the game's wood frame, list, buttons and fonts. One compact panel: the aspect list
    /// on the left, and on the right an inset like the list's holding the boss's name, a row of small star buttons
    /// (copies of the compendium's Close button), what the chosen aspect does, beside a chosen Bountiful a block of the
    /// same buttons for its extras, and what the chosen fight pays; the Close button sits under the inset. Every part has
    /// its own space, so nothing covers anything else (<see cref="Layout"/>). Coordinates are from the frame's top-left
    /// corner.
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
        private const float TextTop = Top + 142f;
        private const float TextHigh = 150f;
        private const float FightHigh = InsetBottom - Top - 308f;

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
            AltarPane pane = Pane(parts, Arrange(parts));
            Layout(pane, 0f);
            AltarWindowView view = root.gameObject.AddComponent<AltarWindowView>();
            view.Init(pane, new ElementList(parts.ListRoot, parts.Element, parts.ListScroll, 28f, 31f), parts.Close);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The right side below the aspect heading, top down: what the aspect does, then beside a chosen Bountiful the
        /// extras' heading and their buttons (<paramref name="extras"/> high; 0 while they do not show), then what the
        /// fight pays. Without the extras every part has its fixed place; with them the description takes the height its
        /// words need, and the frame, the aspect list, the inset and the Close button grow to hold the rest.
        /// </summary>
        public static void Layout(AltarPane pane, float extras)
        {
            float y = TextTop;
            float words = extras <= 0f ? TextHigh
                : Mathf.Max(40f, pane.Description.GetPreferredValues(pane.Description.text, Wide, 0f).y + 4f);
            UiParts.Place(pane.Description.rectTransform, Left, y, Wide, words);
            y += words + 8f;
            if (extras > 0f)
            {
                UiParts.Place(pane.ExtrasTitle.rectTransform, Left, y, Wide, 24f);
                UiParts.Place(pane.ExtraRow, Left, y + 28f, Wide, extras);
                y += 28f + extras + 10f;
            }
            UiParts.Place(pane.Fight.rectTransform, Left, y, Wide, FightHigh);
            Size(pane, Mathf.Max(InsetBottom, y + FightHigh + 8f));
        }

        // The frame, the list beside the inset, the inset and the Close button under it, for an inset ending at
        // insetBottom; at InsetBottom, the window's own size.
        private static void Size(AltarPane pane, float insetBottom)
        {
            float grow = insetBottom - InsetBottom;
            pane.Frame.sizeDelta = new Vector2(Width, Height + grow);
            UiParts.Place(pane.List, 20f, Top, InsetLeft - 36f, Bottom + grow - Top);
            UiParts.Place(pane.Inset, InsetLeft, Top, InsetWide, insetBottom - Top);
            UiParts.Place(pane.Close, InsetLeft + InsetWide - 160f, insetBottom + 10f, 160f, 44f);
        }

        // The inset goes just above the aspect list's background, the buttons close this window, and the compendium's
        // right text area is not used. The sizes are set by Layout, the words as the window opens.
        private static RectTransform Arrange(WindowParts parts)
        {
            Image inset = UiParts.Image(parts.Inset, parts.Frame, "ecr_altar_inset");
            inset.transform.SetSiblingIndex(parts.List.GetSiblingIndex() + 1);
            UiParts.Rewire(parts.Close, AltarWindow.Close);
            UiParts.Rewire(parts.Backdrop, AltarWindow.Close);
            parts.Area.gameObject.SetActive(false);
            return inset.rectTransform;
        }

        private static AltarPane Pane(WindowParts parts, RectTransform inset)
        {
            AltarPane pane = new AltarPane
            {
                Title = parts.Topic,
                Boss = Line(parts, "ecr_altar_boss", Top + 8f, 32f, 24f, false),
                StarsTitle = Line(parts, "ecr_altar_stars", Top + 44f, 24f, 18f, false),
                AspectTitle = Line(parts, "ecr_altar_aspect", Top + 114f, 26f, 20f, false),
                Description = Line(parts, "ecr_altar_text", TextTop, TextHigh, 16f, true),
                ExtrasTitle = Line(parts, "ecr_altar_extras", TextTop, 24f, 18f, false),
                Fight = Line(parts, "ecr_altar_fight", Top + 300f, FightHigh, 16f, true),
                Frame = parts.Frame,
                List = parts.List,
                Inset = inset,
                Close = (RectTransform)parts.Close.transform,
            };
            StarLook(pane, parts);
            pane.StarRow = Row(parts, "ecr_altar_star_row", Top + 70f, 32f);
            pane.ExtraRow = Row(parts, "ecr_altar_extra_row", TextTop, 0f);
            return pane;
        }

        private static RectTransform Row(WindowParts parts, string name, float y, float h)
        {
            RectTransform row = UiParts.Node(name, parts.Frame);
            UiParts.Place(row, Left, y, Wide, h);
            return row;
        }

        // The chosen button's frame: the inventory slot's own, in the orange of the list's "selected" bar.
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
