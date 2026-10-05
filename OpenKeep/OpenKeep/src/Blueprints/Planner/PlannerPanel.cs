using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The queue panel (K): an IMGUI window docked at the right edge, drawn while it is open, the Site planner is active
    /// and no game window is up. It shows the panel's site (the selection's, else the nearest within 60 m, set by
    /// <see cref="PlannerSession"/>): its name and progress, then the queue, each entry with its sizes, the materials
    /// its unbuilt pieces still need and Up / Down / Remove; the entry under the mouse is reported for its glow. While
    /// it shows, the game treats it as one of its own windows (WindowInput): free cursor, no attacks or mouse look,
    /// Esc closes it. Scaled with the screen height.
    /// </summary>
    public static class PlannerPanel
    {
        public const float SiteRange = 60f;
        private const int WindowId = 0x4F4B5150;
        private const float Width = 380f;
        private const float Margin = 16f;
        private const float ButtonWidth = 56f;
        private const float RemoveWidth = 70f;

        private static bool open;
        private static SiteMarker site;
        private static Vector2 scroll;
        private static int hoveredRow = -1;

        public static bool Showing => open && PlannerSession.Active && NoGameWindow;

        private static bool NoGameWindow => Hud.instance != null && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !global::Menu.IsVisible();

        /// <summary>The queue row under the mouse when the panel was last drawn, or -1.</summary>
        public static int HoveredRow => Showing ? hoveredRow : -1;

        public static void Toggle()
        {
            open = !open;
            hoveredRow = -1;
        }

        public static void Close()
        {
            open = false;
            hoveredRow = -1;
        }

        /// <summary>Per frame from the session: the site the panel shows, or null.</summary>
        public static void SetSite(SiteMarker shown) => site = shown;

        public static void OnGui()
        {
            if (!Showing)
            {
                hoveredRow = -1;
                return;
            }
            if (Event.current.type == EventType.Layout)
                PanelModel.Refresh(site);
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Rect rect = Docked(scale);
            GUILayout.Window(WindowId, rect, Draw, Language.Localize(PlannerWords.PanelTitle), PanelStyles.Window,
                GUILayout.Width(rect.width), GUILayout.Height(rect.height));
            GUI.matrix = matrix;
        }

        /// <summary>At the right edge, from a quarter of the way down (below the minimap) to near the bottom.</summary>
        private static Rect Docked(float scale)
        {
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            return new Rect(width - Width - Margin, height * 0.25f, Width, height * 0.62f);
        }

        private static void Draw(int id)
        {
            if (PanelModel.Site == null)
                GUILayout.Label(BlueprintWords.Format(PlannerWords.PanelNoSite, SiteRange), PanelStyles.Grey);
            else
                DrawSite();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Language.Localize(PlannerWords.PanelKeys), PanelStyles.Grey);
        }

        private static void DrawSite()
        {
            GUILayout.Label(PanelModel.Title, PanelStyles.Title);
            GUILayout.Label(PanelModel.Progress, PanelStyles.Text);
            if (PanelModel.Rows.Count == 0)
            {
                GUILayout.Label(Language.Localize(PlannerWords.PanelEmpty), PanelStyles.Grey);
                hoveredRow = -1;
                return;
            }
            DrawRows();
        }

        /// <summary>The rows in a scroll view; on the repaint pass, the row under the mouse when the mouse is inside the view.</summary>
        private static void DrawRows()
        {
            Vector2 mouse = Event.current.mousePosition;
            int under = -1;
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < PanelModel.Rows.Count; i++)
            {
                if (DrawRow(i))
                    under = i;
            }
            GUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint)
                hoveredRow = GUILayoutUtility.GetLastRect().Contains(mouse) ? under : -1;
        }

        /// <summary>One entry: its place and name with the buttons, then its sizes and materials. True on repaint when the mouse is over it.</summary>
        private static bool DrawRow(int i)
        {
            PanelRow row = PanelModel.Rows[i];
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label((i + 1) + ". " + row.Name, PanelStyles.Name, GUILayout.MinWidth(0f), GUILayout.ExpandWidth(true));
            Buttons(i, row);
            GUILayout.EndHorizontal();
            GUILayout.Label(row.Materials.Length > 0 ? row.Sizes + ": " + row.Materials : row.Sizes, PanelStyles.Grey);
            GUILayout.EndVertical();
            return Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
        }

        private static void Buttons(int i, PanelRow row)
        {
            SiteMarker shown = PanelModel.Site;
            GUI.enabled = i > 0;
            if (GUILayout.Button(Language.Localize(PlannerWords.Up), GUILayout.Width(ButtonWidth)))
                QueueEdits.Move(shown, i, -1, row.Name);
            GUI.enabled = i < PanelModel.Rows.Count - 1;
            if (GUILayout.Button(Language.Localize(PlannerWords.Down), GUILayout.Width(ButtonWidth)))
                QueueEdits.Move(shown, i, 1, row.Name);
            GUI.enabled = true;
            if (GUILayout.Button(Language.Localize(PlannerWords.Remove), GUILayout.Width(RemoveWidth)))
                QueueEdits.Remove(shown, i, row.Name);
        }
    }
}
