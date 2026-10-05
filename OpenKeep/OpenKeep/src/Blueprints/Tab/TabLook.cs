using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The look shared by the Blueprints tab's own UI parts (name bands, selection marks, the selection box, the drag
    /// ghost): the build menu's own TMP font, taken from its tab labels when the menu wakes, with one shared outlined
    /// material for every label; colours; and small builders for images and frames that never catch the mouse.
    /// </summary>
    public static class TabLook
    {
        public static readonly Color Band = new Color(0.08f, 0.05f, 0.02f, 0.78f);
        public static readonly Color Picked = new Color(1f, 0.78f, 0.25f, 1f);
        public static readonly Color Target = new Color(0.45f, 1f, 0.35f, 1f);
        public static readonly Color Box = new Color(1f, 0.85f, 0.4f, 1f);

        /// <summary>The menu's font, or null when the menu had no text to take it from (labels are then left out).</summary>
        public static TMP_FontAsset Font { get; private set; }

        private static Material source;
        private static Material outlined;

        /// <summary>BuildUi.Awake: the font and material of the menu's first label.</summary>
        public static void Capture(BuildUi ui)
        {
            TMP_Text text = ui.GetComponentInChildren<TMP_Text>(true);
            if (text == null || text.font == null)
                return;
            Font = text.font;
            source = text.fontSharedMaterial != null ? text.fontSharedMaterial : text.font.material;
        }

        /// <summary>The label material: the menu's font material with a black outline, made once.</summary>
        public static Material Outlined()
        {
            if (outlined != null || source == null)
                return outlined != null ? outlined : source;
            outlined = new Material(source) { name = "OpenKeep Label Outline" };
            outlined.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            outlined.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            return outlined;
        }

        /// <summary>A text in the menu's font that never catches the mouse, filling its parent.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, float size)
        {
            RectTransform rect = Child(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSharedMaterial = Outlined();
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>An image that never catches the mouse, filling its parent.</summary>
        public static Image Image(Transform parent, string name, Color color)
        {
            Image image = Child(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A frame: a faint fill and four 2 px edges, all in one colour (see <see cref="Paint"/>).</summary>
        public static RectTransform Frame(Transform parent, string name)
        {
            RectTransform frame = Child(parent, name);
            Image(frame, "Fill", Color.clear);
            Edge(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f));
            Edge(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f));
            Edge(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f));
            Edge(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f));
            return frame;
        }

        /// <summary>Colours a frame: the edges in the colour, the fill in a fifth of its strength.</summary>
        public static void Paint(Transform frame, Color color)
        {
            foreach (Image image in frame.GetComponentsInChildren<Image>(true))
                image.color = image.name == "Fill" ? new Color(color.r, color.g, color.b, 0.22f) : color;
        }

        private static void Edge(Transform frame, string name, Vector2 min, Vector2 max)
        {
            RectTransform edge = Image(frame, name, Color.clear).rectTransform;
            edge.anchorMin = min;
            edge.anchorMax = max;
            edge.pivot = new Vector2(min.x == max.x ? min.x : 0.5f, min.y == max.y ? min.y : 0.5f);
            edge.sizeDelta = new Vector2(min.x == max.x ? 2f : 0f, min.y == max.y ? 2f : 0f);
        }

        /// <summary>A UI child stretched over its parent.</summary>
        public static RectTransform Child(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }
    }
}
