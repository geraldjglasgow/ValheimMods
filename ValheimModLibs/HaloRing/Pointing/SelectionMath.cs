using UnityEngine;

namespace HaloRing.Pointing;

/// <summary>
/// Picking by angle, not by hit test, on screen coordinates: 0 degrees is +x (three o'clock), 90 is +y (twelve),
/// increasing counter-clockwise. A gamepad stick goes through the same formula as the pointer's offset.
/// </summary>
internal static class SelectionMath
{
	public static float Normalize360(float degrees)
	{
		float wrapped = degrees % 360f;
		return wrapped < 0f ? wrapped + 360f : wrapped;
	}

	/// <summary>Null inside the dead zone, else the segment the offset points into, without hysteresis.</summary>
	public static int? RawIndex(Vector2 offset, float deadZoneRadius, int segmentCount, float startAngleOffsetDeg)
	{
		if (offset.magnitude < deadZoneRadius)
			return null;
		float relative = RelativeAngle(offset, startAngleOffsetDeg);
		return Mathf.FloorToInt(relative / (360f / segmentCount));
	}

	/// <summary>Degrees past the raw segment's leading edge (0 at the edge, up to one segment width).</summary>
	public static float PositionInSegment(Vector2 offset, int segmentCount, float startAngleOffsetDeg, int rawIndex) =>
		RelativeAngle(offset, startAngleOffsetDeg) - rawIndex * (360f / segmentCount);

	private static float RelativeAngle(Vector2 offset, float startAngleOffsetDeg)
	{
		float angle = Normalize360(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
		return Normalize360(angle - startAngleOffsetDeg);
	}
}
