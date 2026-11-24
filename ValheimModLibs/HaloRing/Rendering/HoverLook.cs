using UnityEngine;

namespace HaloRing.Rendering;

/// <summary>A ring's hover look, taken from its <see cref="HaloRingStyle"/> when the style is set.</summary>
internal readonly struct HoverLook
{
	public readonly float HoverScale;
	public readonly float AnimationDuration;
	public readonly Color BaseColor;
	public readonly Color HighlightColor;

	public HoverLook(HaloRingStyle style)
	{
		HoverScale = style.HoverScale;
		AnimationDuration = style.AnimationSeconds;
		BaseColor = style.SegmentColor;
		HighlightColor = style.HighlightColor;
	}
}
