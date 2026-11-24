using UnityEngine;

namespace HaloRing;

/// <summary>Which analogue stick points while a gamepad is in use.</summary>
public enum RingStick
{
	Left,
	Right,
}

/// <summary>
/// A ring's size and look, in pixels at a 1080 high screen (scaled by the game's own UI scale and
/// <see cref="UiScale"/>). Angles are screen degrees: 0 at three o'clock, counter-clockwise; segment 0 starts at
/// <see cref="StartAngle"/> and the rest follow counter-clockwise. The defaults are the retired HaloMenu mod's. A caller owns its own
/// settings and hands a style to <see cref="HaloRingMenu.Style"/>; the layout follows on the next open, the look at once.
/// </summary>
public sealed class HaloRingStyle
{
	public float InnerRadius = 90f;
	public float OuterRadius = 190f;
	public float GapDegrees = 3f;
	public float StartAngle = -90f;
	public float DeadZone = 40f;
	public float IconPadding = 0.7f;
	public float MaxIconSize = 64f;
	public float UiScale = 1f;

	public float HoverScale = 1.08f;
	public float AnimationSeconds = 0.08f;
	public Color SegmentColor = new Color32(0x14, 0x14, 0x16, 0xCC);
	public Color HighlightColor = new Color32(0xD4, 0xAF, 0x37, 0xFF);
	public bool ShowLabel = true;

	public bool Gamepad = true;
	public RingStick Stick = RingStick.Right;

	/// <summary>A start angle that puts segment 0's middle at twelve o'clock for a ring of that many segments.</summary>
	public static float TopFirst(int segments) => 90f - 180f / Mathf.Max(1, segments);
}
