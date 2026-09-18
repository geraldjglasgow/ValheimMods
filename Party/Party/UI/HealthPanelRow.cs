using UnityEngine;
using Party.Client;

namespace Party.UI
{
    /// <summary>Draws one member's name and bars, and reports how tall a row is so the panel can size itself.</summary>
    public static class HealthPanelRow
    {
        private static readonly Color BarBackground = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color HealthColor = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color StaminaColor = new Color(0.15f, 0.7f, 0.15f);
        private static readonly Color EitrColor = new Color(0.2f, 0.5f, 0.9f);

        public static float RowHeight()
        {
            float height = PartyConfig.FontSize.Value + 4f + PartyConfig.BarHeight.Value;
            if (PartyConfig.ShowStamina.Value)
                height += PartyConfig.BarHeight.Value * 0.6f + 4f;
            if (PartyConfig.ShowEitr.Value)
                height += PartyConfig.BarHeight.Value * 0.6f + 4f;
            return height;
        }

        public static float Draw(PartyMemberView member, float y, float? distance = null)
        {
            float x = PartyConfig.PanelPadding.Value;
            float width = PartyConfig.BarWidth.Value;
            Color previous = GUI.color;
            GUI.color = member.Online ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
            DrawName(member, x, y, width);
            if (distance.HasValue)
                DrawDistance(distance.Value, x, y, width);
            float barY = y + PartyConfig.FontSize.Value + 4f;
            barY = DrawBar(x, barY, width, member.Online ? member.Health : 0f, HealthColor);
            if (PartyConfig.ShowStamina.Value)
                barY = DrawBar(x, barY, width, member.Online ? member.Stamina : 0f, StaminaColor, 0.6f);
            if (PartyConfig.ShowEitr.Value)
                barY = DrawBar(x, barY, width, member.Online ? member.Eitr : 0f, EitrColor, 0.6f);
            GUI.color = previous;
            return barY + PartyConfig.RowSpacing.Value;
        }

        private static void DrawName(PartyMemberView member, float x, float y, float width)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = PartyConfig.FontSize.Value };
            bool leader = member.IsLeader;
            style.normal.textColor = leader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : Color.white;
            style.fontStyle = leader ? FontStyle.Bold : FontStyle.Normal;
            string label = (leader ? "* " : "") + member.Name;
            GUI.Label(new Rect(x, y, width, PartyConfig.FontSize.Value + 6f), label, style);
        }

        private static void DrawDistance(float distance, float x, float y, float width)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = PartyConfig.FontSize.Value - 2, alignment = TextAnchor.UpperRight };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.7f) * GUI.color;
            GUI.Label(new Rect(x, y, width, PartyConfig.FontSize.Value + 6f), $"{distance:0}m", style);
        }

        /// <summary>A rounded background plus a rounded fill, clipped to the health fraction so the cut edge stays flat.</summary>
        private static float DrawBar(float x, float y, float width, float fraction, Color color, float heightScale = 1f)
        {
            float height = PartyConfig.BarHeight.Value * heightScale;
            Rect background = new Rect(x, y, width, height);
            Color previous = GUI.color;
            GUI.color = BarBackground * new Color(1f, 1f, 1f, previous.a);
            RoundedTexture.Style().Draw(background, false, false, false, false);
            float fillWidth = width * Mathf.Clamp01(fraction);
            if (fillWidth > 0.5f)
            {
                GUI.color = color * previous;
                GUI.BeginGroup(new Rect(x, y, fillWidth, height));
                RoundedTexture.Style().Draw(new Rect(0, 0, width, height), false, false, false, false);
                GUI.EndGroup();
            }
            GUI.color = previous;
            return y + height + 4f;
        }
    }
}
