using System.Collections.Generic;
using System.Globalization;
using EarthWright.Core;
using Translations;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// IMGUI building blocks of the panel: a typed number field that keeps what the player is typing while it has the
    /// keyboard (and otherwise shows the live value, so re-clamping by the brush shows at once), choice grids with
    /// localized captions, headings, and the panel's opaque window style. Labels and captions are $tokens (or plain text),
    /// localized once per language and kept, since the panel is drawn several times a frame.
    /// </summary>
    internal static class PanelFields
    {
        public const float LabelWidth = 170f;
        private const float FieldWidth = 90f;

        private static readonly Dictionary<string, string> typing = new Dictionary<string, string>();
        private static readonly Dictionary<string, string[]> captions = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>();
        private static int wordsFor = -1;
        private static GUIStyle window;
        private static GUIStyle heading;
        private static Texture2D background;

        /// <summary>A number field; true (with the typed number) only in the pass the player changed the text to a number.</summary>
        public static bool Float(string id, string labelToken, float value, out float typed, string format = "0.###")
        {
            string name = "ew_panel_" + id;
            bool focused = GUI.GetNameOfFocusedControl() == name;
            string shown = focused && typing.TryGetValue(name, out string text) ? text : value.ToString(format, CultureInfo.InvariantCulture);
            if (!focused)
                typing.Remove(name);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Text(labelToken), GUILayout.Width(LabelWidth));
            GUI.SetNextControlName(name);
            string edited = GUILayout.TextField(shown, GUILayout.Width(FieldWidth));
            GUILayout.EndHorizontal();
            typed = value;
            if (edited == shown)
                return false;
            typing[name] = edited;
            return TryParse(edited, out typed);
        }

        /// <summary>A number shown and typed in percent, handed back as 0..1.</summary>
        public static bool Percent(string id, string labelToken, float value, out float typed)
        {
            bool changed = Float(id, labelToken, value * 100f, out float percent, "0.#");
            typed = percent / 100f;
            return changed;
        }

        private static bool TryParse(string text, out float value)
        {
            return float.TryParse(text.Replace(',', '.').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>A labelled grid of choices; returns the selected index.</summary>
        public static int Choice(string labelToken, int selected, string[] captionTokens, int columns)
        {
            GUILayout.Label(Text(labelToken));
            return GUILayout.SelectionGrid(selected, Localized(captionTokens), columns);
        }

        /// <summary>The captions localized once per set and language.</summary>
        private static string[] Localized(string[] tokens)
        {
            Forget();
            string key = string.Join("|", tokens);
            if (!captions.TryGetValue(key, out string[] shown) || Localization.m_instance == null)
            {
                shown = new string[tokens.Length];
                for (int i = 0; i < tokens.Length; i++)
                    shown[i] = TokenText.Localize(tokens[i]);
                if (Localization.m_instance != null)
                    captions[key] = shown;
            }
            return shown;
        }

        /// <summary>A label in the current language, localized once per language.</summary>
        private static string Text(string token)
        {
            Forget();
            if (!words.TryGetValue(token, out string text) || Localization.m_instance == null)
            {
                text = TokenText.Localize(token);
                if (Localization.m_instance != null)
                    words[token] = text;
            }
            return text;
        }

        /// <summary>Drops the localized labels and captions after a change of language.</summary>
        private static void Forget()
        {
            if (wordsFor == Lang.Version)
                return;
            wordsFor = Lang.Version;
            words.Clear();
            captions.Clear();
        }

        public static void Heading(string token)
        {
            GUILayout.Space(6f);
            GUILayout.Label(Text(token), HeadingStyle);
        }

        public static GUIStyle HeadingStyle => heading ?? (heading = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true });

        /// <summary>The skin's window with an opaque dark background, readable over bright snow.</summary>
        public static GUIStyle WindowStyle
        {
            get
            {
                if (window != null && background != null)
                    return window;
                background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                background.SetPixel(0, 0, new Color(0.09f, 0.08f, 0.07f, 0.94f));
                background.Apply();
                window = new GUIStyle(GUI.skin.window) { richText = true };
                window.normal.background = background;
                window.onNormal.background = background;
                return window;
            }
        }
    }
}
