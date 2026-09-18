using HaloMenu.Config;
using UnityEngine;

namespace HaloMenu.Layout
{
    /// <summary>
    /// One ring's cached geometry: inner/outer radius (already screen-scaled), the dead zone radius, and one
    /// <see cref="SegmentGeometry"/> per segment. Rebuild only on the documented invalidations - never per frame,
    /// never merely because the ring opened, closed or a different segment is hovered.
    /// </summary>
    public sealed class RingLayout
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

        public static RingLayout Build(RingSettings s, float screenScale)
        {
            int segmentCount = s.SegmentCount.Value;
            float inner = s.InnerRadius.Value * screenScale;
            float outer = ValidOuterRadius(s, screenScale, inner);
            float gap = s.GapDegrees.Value;
            float start = s.StartAngleOffset.Value;
            float deadZone = s.DeadZoneRadius.Value * screenScale;
            float maxIcon = s.MaxIconSize.Value * screenScale;
            SegmentGeometry[] segments = BuildSegments(segmentCount, inner, outer, gap, start, s.IconPadding.Value, maxIcon);
            return new RingLayout(segmentCount, inner, outer, deadZone, start, segments);
        }

        private static float ValidOuterRadius(RingSettings s, float screenScale, float inner)
        {
            float outer = s.OuterRadius.Value * screenScale;
            if (outer > inner)
                return outer;
            HaloLog.Warning($"OuterRadius ({s.OuterRadius.Value}) must be greater than InnerRadius ({s.InnerRadius.Value}); clamping.");
            return inner + 1f;
        }

        private static SegmentGeometry[] BuildSegments(int count, float inner, float outer, float gap, float start, float iconPadding, float maxIconSize)
        {
            SegmentGeometry[] segments = new SegmentGeometry[count];
            float segmentWidth = Geometry.SegmentWidthDegrees(count);
            float drawnWidth = Geometry.DrawnWidthDegrees(count, gap);
            float iconSize = Geometry.IconSize(inner, outer, drawnWidth, iconPadding, maxIconSize);
            float centroidRadius = Geometry.IconCentroidRadius(inner, outer);
            for (int i = 0; i < count; i++)
            {
                float segStart = start + i * segmentWidth;
                float drawnStart = segStart + gap / 2f;
                float drawnEnd = segStart + segmentWidth - gap / 2f;
                float bisector = segStart + segmentWidth / 2f;
                Vector2 iconCenter = AngleToPoint(bisector, centroidRadius);
                segments[i] = new SegmentGeometry(drawnStart, drawnEnd, iconCenter, iconSize);
            }
            return segments;
        }

        private static Vector2 AngleToPoint(float degrees, float radius)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }
    }
}
