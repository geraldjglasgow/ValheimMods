using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrindstoneSkills
{
    /// <summary>
    /// The line tension bar, on the angler's screen only, while a fish is on the line: a slim bar under the crosshair that
    /// fills green to red with the tension, and a word above it for the fight's state ("Line", "Thrashing! Ease off",
    /// "Spent! Reel it in", "Catch your breath 3"). It hangs under the game's HUD root, so it hides with the HUD, and is
    /// built on first use from plain UI images and the HUD's own font. <see cref="Show"/> is called every step of the
    /// fight; the bar hides itself a moment after the calls stop (the fish landed, got away or the line snapped).
    /// </summary>
    public sealed class TensionBar : MonoBehaviour
    {
        private const float Width = 240f;
        private const float BarHeight = 10f;
        private const float LabelHeight = 22f;
        private const float BelowCentre = 90f;
        private const float StaleSeconds = 0.25f;

        private static readonly Color Calm = new Color(0.45f, 0.8f, 0.35f);
        private static readonly Color Strained = new Color(0.95f, 0.8f, 0.25f);
        private static readonly Color Breaking = new Color(0.9f, 0.2f, 0.15f);

        private static TensionBar instance;

        private RectTransform fill;
        private Image fillImage;
        private TMP_Text label;
        private float shownUntil;

        /// <summary>Shows the fight's tension and state; builds the bar the first time.</summary>
        public static void Show(FloatFight fight)
        {
            TensionBar bar = instance != null ? instance : Build();
            if (bar == null || fight == null)
                return;
            bar.gameObject.SetActive(true);
            bar.shownUntil = Time.time + StaleSeconds;
            bar.Set(fight.Tension, State(fight));
        }

        public static void Hide()
        {
            if (instance != null)
                instance.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Time.time > shownUntil)
                gameObject.SetActive(false);
        }

        private void Set(float tension, string state)
        {
            float share = Mathf.Clamp01(tension);
            fill.sizeDelta = new Vector2(Width * share, BarHeight);
            fillImage.color = share < 0.5f ? Color.Lerp(Calm, Strained, share * 2f) : Color.Lerp(Strained, Breaking, share * 2f - 1f);
            if (label.text != state)
                label.text = state;
        }

        private static string State(FloatFight fight)
        {
            if (fight.InGrace)
                return "Catch your breath " + Mathf.CeilToInt(fight.GraceUntil - Time.time);
            if (fight.Spent)
                return "<color=#9be07a>Spent! Reel it in</color>";
            return fight.Thrashing ? "<color=#ff6a50>Thrashing! Ease off</color>" : "Line";
        }

        private static TensionBar Build()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
                return null;
            RectTransform root = Rect("grindstone_tension", hud.m_rootObject.transform, new Vector2(0f, -BelowCentre), new Vector2(Width, BarHeight + LabelHeight));
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 1f);
            instance = root.gameObject.AddComponent<TensionBar>();
            instance.label = Label(root, hud);
            RectTransform back = Rect("back", root, new Vector2(0f, -LabelHeight), new Vector2(Width, BarHeight));
            back.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            instance.fill = Rect("fill", back, Vector2.zero, new Vector2(0f, BarHeight));
            instance.fillImage = instance.fill.gameObject.AddComponent<Image>();
            return instance;
        }

        private static TMP_Text Label(RectTransform root, Hud hud)
        {
            TextMeshProUGUI text = Rect("label", root, Vector2.zero, new Vector2(Width, LabelHeight)).gameObject.AddComponent<TextMeshProUGUI>();
            if (hud.m_hoverName != null)
                text.font = hud.m_hoverName.font;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Bottom;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>A new UI rectangle, anchored at its parent's top left, placed by its top left corner.</summary>
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
