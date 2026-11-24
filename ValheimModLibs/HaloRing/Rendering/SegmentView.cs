using HaloRing.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace HaloRing.Rendering;

/// <summary>
/// One pooled segment: a fill wedge, a rim wedge (the hover outline) and an icon, children of a root placed at the
/// segment's centroid so the hover scale grows it about that point. Made once per pool, never while a ring opens or
/// closes.
/// </summary>
internal sealed class SegmentView : MonoBehaviour
{
	private const float RimThickness = 4f;
	private const float ShakeDuration = 0.25f;
	private const float ShakeAmplitude = 6f;

	private RectTransform root = null!;
	private WedgeGraphic fill = null!;
	private WedgeGraphic rim = null!;
	private Image icon = null!;

	private float hoverT;
	private float shakeTimer;
	private float shownScale = 1f;
	private bool shaking;

	public static SegmentView Create(Transform parent)
	{
		RectTransform root = RingUi.NewRect("Segment", parent, Vector2.zero);
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

	/// <summary>The wedge always shows, filled or not (an empty ring is still a ring); only the icon needs an item.</summary>
	public void SetContent(Sprite? sprite, bool slotFilled)
	{
		icon.sprite = sprite;
		icon.enabled = slotFilled && sprite != null;
	}

	public void TriggerShake() => shakeTimer = ShakeDuration;

	public void Tick(bool highlighted, bool entryEnabled, HoverLook look, float deltaTime)
	{
		hoverT = Advance(hoverT, highlighted ? 1f : 0f, look.AnimationDuration, deltaTime);
		float eased = 1f - (1f - hoverT) * (1f - hoverT);
		ApplyScale(eased, look.HoverScale, deltaTime);
		ApplyColors(eased, entryEnabled, look);
	}

	/// <summary>Scale and icon offset are written only when they change: a transform write rebuilds the canvas.</summary>
	private void ApplyScale(float eased, float hoverScale, float deltaTime)
	{
		float scale = Mathf.Lerp(1f, hoverScale, eased);
		if (scale != shownScale)
		{
			shownScale = scale;
			root.localScale = new Vector3(scale, scale, 1f);
		}
		ApplyShake(deltaTime);
	}

	private void ApplyShake(float deltaTime)
	{
		if (shakeTimer <= 0f)
		{
			if (shaking)
			{
				shaking = false;
				icon.rectTransform.anchoredPosition = Vector2.zero;
			}
			return;
		}
		shaking = true;
		shakeTimer = Mathf.Max(0f, shakeTimer - deltaTime);
		float wobble = Mathf.Sin(shakeTimer * 40f) * ShakeAmplitude * (shakeTimer / ShakeDuration);
		icon.rectTransform.anchoredPosition = new Vector2(wobble, 0f);
	}

	private void ApplyColors(float eased, bool entryEnabled, HoverLook look)
	{
		Color brightened = Color.Lerp(look.BaseColor, Color.white, 0.35f);
		Color fillColor = Color.Lerp(look.BaseColor, brightened, eased);
		float opacity = entryEnabled ? 1f : 0.4f;
		fillColor.a *= opacity;
		fill.color = fillColor;
		Color rimColor = look.HighlightColor;
		rimColor.a *= eased * opacity;
		rim.color = rimColor;
		icon.color = new Color(1f, 1f, 1f, opacity);
	}

	private static float Advance(float current, float target, float duration, float deltaTime) =>
		duration <= 0f ? target : Mathf.MoveTowards(current, target, deltaTime / duration);

	private static WedgeGraphic NewWedge(string name, Transform parent)
	{
		WedgeGraphic wedge = RingUi.NewRect(name, parent, Vector2.zero).gameObject.AddComponent<WedgeGraphic>();
		wedge.raycastTarget = false;
		return wedge;
	}

	private static Image NewIcon(Transform parent)
	{
		Image image = RingUi.NewRect("Icon", parent, Vector2.zero).gameObject.AddComponent<Image>();
		image.preserveAspect = true;
		image.raycastTarget = false;
		return image;
	}
}
