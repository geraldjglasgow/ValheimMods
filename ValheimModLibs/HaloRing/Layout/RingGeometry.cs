using UnityEngine;

namespace HaloRing.Layout;

/// <summary>Ring geometry: a segment's angular width, the icon's centre radius and size. No scene state.</summary>
internal static class RingGeometry
{
	/// <summary>The band a highlight must be crossed by before the neighbouring segment takes over.</summary>
	public const float HysteresisDegrees = 2f;

	public static float SegmentWidthDegrees(int segmentCount) => 360f / segmentCount;

	public static float DrawnWidthDegrees(int segmentCount, float gapDegrees) => SegmentWidthDegrees(segmentCount) - gapDegrees;

	public static float IconCentroidRadius(float innerRadius, float outerRadius) => (innerRadius + outerRadius) / 2f;

	/// <summary>The square icon that fits one drawn segment: the smaller of the chord and radial limits, padded, capped.</summary>
	public static float IconSize(float innerRadius, float outerRadius, float drawnWidthDegrees, float iconPadding, float maxIconSize)
	{
		float centroidRadius = IconCentroidRadius(innerRadius, outerRadius);
		float chordLimit = 2f * centroidRadius * Mathf.Sin(drawnWidthDegrees * Mathf.Deg2Rad / 2f);
		float radialLimit = outerRadius - innerRadius;
		float fitted = Mathf.Min(chordLimit, radialLimit) * iconPadding;
		return Mathf.Min(fitted, maxIconSize);
	}

	public static Vector2 AngleToPoint(float degrees, float radius)
	{
		float rad = degrees * Mathf.Deg2Rad;
		return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
	}
}
