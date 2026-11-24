using UnityEngine;

namespace HaloRing.Layout;

/// <summary>One segment's fixed geometry: the drawn wedge's span (gap removed) and its icon's square, in the ring
/// root's local space (y up, centre at the origin).</summary>
internal readonly struct SegmentGeometry
{
	public readonly float DrawnStartDegrees;
	public readonly float DrawnEndDegrees;
	public readonly Vector2 IconCenter;
	public readonly float IconSize;

	public SegmentGeometry(float drawnStartDegrees, float drawnEndDegrees, Vector2 iconCenter, float iconSize)
	{
		DrawnStartDegrees = drawnStartDegrees;
		DrawnEndDegrees = drawnEndDegrees;
		IconCenter = iconCenter;
		IconSize = iconSize;
	}
}
