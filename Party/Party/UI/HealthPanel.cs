using UnityEngine;
using Party.Client;

namespace Party.UI
{
    /// <summary>The draggable party health panel. Drag works in edit mode, or whenever the cursor is already free (inventory, map).</summary>
    public static class HealthPanel
    {
        private static readonly int WindowId = "Party.HealthPanel".GetStableHashCode();

        public static bool EditMode { get; private set; }
        private static bool wasDragging;

        public static void ToggleEditMode(bool on)
        {
            EditMode = on;
            if (!on)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Re-asserts the free cursor every frame - the game's own controller re-locks it otherwise.</summary>
        public static void EnforceCursor()
        {
            if (!EditMode)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Draw()
        {
            if (!PartyClientState.InParty && !EditMode)
                return;
            EnsurePosition();
            float scale = Mathf.Max(0.5f, PartyConfig.PanelScale.Value);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), new Vector2(PartyConfig.PanelX.Value, PartyConfig.PanelY.Value));
            Rect rect = new Rect(PartyConfig.PanelX.Value, PartyConfig.PanelY.Value, PanelWidth(), PanelHeight());
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, PartyConfig.PanelOpacity.Value);
            rect = GUI.Window(WindowId, rect, DrawWindow, CanDrag() ? Title() + " (drag me)" : Title(), TitleStyle());
            GUI.color = previous;
            HandleDrag(rect);
        }

        private static string Title() => PartyClientState.Name.Length > 0 ? PartyClientState.Name : "Party";

        private static GUIStyle TitleStyle() => new GUIStyle(GUI.skin.window) { fontSize = PartyConfig.TitleFontSize.Value };

        /// <summary>First-run default: upper-middle of the screen, clear of the hotbar, independent of resolution.</summary>
        private static void EnsurePosition()
        {
            if (PartyConfig.PanelY.Value < 0f)
                PartyConfig.PanelY.Value = Screen.height * 0.35f;
        }

        private static bool CanDrag() => EditMode || Cursor.lockState == CursorLockMode.None;

        private static void DrawWindow(int id)
        {
            float y = PartyConfig.TitleFontSize.Value + PartyConfig.PanelPadding.Value * 0.5f;
            if (PartyConfig.ShowOwnRow.Value)
                y = HealthPanelRow.Draw(SelfRow(), y, null);
            Vector3 localPos = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == Identity.LocalPlayerId)
                    continue;
                float? distance = member.PositionValid ? Vector3.Distance(localPos, member.Position) : (float?)null;
                y = HealthPanelRow.Draw(member, y, distance);
            }
            if (CanDrag())
                GUI.DragWindow();
        }

        private static PartyMemberView SelfRow()
        {
            Player local = Player.m_localPlayer;
            return new PartyMemberView
            {
                Id = Identity.LocalPlayerId,
                Name = Identity.LocalPlayerName,
                Online = true,
                Health = local != null && local.GetMaxHealth() > 0 ? local.GetHealth() / local.GetMaxHealth() : 1f,
                Stamina = local != null && local.GetMaxStamina() > 0 ? local.GetStamina() / local.GetMaxStamina() : 1f,
                Eitr = local != null && local.GetMaxEitr() > 0 ? local.GetEitr() / local.GetMaxEitr() : 1f,
            };
        }

        private static void HandleDrag(Rect rect)
        {
            if (!CanDrag())
                return;
            bool dragging = Event.current.type == EventType.MouseDrag;
            if (wasDragging && !dragging)
            {
                PartyConfig.PanelX.Value = rect.x;
                PartyConfig.PanelY.Value = rect.y;
            }
            wasDragging = dragging;
        }

        private static float PanelWidth() => PartyConfig.BarWidth.Value + PartyConfig.PanelPadding.Value * 2f;

        private static float PanelHeight()
        {
            int otherCount = System.Math.Max(0, PartyClientState.Members.Count - 1);
            int rows = (PartyConfig.ShowOwnRow.Value ? 1 : 0) + otherCount;
            float rowHeight = HealthPanelRow.RowHeight();
            float titleHeight = PartyConfig.TitleFontSize.Value + PartyConfig.PanelPadding.Value;
            return titleHeight + rows * (rowHeight + PartyConfig.RowSpacing.Value) + PartyConfig.PanelPadding.Value;
        }
    }
}
