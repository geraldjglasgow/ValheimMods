using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>
    /// Selection by angle, not hit-testing. Standard math convention on screen-space coordinates: 0 degrees is
    /// +x (3 o'clock), 90 is +y (12 o'clock, since Unity's mouse position and RectTransform local space are both
    /// y-up), increasing counter-clockwise. This one formula also drives gamepad selection: feed the stick vector
    /// in as the offset and the same path runs.
    /// </summary>
    public static class SelectionMath
    {
        public static float Normalize360(float degrees)
        {
            float wrapped = degrees % 360f;
            return wrapped < 0f ? wrapped + 360f : wrapped;
        }

        /// <summary>Null when offset is inside the dead zone. Otherwise the raw segment index, ignoring hysteresis.</summary>
        public static int? RawIndex(Vector2 offset, float deadZoneRadius, int segmentCount, float startAngleOffsetDeg)
        {
            if (offset.magnitude < deadZoneRadius)
                return null;
            float relative = RelativeAngle(offset, startAngleOffsetDeg);
            float segmentWidth = 360f / segmentCount;
            return Mathf.FloorToInt(relative / segmentWidth);
        }

        /// <summary>Degrees past the raw segment's leading edge (0 at the edge, up to one segment width).</summary>
        public static float PositionInSegment(Vector2 offset, int segmentCount, float startAngleOffsetDeg, int rawIndex)
        {
            float relative = RelativeAngle(offset, startAngleOffsetDeg);
            float segmentWidth = 360f / segmentCount;
            return relative - rawIndex * segmentWidth;
        }

        private static float RelativeAngle(Vector2 offset, float startAngleOffsetDeg)
        {
            float angle = Normalize360(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
            return Normalize360(angle - startAngleOffsetDeg);
        }
    }
}
