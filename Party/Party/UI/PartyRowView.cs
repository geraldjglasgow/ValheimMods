using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Party.Client;

namespace Party.UI
{
    /// <summary>One health/stamina/eitr bar: a rounded, masked background with a plain fill clipped to that shape.</summary>
    public sealed class BarView
    {
        private readonly GameObject root;
        private readonly RectTransform fill;
        private readonly float height;

        public BarView(Transform parent, float y, float width, float barHeight, Color color)
        {
            height = barHeight;
            GameObject mask = NewRect("BarBackground", parent, 0, y, width, barHeight);
            root = mask;
            Image background = mask.AddComponent<Image>();
            background.sprite = RoundedSprite.Get();
            background.type = Image.Type.Sliced;
            background.color = new Color(0f, 0f, 0f, 0.55f);
            mask.AddComponent<Mask>().showMaskGraphic = true;

            GameObject fillGo = NewRect("Fill", mask.transform, 0, 0, width, barHeight);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = color;
            fill = fillGo.GetComponent<RectTransform>();
        }

        public void SetFraction(float fraction, float fullWidth)
        {
            float width = fullWidth * Mathf.Clamp01(fraction);
            fill.sizeDelta = new Vector2(width, height);
            fill.gameObject.SetActive(width > 0.5f);
        }

        public void SetActive(bool active) => root.SetActive(active);

        private static GameObject NewRect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return go;
        }
    }

    /// <summary>One party member's row: name, distance, and up to three bars. Built once, updated every tick.</summary>
    public sealed class PartyRowView
    {
        public readonly GameObject Root;
        private readonly TMP_Text nameText;
        private readonly TMP_Text distanceText;
        private readonly BarView health;
        private readonly BarView stamina;
        private readonly BarView eitr;
        private readonly float barWidth;

        public PartyRowView(Transform parent, float width)
        {
            barWidth = width;
            Root = new GameObject("Row", typeof(RectTransform));
            Root.transform.SetParent(parent, false);
            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(width, HealthPanelLayout.RowHeight());

            nameText = CreateText("Name", 0f, width - 70f);
            distanceText = CreateText("Distance", 0f, 70f);
            distanceText.GetComponent<RectTransform>().anchoredPosition = new Vector2(width - 70f, 0f);
            distanceText.alignment = TextAlignmentOptions.TopRight;
            distanceText.fontSize = PartyConfig.FontSize.Value - 4;

            float barY = PartyConfig.FontSize.Value + 6f;
            health = new BarView(Root.transform, barY, width, PartyConfig.BarHeight.Value, new Color(0.8f, 0.1f, 0.1f));
            barY += PartyConfig.BarHeight.Value + 4f;
            stamina = new BarView(Root.transform, barY, width, PartyConfig.BarHeight.Value * 0.6f, new Color(0.15f, 0.7f, 0.15f));
            barY += PartyConfig.BarHeight.Value * 0.6f + 4f;
            eitr = new BarView(Root.transform, barY, width, PartyConfig.BarHeight.Value * 0.6f, new Color(0.2f, 0.5f, 0.9f));
        }

        private TMP_Text CreateText(string name, float x, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, PartyConfig.FontSize.Value + 6f);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = PartyFont.Get();
            text.fontSize = PartyConfig.FontSize.Value;
            text.color = Color.white;
            return text;
        }

        public void Apply(PartyMemberView member, float? distance)
        {
            bool leader = member.IsLeader;
            nameText.text = (leader ? "* " : "") + member.Name;
            nameText.color = leader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : Color.white;
            nameText.fontStyle = leader ? FontStyles.Bold : FontStyles.Normal;
            float alpha = member.Online ? 1f : 0.5f;
            nameText.alpha = alpha;
            distanceText.gameObject.SetActive(distance.HasValue);
            if (distance.HasValue)
                distanceText.text = $"{distance.Value:0}m";

            health.SetFraction(member.Online ? member.Health : 0f, barWidth);
            stamina.SetFraction(member.Online ? member.Stamina : 0f, barWidth);
            eitr.SetFraction(member.Online ? member.Eitr : 0f, barWidth);
            stamina.SetActive(PartyConfig.ShowStamina.Value);
            eitr.SetActive(PartyConfig.ShowEitr.Value);
        }
    }
}
