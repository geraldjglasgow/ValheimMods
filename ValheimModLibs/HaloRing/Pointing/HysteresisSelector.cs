using UnityEngine;

namespace HaloRing.Pointing;

/// <summary>
/// Which segment is highlighted, kept across the hysteresis band at each boundary so a pointer resting on a boundary
/// does not flicker. One per ring, reset as it opens.
/// </summary>
internal sealed class HysteresisSelector
{
	private int? highlighted;

	public void Reset() => highlighted = null;

	public int? Update(Vector2 offset, int segmentCount, float startAngleOffsetDeg, float deadZoneRadius, float hysteresisDegrees)
	{
		int? raw = SelectionMath.RawIndex(offset, deadZoneRadius, segmentCount, startAngleOffsetDeg);
		if (raw == null)
		{
			highlighted = null;
			return null;
		}
		float posInSegment = SelectionMath.PositionInSegment(offset, segmentCount, startAngleOffsetDeg, raw.Value);
		highlighted = Resolve(raw.Value, posInSegment, 360f / segmentCount, segmentCount, hysteresisDegrees);
		return highlighted;
	}

	private int Resolve(int rawIndex, float posInSegment, float segmentWidth, int segmentCount, float hysteresisDegrees)
	{
		if (highlighted == null || rawIndex == highlighted.Value)
			return rawIndex;
		if (rawIndex == Mod(highlighted.Value + 1, segmentCount))
			return posInSegment >= hysteresisDegrees ? rawIndex : highlighted.Value;
		if (rawIndex == Mod(highlighted.Value - 1, segmentCount))
			return (segmentWidth - posInSegment) >= hysteresisDegrees ? rawIndex : highlighted.Value;
		return rawIndex;
	}

	private static int Mod(int a, int n) => (a % n + n) % n;
}
