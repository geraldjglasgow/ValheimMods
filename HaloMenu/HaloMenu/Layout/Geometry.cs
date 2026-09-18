using UnityEngine;

namespace HaloMenu.Layout
{
    /// <summary>Pure ring geometry math: segment angular width, icon centroid and size. No Unity scene state.</summary>
    public static class Geometry
    {
        /// <summary>The hysteresis band a highlight must be crossed by before the neighbouring segment takes over.</summary>
        public const float HysteresisDegrees = 2f;

        public static float SegmentWidthDegrees(int segmentCount) => 360f / segmentCount;

        public static float DrawnWidthDegrees(int segmentCount, float gapDegrees) => SegmentWidthDegrees(segmentCount) - gapDegrees;

        public static float IconCentroidRadius(float innerRadius, float outerRadius) => (innerRadius + outerRadius) / 2f;

        /// <summary>The square icon size that fits inside one drawn segment, the smaller of the chord and radial
        /// limits, padded and capped.</summary>
        public static float IconSize(float innerRadius, float outerRadius, float drawnWidthDegrees, float iconPadding, float maxIconSize)
        {
            float centroidRadius = IconCentroidRadius(innerRadius, outerRadius);
            float chordLimit = 2f * centroidRadius * Mathf.Sin(drawnWidthDegrees * Mathf.Deg2Rad / 2f);
            float radialLimit = outerRadius - innerRadius;
            float fitted = Mathf.Min(chordLimit, radialLimit) * iconPadding;
            return Mathf.Min(fitted, maxIconSize);
        }

        /// <summary>The ring's own UIScale times Valheim's own UI scale factor (<see cref="Runtime.GameUiScale"/>,
        /// which already reflects screen resolution).</summary>
        public static float ScreenScale(float ringUiScale, float gameUiScale) => ringUiScale * gameUiScale;
    }
}
