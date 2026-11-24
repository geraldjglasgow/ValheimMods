using UnityEngine;

namespace HaloRing.Layout;

/// <summary>
/// One ring's geometry at one screen scale: radii (scaled), the dead zone and a <see cref="SegmentGeometry"/> per
/// segment. Built only when the style, the segment count or the screen scale changed, checked as the ring opens.
/// </summary>
internal sealed class RingLayout
{
	public readonly int SegmentCount;
	public readonly float InnerRadius;
	public readonly float OuterRadius;
	public readonly float DeadZoneRadius;
	public readonly float StartAngleOffset;
	public readonly SegmentGeometry[] Segments;

	private RingLayout(int segmentCount, float innerRadius, float outerRadius, float deadZoneRadius, float startAngleOffset, SegmentGeometry[] segments)
	{
		SegmentCount = segmentCount;
		InnerRadius = innerRadius;
		OuterRadius = outerRadius;
		DeadZoneRadius = deadZoneRadius;
		StartAngleOffset = startAngleOffset;
		Segments = segments;
	}

	/// <summary>An outer radius not above the inner one is set one pixel outside it.</summary>
	public static RingLayout Build(HaloRingStyle s, int segmentCount, float screenScale)
	{
		float inner = s.InnerRadius * screenScale;
		float outer = s.OuterRadius * screenScale;
		if (outer <= inner)
			outer = inner + 1f;
		float deadZone = s.DeadZone * screenScale;
		float maxIcon = s.MaxIconSize * screenScale;
		SegmentGeometry[] segments = BuildSegments(segmentCount, inner, outer, s.GapDegrees, s.StartAngle, s.IconPadding, maxIcon);
		return new RingLayout(segmentCount, inner, outer, deadZone, s.StartAngle, segments);
	}

	private static SegmentGeometry[] BuildSegments(int count, float inner, float outer, float gap, float start, float iconPadding, float maxIconSize)
	{
		var segments = new SegmentGeometry[count];
		float segmentWidth = RingGeometry.SegmentWidthDegrees(count);
		float drawnWidth = RingGeometry.DrawnWidthDegrees(count, gap);
		float iconSize = RingGeometry.IconSize(inner, outer, drawnWidth, iconPadding, maxIconSize);
		float centroidRadius = RingGeometry.IconCentroidRadius(inner, outer);
		for (int i = 0; i < count; i++)
		{
			float segStart = start + i * segmentWidth;
			float drawnStart = segStart + gap / 2f;
			float drawnEnd = segStart + segmentWidth - gap / 2f;
			Vector2 iconCenter = RingGeometry.AngleToPoint(segStart + segmentWidth / 2f, centroidRadius);
			segments[i] = new SegmentGeometry(drawnStart, drawnEnd, iconCenter, iconSize);
		}
		return segments;
	}
}
