using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Party.Client;

namespace Party.UI
{
    /// <summary>One health/stamina/eitr bar: a rounded, masked background with a plain fill clipped to that shape.</summary>
    public sealed class BarView
    {
        private const float SmoothRate = 12f;

        private readonly GameObject root;
        private readonly RectTransform fill;
        private readonly float height;
        private float shown = 1f;

        public BarView(Transform parent, float y, float width, float barHeight, Color color)
        {
            height = barHeight;
            GameObject mask = NewRect("BarBackground", parent, 0, y, width, barHeight);
            root = mask;
            Image background = mask.AddComponent<Image>();
            RoundedSprite.Apply(background, barHeight * 0.5f);
            background.color = new Color(0f, 0f, 0f, 0.55f);
            mask.AddComponent<Mask>().showMaskGraphic = true;

            GameObject fillGo = NewRect("Fill", mask.transform, 0, 0, width, barHeight);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = color;
            fill = fillGo.GetComponent<RectTransform>();
        }

        /// <summary>Eases toward the target so the few-per-second vitals reports read as movement, not jumps.</summary>
        public void SetFraction(float fraction, float fullWidth)
        {
            shown = Mathf.Lerp(shown, Mathf.Clamp01(fraction), 1f - Mathf.Exp(-SmoothRate * Time.deltaTime));
            float width = fullWidth * shown;
            fill.sizeDelta = new Vector2(width, height);
            fill.gameObject.SetActive(width > 0.5f);
        }

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

    /// <summary>
    /// One party member's row: name, distance/offline status, and the enabled bars. Built once for the current
    /// layout settings (the panel rebuilds rows when those change), updated every tick. Bar colors follow the
    /// game's own vitals palette: red health, yellow stamina, blue eitr.
    /// </summary>
    public sealed class PartyRowView
    {
        private const float StatusWidth = 80f;

        public readonly GameObject Root;
        private readonly TMP_Text nameText;
        private readonly TMP_Text statusText;
        private readonly BarView health;
        private readonly BarView stamina;
        private readonly BarView eitr;
        private readonly Image[] ailmentIcons;
        private readonly float barWidth;
        private float nextBarY;

        public PartyRowView(Transform parent, float width)
        {
            barWidth = width;
            Root = new GameObject("Row", typeof(RectTransform));
            Root.transform.SetParent(parent, false);
            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(width, HealthPanelLayout.RowHeight());

            nameText = CreateText("Name", 0f, width - StatusWidth);
            statusText = CreateText("Status", width - StatusWidth, StatusWidth);
            statusText.alignment = TextAlignmentOptions.TopRight;
            statusText.fontSize = Mathf.Max(8, PartyConfig.FontSize.Value - 3);

            nextBarY = HealthPanelLayout.NameHeight();
            health = BuildBar(PartyConfig.BarHeight.Value, new Color(0.78f, 0.2f, 0.16f));
            if (PartyConfig.ShowStamina.Value)
                stamina = BuildBar(HealthPanelLayout.SubBarHeight(), new Color(0.85f, 0.75f, 0.24f));
            if (PartyConfig.ShowEitr.Value)
                eitr = BuildBar(HealthPanelLayout.SubBarHeight(), new Color(0.4f, 0.55f, 0.95f));
            if (PartyConfig.ShowAilments.Value)
                ailmentIcons = BuildAilmentIcons();
        }

        private BarView BuildBar(float height, Color color)
        {
            BarView bar = new BarView(Root.transform, nextBarY, barWidth, height, color);
            nextBarY += height + HealthPanelLayout.BarGap;
            return bar;
        }

        /// <summary>One hidden Image per tracked ailment, in a strip under the bars; Apply toggles them.</summary>
        private Image[] BuildAilmentIcons()
        {
            Image[] icons = new Image[Ailments.Count];
            float size = HealthPanelLayout.AilmentIconSize();
            for (int i = 0; i < icons.Length; i++)
            {
                GameObject go = new GameObject("Ailment", typeof(RectTransform));
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetParent(Root.transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(i * (size + 2f), -nextBarY);
                rect.sizeDelta = new Vector2(size, size);
                icons[i] = go.AddComponent<Image>();
                icons[i].preserveAspect = true;
                go.SetActive(false);
            }
            return icons;
        }

        private TMP_Text CreateText(string name, float x, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            // Twice the font size, not NameHeight(): TMP's Ellipsis mode drops the whole line when the
            // font's line height exceeds the rect, which made names vanish in a FontSize + 6 box.
            rect.sizeDelta = new Vector2(width, PartyConfig.FontSize.Value * 2f);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = PartyFont.Get();
            text.fontSize = PartyConfig.FontSize.Value;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        public void Apply(PartyMemberView member, float? distance)
        {
            bool leader = member.IsLeader;
            nameText.text = (leader ? "* " : "") + member.Name;
            nameText.color = leader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : Color.white;
            nameText.fontStyle = leader ? FontStyles.Bold : FontStyles.Normal;
            float alpha = member.Online ? 1f : 0.45f;
            nameText.alpha = alpha;
            statusText.alpha = member.Online ? 0.8f : 0.45f;
            statusText.text = member.Online ? (distance.HasValue ? $"{distance.Value:0}m" : "") : "offline";

            health.SetFraction(member.Online ? member.Health : 0f, barWidth);
            stamina?.SetFraction(member.Online ? member.Stamina : 0f, barWidth);
            eitr?.SetFraction(member.Online ? member.Eitr : 0f, barWidth);
            ApplyAilments(member);
        }

        /// <summary>Icons pack to the left in bit order; sprites resolve lazily so a missing ObjectDB just retries.</summary>
        private void ApplyAilments(PartyMemberView member)
        {
            if (ailmentIcons == null)
                return;
            int shown = 0;
            float size = HealthPanelLayout.AilmentIconSize();
            for (int i = 0; i < ailmentIcons.Length; i++)
            {
                bool on = member.Online && (member.Ailments & (1 << i)) != 0;
                if (on && ailmentIcons[i].sprite == null)
                    ailmentIcons[i].sprite = Ailments.Icon(i);
                on &= ailmentIcons[i].sprite != null;
                ailmentIcons[i].gameObject.SetActive(on);
                if (on)
                    ailmentIcons[i].rectTransform.anchoredPosition = new Vector2(shown++ * (size + 2f), ailmentIcons[i].rectTransform.anchoredPosition.y);
            }
        }
    }
}
