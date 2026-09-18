using HaloMenu.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace HaloMenu.Rendering
{
    /// <summary>
    /// One pooled segment: a fill wedge, a rim wedge (the hover outline) and an icon, all children of a root
    /// positioned at the segment's own centroid so hover-scaling the root grows the segment about that point. Built
    /// once per pool (<see cref="Create"/>); never instantiated or destroyed while a ring opens or closes.
    /// </summary>
    public sealed class SegmentView : MonoBehaviour
    {
        private const float RimThickness = 4f;
        private const float ShakeDuration = 0.25f;
        private const float ShakeAmplitude = 6f;

        private RectTransform root;
        private WedgeGraphic fill;
        private WedgeGraphic rim;
        private Image icon;

        private float hoverT;
        private float shakeTimer;

        public static SegmentView Create(Transform parent)
        {
            RectTransform root = NewRect("Segment", parent, Vector2.zero, Vector2.zero);
            SegmentView view = root.gameObject.AddComponent<SegmentView>();
            view.root = root;
            view.fill = NewWedge("Fill", root);
            view.rim = NewWedge("Rim", root);
            view.icon = NewIcon(root);
            return view;
        }

        public void SetGeometry(SegmentGeometry g, float innerRadius, float outerRadius)
        {
            root.anchoredPosition = g.IconCenter;
            fill.Apply(innerRadius, outerRadius, g.DrawnStartDegrees, g.DrawnEndDegrees, g.IconCenter);
            rim.Apply(outerRadius, outerRadius + RimThickness, g.DrawnStartDegrees, g.DrawnEndDegrees, g.IconCenter);
            icon.rectTransform.sizeDelta = new Vector2(g.IconSize, g.IconSize);
        }

        public void SetContent(Sprite sprite, bool slotFilled)
        {
            // The wedge itself always renders, filled or not - an empty ring is still a visible ring (see
            // SPEC.md: "it opens an empty ring"). Only the icon depends on whether a slot has an entry.
            icon.sprite = sprite;
            icon.enabled = slotFilled && sprite != null;
        }

        public void TriggerShake() => shakeTimer = ShakeDuration;

        public void Tick(bool highlighted, bool entryEnabled, HoverVisualConfig cfg, float deltaTime)
        {
            float targetT = highlighted ? 1f : 0f;
            hoverT = Advance(hoverT, targetT, cfg.AnimationDuration, deltaTime);
            float eased = 1f - (1f - hoverT) * (1f - hoverT);
            ApplyScale(eased, cfg.HoverScale, deltaTime);
            ApplyColors(eased, entryEnabled, cfg);
        }

        private void ApplyScale(float eased, float hoverScale, float deltaTime)
        {
            float scale = Mathf.Lerp(1f, hoverScale, eased);
            root.localScale = new Vector3(scale, scale, 1f);
            if (shakeTimer > 0f)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - deltaTime);
                float wobble = Mathf.Sin(shakeTimer * 40f) * ShakeAmplitude * (shakeTimer / ShakeDuration);
                icon.rectTransform.anchoredPosition = new Vector2(wobble, 0f);
            }
            else
            {
                icon.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void ApplyColors(float eased, bool entryEnabled, HoverVisualConfig cfg)
        {
            Color brightened = Color.Lerp(cfg.BaseColor, Color.white, 0.35f);
            Color fillColor = Color.Lerp(cfg.BaseColor, brightened, eased);
            float opacity = entryEnabled ? 1f : 0.4f;
            fillColor.a *= opacity;
            fill.color = fillColor;
            Color rimColor = cfg.HighlightColor;
            rimColor.a *= eased * opacity;
            rim.color = rimColor;
            icon.color = new Color(1f, 1f, 1f, opacity);
        }

        private static float Advance(float current, float target, float duration, float deltaTime)
        {
            if (duration <= 0f)
                return target;
            float rate = 1f / duration;
            return Mathf.MoveTowards(current, target, rate * deltaTime);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static WedgeGraphic NewWedge(string name, Transform parent)
        {
            RectTransform rect = NewRect(name, parent, Vector2.zero, Vector2.zero);
            WedgeGraphic wedge = rect.gameObject.AddComponent<WedgeGraphic>();
            wedge.raycastTarget = false;
            return wedge;
        }

        private static Image NewIcon(Transform parent)
        {
            RectTransform rect = NewRect("Icon", parent, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
