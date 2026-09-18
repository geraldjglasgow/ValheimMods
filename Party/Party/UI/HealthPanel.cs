using UnityEngine;
using Party.Client;

namespace Party.UI
{
    /// <summary>The draggable party health panel. Drag only works in edit mode (<c>/party panel edit</c>).</summary>
    public static class HealthPanel
    {
        private const int WindowId = 0x50617274;

        public static bool EditMode { get; private set; }
        private static bool wasDragging;

        public static void ToggleEditMode(bool on)
        {
            EditMode = on;
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = on;
        }

        public static void Draw()
        {
            if (!PartyClientState.InParty && !EditMode)
                return;
            float scale = Mathf.Max(0.5f, PartyConfig.PanelScale.Value);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), new Vector2(PartyConfig.PanelX.Value, PartyConfig.PanelY.Value));
            Rect rect = new Rect(PartyConfig.PanelX.Value, PartyConfig.PanelY.Value, PanelWidth(), PanelHeight());
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, PartyConfig.PanelOpacity.Value);
            rect = GUI.Window(WindowId, rect, DrawWindow, EditMode ? "Party (drag me - /party panel done to finish)" : Title());
            GUI.color = previous;
            HandleDrag(rect);
        }

        private static string Title() => PartyClientState.Name.Length > 0 ? PartyClientState.Name : "Party";

        private static void DrawWindow(int id)
        {
            float y = 20f;
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
            if (EditMode)
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
            if (!EditMode)
                return;
            bool dragging = Event.current.type == EventType.MouseDrag;
            if (wasDragging && !dragging)
            {
                PartyConfig.PanelX.Value = rect.x;
                PartyConfig.PanelY.Value = rect.y;
            }
            wasDragging = dragging;
        }

        private static float PanelWidth() => PartyConfig.BarWidth.Value + 90f;

        private static float PanelHeight()
        {
            int otherCount = System.Math.Max(0, PartyClientState.Members.Count - 1);
            int rows = (PartyConfig.ShowOwnRow.Value ? 1 : 0) + otherCount;
            float rowHeight = HealthPanelRow.RowHeight();
            return 24f + rows * (rowHeight + PartyConfig.RowSpacing.Value);
        }
    }
}
