using UnityEngine;

namespace HaloMenu.Rendering
{
    /// <summary>A ring's hover look, parsed from its <see cref="Config.RingSettings"/> once and again after a Visual setting changes.</summary>
    public readonly struct HoverVisualConfig
    {
        public readonly float HoverScale;
        public readonly float AnimationDuration;
        public readonly Color BaseColor;
        public readonly Color HighlightColor;

        public HoverVisualConfig(float hoverScale, float animationDuration, Color baseColor, Color highlightColor)
        {
            HoverScale = hoverScale;
            AnimationDuration = animationDuration;
            BaseColor = baseColor;
            HighlightColor = highlightColor;
        }
    }
}
