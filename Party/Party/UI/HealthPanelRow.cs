using UnityEngine;
using Party.Client;

namespace Party.UI
{
    /// <summary>Draws one member's name and bars, and reports how tall a row is so the panel can size itself.</summary>
    public static class HealthPanelRow
    {
        private static readonly Color HealthColor = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color StaminaColor = new Color(0.15f, 0.7f, 0.15f);
        private static readonly Color EitrColor = new Color(0.2f, 0.5f, 0.9f);

        public static float RowHeight()
        {
            float height = PartyConfig.BarHeight.Value;
            if (PartyConfig.ShowStamina.Value)
                height += PartyConfig.BarHeight.Value * 0.6f + 2f;
            if (PartyConfig.ShowEitr.Value)
                height += PartyConfig.BarHeight.Value * 0.6f + 2f;
            return Mathf.Max(height, PartyConfig.FontSize.Value + 2f);
        }

        public static float Draw(PartyMemberView member, float y)
        {
            Color previous = GUI.color;
            GUI.color = member.Online ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
            DrawName(member, y);
            float barY = y + PartyConfig.FontSize.Value + 2f;
            barY = DrawBar(barY, member.Online ? member.Health : 0f, HealthColor);
            if (PartyConfig.ShowStamina.Value)
                barY = DrawBar(barY, member.Online ? member.Stamina : 0f, StaminaColor, 0.6f);
            if (PartyConfig.ShowEitr.Value)
                barY = DrawBar(barY, member.Online ? member.Eitr : 0f, EitrColor, 0.6f);
            GUI.color = previous;
            return barY + PartyConfig.RowSpacing.Value;
        }

        private static void DrawName(PartyMemberView member, float y)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = PartyConfig.FontSize.Value };
            bool leader = member.IsLeader;
            style.normal.textColor = leader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : Color.white;
            style.fontStyle = leader ? FontStyle.Bold : FontStyle.Normal;
            string label = (leader ? "* " : "") + member.Name;
            GUI.Label(new Rect(8, y, PartyConfig.BarWidth.Value + 60f, PartyConfig.FontSize.Value + 4f), label, style);
        }

        private static float DrawBar(float y, float fraction, Color color, float heightScale = 1f)
        {
            float height = PartyConfig.BarHeight.Value * heightScale;
            Rect background = new Rect(8, y, PartyConfig.BarWidth.Value, height);
            GUI.DrawTexture(background, Texture2D.blackTexture);
            Rect fill = new Rect(background.x, background.y, background.width * Mathf.Clamp01(fraction), height);
            Color previous = GUI.color;
            GUI.color = color * previous;
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = previous;
            return y + height + 2f;
        }
    }
}
