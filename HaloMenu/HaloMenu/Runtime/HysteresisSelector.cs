using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>
    /// Tracks which segment is highlighted, sticky across the ~2 degree hysteresis band at each boundary so a
    /// cursor resting exactly on a boundary does not flicker. One instance per open ring; call <see cref="Reset"/>
    /// when the ring opens.
    /// </summary>
    public sealed class HysteresisSelector
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
            float segmentWidth = 360f / segmentCount;
            highlighted = Resolve(raw.Value, posInSegment, segmentWidth, segmentCount, hysteresisDegrees);
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
}
