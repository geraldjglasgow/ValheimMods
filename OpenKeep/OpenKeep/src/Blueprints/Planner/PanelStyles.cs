using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The queue panel's looks, made on first use inside OnGUI (the skin exists only there): the skin's window with an
    /// opaque dark background, readable over bright snow, and rich-text labels in white, grey and a bold title; entry
    /// names stay on one line, cut at the edge.
    /// </summary>
    public static class PanelStyles
    {
        private static GUIStyle window;
        private static GUIStyle text;
        private static GUIStyle grey;
        private static GUIStyle title;
        private static GUIStyle name;
        private static Texture2D background;

        public static GUIStyle Window => Made(ref window);

        public static GUIStyle Text => Made(ref text);

        public static GUIStyle Grey => Made(ref grey);

        public static GUIStyle Title => Made(ref title);

        public static GUIStyle Name => Made(ref name);

        private static GUIStyle Made(ref GUIStyle style)
        {
            if (window == null || background == null)
                Make();
            return style;
        }

        private static void Make()
        {
            background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            background.SetPixel(0, 0, new Color(0.09f, 0.08f, 0.07f, 0.94f));
            background.Apply();
            window = new GUIStyle(GUI.skin.window) { richText = true };
            window.normal.background = background;
            window.onNormal.background = background;
            text = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 14 };
            text.normal.textColor = Color.white;
            grey = new GUIStyle(text);
            grey.normal.textColor = new Color(0.78f, 0.78f, 0.78f);
            title = new GUIStyle(text) { fontSize = 16, fontStyle = FontStyle.Bold };
            name = new GUIStyle(text) { wordWrap = false, clipping = TextClipping.Clip, fontStyle = FontStyle.Bold };
        }
    }
}
