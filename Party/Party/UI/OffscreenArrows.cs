using UnityEngine;
using Party.Client;

namespace Party.UI
{
    /// <summary>Edge-of-screen arrows pointing at party members who are off camera.</summary>
    public static class OffscreenArrows
    {
        private const float Margin = 40f;

        public static void Draw()
        {
            if (!PartyClientState.InParty)
                return;
            Camera cam = Utils.GetMainCamera();
            if (cam == null)
                return;
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == Identity.LocalPlayerId || !member.Online || !member.PositionValid)
                    continue;
                DrawFor(cam, member);
            }
        }

        private static void DrawFor(Camera cam, PartyMemberView member)
        {
            Vector3 screen = cam.WorldToScreenPoint(member.Position);
            bool behind = screen.z < 0f;
            if (behind)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }
            Vector2 guiPoint = new Vector2(screen.x, Screen.height - screen.y);
            if (!behind && OnScreen(screen))
                return;
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 dir = (guiPoint - center).normalized;
            DrawArrow(center + dir * ClampScale(center, dir), dir, member);
        }

        private static bool OnScreen(Vector3 screen) =>
            screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;

        private static float ClampScale(Vector2 center, Vector2 dir)
        {
            float halfW = center.x - Margin;
            float halfH = center.y - Margin;
            float scaleX = dir.x != 0f ? Mathf.Abs(halfW / dir.x) : float.MaxValue;
            float scaleY = dir.y != 0f ? Mathf.Abs(halfH / dir.y) : float.MaxValue;
            return Mathf.Min(scaleX, scaleY);
        }

        private static void DrawArrow(Vector2 pos, Vector2 dir, PartyMemberView member)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, pos);
            GUI.color = member.IsLeader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : ColorHelper.Parse(PartyConfig.PartyColor.Value);
            GUI.DrawTexture(new Rect(pos.x - 4f, pos.y - 10f, 8f, 20f), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = Color.white;
            GUI.Label(new Rect(pos.x - 40f, pos.y + 12f, 80f, 16f), member.Name, LabelStyle());
            GUI.color = previousColor;
        }

        private static GUIStyle LabelStyle() => new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
    }
}
