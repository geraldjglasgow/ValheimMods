using EliteCreaturesReborn.Recap.Window;
using TMPro;
using UnityEngine;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>What the right side of the altar window is made of, for the view to drive.</summary>
    internal sealed class AltarPane
    {
        public TMP_Text Title = null!;
        public TMP_Text NowTitle = null!;
        public TMP_Text Boss = null!;
        public TMP_Text StarsTitle = null!;
        public RectTransform StarRow = null!;
        public TMP_Text AspectTitle = null!;
        public TMP_Text Description = null!;
        public TMP_Text Now = null!;
    }

    /// <summary>
    /// Builds the altar window from a copy of the game's own compendium window, as the death recap is built
    /// (<see cref="WindowBuilder"/>): the game's wood frame, list, buttons and fonts. The list on the left holds the
    /// aspects, the right side the boss's name, a row of star buttons (copies of the compendium's Close button), what the
    /// chosen aspect does, and what the altar shows right now. Coordinates are from the frame's top-left corner.
    /// </summary>
    internal static class AltarWindowBuilder
    {
        public const float Width = 900f;
        public const float Height = 600f;
        public const float Left = 320f;
        public const float Wide = 560f;

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
            view.Init(Pane(parts), new ElementList(parts.ListRoot, parts.Element, parts.ListScroll, 36f, 40f), parts.Close);
            root.gameObject.SetActive(false);
            return view;
        }

        // The frame shrinks to the picker's size; the aspect list keeps the left, the close button sits under it, and the
        // right text area is not used: the right side's texts are placed on the frame. The words are written as it opens.
        private static void Arrange(WindowParts parts)
        {
            parts.Frame.sizeDelta = new Vector2(Width, Height);
            UiParts.Place(parts.List, 20f, 51f, 280f, 470f);
            UiParts.Place((RectTransform)parts.Close.transform, 60f, 535f, 200f, 46f);
            UiParts.Rewire(parts.Close, AltarWindow.Close);
            UiParts.Rewire(parts.Backdrop, AltarWindow.Close);
            parts.Area.gameObject.SetActive(false);
        }

        private static AltarPane Pane(WindowParts parts)
        {
            AltarPane pane = new AltarPane
            {
                Title = parts.Topic,
                Boss = Line(parts, "ecr_altar_boss", 50f, 34f, 24f, false),
                StarsTitle = Line(parts, "ecr_altar_stars", 94f, 26f, 18f, false),
                AspectTitle = Line(parts, "ecr_altar_aspect", 186f, 28f, 20f, false),
                Description = Line(parts, "ecr_altar_text", 218f, 112f, 16f, true),
                Now = Line(parts, "ecr_altar_now", 376f, 200f, 16f, true),
                NowTitle = Line(parts, "ecr_altar_now_title", 344f, 26f, 18f, false),
            };
            pane.StarRow = UiParts.Node("ecr_altar_star_row", parts.Frame);
            UiParts.Place(pane.StarRow, Left, 124f, Wide, 40f);
            return pane;
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
