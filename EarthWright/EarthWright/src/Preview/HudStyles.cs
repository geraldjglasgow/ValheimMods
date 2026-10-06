using System.Text.RegularExpressions;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The IMGUI text styles of the HUD (built inside OnGUI, where the skin exists, and again when the scale setting
    /// changes; they wrap within the rectangle they are given) and a shadowed label: the text is drawn once in black, one pixel off, then in colour, so it stays
    /// readable on snow and sand. Colour tags are removed from the shadow pass so the shadow stays black; the caller
    /// keeps the shadow text of each line (<see cref="Plain"/>), since the same lines are drawn frame after frame.
    /// </summary>
    internal static class HudStyles
    {
        private static readonly Regex ColourTags = new Regex("</?color[^>]*>", RegexOptions.Compiled);
        private static readonly GUIContent measure = new GUIContent();

        private static float builtScale = -1f;

        public static GUIStyle Line { get; private set; }
        public static GUIStyle Shadow { get; private set; }
        public static GUIStyle Badge { get; private set; }
        public static GUIStyle BadgeShadow { get; private set; }

        public static void Ensure(float scale)
        {
            if (Line != null && Mathf.Abs(scale - builtScale) < 0.001f)
                return;
            builtScale = scale;
            int size = Mathf.RoundToInt(15f * scale);
            Line = Make(size, TextAnchor.UpperLeft, new Color(1f, 0.96f, 0.88f));
            Shadow = Make(size, TextAnchor.UpperLeft, new Color(0f, 0f, 0f, 0.85f));
            Badge = Make(Mathf.RoundToInt(12f * scale), TextAnchor.UpperLeft, new Color(1f, 0.72f, 0.3f));
            BadgeShadow = Make(Mathf.RoundToInt(12f * scale), TextAnchor.UpperLeft, new Color(0f, 0f, 0f, 0.85f));
        }

        private static GUIStyle Make(int size, TextAnchor anchor, Color colour)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size, alignment = anchor, richText = true, wordWrap = true, clipping = TextClipping.Overflow,
            };
            style.normal.textColor = colour;
            return style;
        }

        /// <summary>A label with a one pixel (times scale) black shadow.</summary>
        public static void Shadowed(Rect rect, string text, string shadowText, GUIStyle style, GUIStyle shadow, float scale)
        {
            float offset = Mathf.Max(1f, scale);
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), shadowText, shadow);
            GUI.Label(rect, text, style);
        }

        /// <summary>The height a style needs for the text wrapped at the width (one shared content object, no garbage).</summary>
        public static float Height(GUIStyle style, string text, float width)
        {
            measure.text = text;
            return style.CalcHeight(measure, width);
        }

        /// <summary>The text without its colour tags, for the shadow pass. Call it when the text changes, not per repaint.</summary>
        public static string Plain(string text) => text.IndexOf('<') < 0 ? text : ColourTags.Replace(text, "");
    }
}
