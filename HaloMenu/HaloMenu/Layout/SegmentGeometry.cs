using UnityEngine;

namespace HaloMenu.Layout
{
    /// <summary>One segment's fixed geometry: the drawn wedge's angular span (gap already removed) and its icon's
    /// centered square, in the ring root's local space (screen-space convention, y-up, center at the origin).</summary>
    public readonly struct SegmentGeometry
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
}
