using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The breadcrumb's parts in a row: folder names in the menu's font (a click is a button click, so a gamepad works
    /// too) with "&gt;" between them, the folder shown last in bold and inert. Parts are made once and reused; a chain
    /// wider than the strip drops parts from the left behind "...", which opens the folder above the first part shown.
    /// </summary>
    public static class BreadcrumbParts
    {
        private const float FontSize = 16f;
        private const float Spacing = 4f;
        private const float Padding = 8f;
        private const string Dots = "...";
        private const string ArrowText = ">";

        private static readonly Color Plain = new Color(0.93f, 0.85f, 0.68f, 1f);
        private static readonly List<FolderTarget> crumbs = new List<FolderTarget>();
        private static readonly List<TMP_Text> arrows = new List<TMP_Text>();
        private static RectTransform row;

        /// <summary>The row the parts sit in, laid out left to right in the middle of the strip.</summary>
        public static void Install(RectTransform strip)
        {
            crumbs.Clear();
            arrows.Clear();
            row = TabLook.Child(strip, "Parts");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = Spacing;
            layout.padding = new RectOffset((int)Padding, (int)Padding, 0, 0);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        /// <summary>Lays out the chain (paths from the top to the folder shown) in the strip's width.</summary>
        public static void Build(List<string> chain, float width)
        {
            if (row == null || TabLook.Font == null)
                return;
            int first = FirstShown(chain, width - 2f * Padding);
            int order = 0, crumb = 0, arrow = 0;
            if (first > 0)
                Place(Crumb(crumb++), Dots, chain[first - 1], false, order++);
            for (int i = first; i < chain.Count; i++)
            {
                if (order > 0)
                    PlaceArrow(ArrowAt(arrow++), order++);
                Place(Crumb(crumb++), FolderPanel.NameOf(chain[i]), chain[i], i == chain.Count - 1, order++);
            }
            Hide(crumbs, crumb);
            for (int i = arrow; i < arrows.Count; i++)
                arrows[i].gameObject.SetActive(false);
        }

        /// <summary>The first part that fits: every part from it on (behind "..." when it is not the first) fits the width.</summary>
        private static int FirstShown(List<string> chain, float width)
        {
            float arrow = Measure(ArrowText) + 2f * Spacing;
            float used = 0f;
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                used += Measure(FolderPanel.NameOf(chain[i])) + (i < chain.Count - 1 ? arrow : 0f);
                float dots = i > 0 ? Measure(Dots) + arrow : 0f;
                if (used + dots > width && i < chain.Count - 1)
                    return i + 1;
            }
            return 0;
        }

        private static void Place(FolderTarget part, string text, string folder, bool current, int order)
        {
            part.Tinted.text = text;
            part.Tinted.fontStyle = current ? FontStyles.Bold : FontStyles.Normal;
            part.Tinted.color = current ? Color.white : Plain;
            part.Set(folder, opens: !current, renames: true);
            part.gameObject.SetActive(true);
            part.transform.SetSiblingIndex(order);
        }

        private static void PlaceArrow(TMP_Text text, int order)
        {
            text.gameObject.SetActive(true);
            text.transform.SetSiblingIndex(order);
        }

        private static float Measure(string text) => Crumb(0).Tinted.GetPreferredValues(text).x;

        private static FolderTarget Crumb(int index)
        {
            while (crumbs.Count <= index)
                crumbs.Add(MakeCrumb());
            return crumbs[index];
        }

        private static TMP_Text ArrowAt(int index)
        {
            while (arrows.Count <= index)
                arrows.Add(MakeArrow());
            return arrows[index];
        }

        private static TMP_Text MakeArrow()
        {
            TMP_Text text = Text("Arrow", Plain, raycast: false);
            text.text = ArrowText;
            return text;
        }

        /// <summary>A part: its text, a button without a look of its own (the text is underlined under the mouse) and its target.</summary>
        private static FolderTarget MakeCrumb()
        {
            TMP_Text text = Text("Crumb", Plain, raycast: true);
            Button button = text.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = text;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            FolderTarget target = text.gameObject.AddComponent<FolderTarget>();
            target.Tinted = text;
            button.onClick.AddListener(target.Activate);
            return target;
        }

        private static TMP_Text Text(string name, Color color, bool raycast)
        {
            TMP_Text text = TabLook.Text(row, name, FontSize);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Left;
            text.color = color;
            text.raycastTarget = raycast;
            return text;
        }

        private static void Hide(List<FolderTarget> parts, int from)
        {
            for (int i = from; i < parts.Count; i++)
                parts[i].gameObject.SetActive(false);
        }
    }
}
