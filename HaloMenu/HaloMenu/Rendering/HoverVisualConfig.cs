using UnityEngine;

namespace HaloMenu.Rendering
{
    /// <summary>Read live from <see cref="Config.RingSettings"/> every frame the ring is open; nothing here is cached.</summary>
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
