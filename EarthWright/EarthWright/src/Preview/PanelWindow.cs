using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The EarthWright panel (F6 or the Esc menu's EarthWright button): a draggable IMGUI window with the brush values,
    /// the presets and every module's section (<see cref="PanelSections"/>). While it is open the cursor is free and
    /// the game ignores clicks and mouse look the way it does for its own windows (see <see cref="PanelInputPatches"/>);
    /// Esc closes it. Its position is kept in a local setting, written when a drag ends.
    /// </summary>
    internal static class PanelWindow
    {
        private const int WindowId = 0x45575031;
        private const float Width = 440f;
        private const float TitleHeight = 22f;

        private static Rect rect;
        private static Vector2 scroll;
        private static bool placed;

        public static bool IsOpen { get; private set; }

        /// <summary>A text field of the panel has the keyboard (updated every GUI pass).</summary>
        public static bool Typing { get; private set; }

        public static void Update()
        {
            if (IsOpen && Player.m_localPlayer == null)
                Close();
            if (Player.m_localPlayer != null && Keys.Pressed(HudSettings.PanelKey))
                Toggle();
        }

        public static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public static void Open()
        {
            IsOpen = true;
            placed = false;
        }

        public static void Close()
        {
            if (IsOpen)
                SavePosition();
            IsOpen = false;
        }

        public static void OnGui()
        {
            if (!IsOpen || global::Menu.IsVisible())
            {
                ReleaseKeyboard();
                return;
            }
            float scale = HudSettings.PanelScale.Value;
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Place(scale);
            rect = GUILayout.Window(WindowId, rect, Draw, Language.Localize("$ew_preview_panel_title"), PanelFields.WindowStyle, GUILayout.Width(Width));
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Screen.width / scale - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Screen.height / scale - TitleHeight));
            GUI.matrix = matrix;
            Typing = GUIUtility.keyboardControl != 0;
            if (Event.current.rawType == EventType.MouseUp)
                SavePosition();
        }

        private static void ReleaseKeyboard()
        {
            if (!Typing)
                return;
            Typing = false;
            GUIUtility.keyboardControl = 0;
        }

        /// <summary>Takes the saved position (screen pixels) the first time the window is drawn after opening.</summary>
        private static void Place(float scale)
        {
            if (placed)
                return;
            placed = true;
            Vector2 saved = HudSettings.PanelPosition.Value;
            rect = new Rect(saved.x / scale, saved.y / scale, Width, 0f);
        }

        private static void SavePosition()
        {
            Vector2 position = rect.position * HudSettings.PanelScale.Value;
            if ((position - HudSettings.PanelPosition.Value).sqrMagnitude > 1f)
                HudSettings.PanelPosition.Value = position;
        }

        private static void Draw(int id)
        {
            EndTypingOnEnter();
            Header();
            float maxHeight = Screen.height / HudSettings.PanelScale.Value * 0.8f;
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(maxHeight));
            Safe.Run("EarthWright panel brush", PanelBrush.Draw);
            Safe.Run("EarthWright panel presets", PanelPresets.Draw);
            foreach (PanelSections.Section section in PanelSections.Visible())
            {
                PanelFields.Heading(section.Title);
                if (section.Draw != null)
                    Safe.Run("EarthWright panel section " + section.Title, section.Draw);
            }
            GUILayout.EndScrollView();
            // A click on the window's background (no control took it) ends typing, as Enter does.
            if (Event.current.type == EventType.MouseDown)
                GUIUtility.keyboardControl = 0;
            GUI.DragWindow(new Rect(0f, 0f, 10000f, TitleHeight));
        }

        private static void EndTypingOnEnter()
        {
            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            if (!enter || GUIUtility.keyboardControl == 0)
                return;
            GUIUtility.keyboardControl = 0;
            e.Use();
        }

        private static void Header()
        {
            GUILayout.BeginHorizontal();
            if (Plugin.Synced != null && Plugin.Synced.IsLocked)
                GUILayout.Label("<color=#ffb84d>" + Language.Localize("$ew_preview_locked") + "</color>", PanelFields.HeadingStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Language.Localize("$ew_preview_close"), GUILayout.Width(80f)))
                Close();
            GUILayout.EndHorizontal();
        }
    }
}
