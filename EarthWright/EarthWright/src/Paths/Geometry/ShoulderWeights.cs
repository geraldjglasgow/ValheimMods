using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// How strongly each vertex takes the planned height: 1 on the surface, falling smoothly to 0 across the
    /// shoulders beside it and, when the ends blend, across the blend length past them. The blend weight is what
    /// cuts or fills the ground beside a ramp into a smooth bank instead of a wall. Pure functions.
    /// </summary>
    public static class ShoulderWeights
    {
        /// <summary>Vertices with a smaller weight are left out: the change would not be visible.</summary>
        public const float MinWeight = 0.02f;

        /// <summary>Paint reaches this far past the surface edge, so a narrow path running diagonally has no gaps.</summary>
        public const float PaintMargin = 0.3f;

        /// <summary>0..1: how far the vertex moves from its current height to the planned one.</summary>
        public static float Weight(VertexSample sample, PathShape shape)
        {
            float side = SideBeyond(sample, shape);
            if (side <= 0f && sample.Outside <= 0f)
                return 1f;
            if (sample.Outside > 0f && !shape.BlendEnds)
                return 0f;
            return Falloff(side, shape.Shoulder) * Falloff(sample.Outside, shape.EndBlend);
        }

        /// <summary>The vertex lies on the surface (within the paint margin) and gets the surface paint.</summary>
        public static bool Painted(VertexSample sample, PathShape shape)
        {
            return sample.Outside <= 0f && SideBeyond(sample, shape) <= PaintMargin;
        }

        /// <summary>Metres the vertex lies beyond the surface edge on its side of the line; 0 or less on the surface.</summary>
        public static float SideBeyond(VertexSample sample, PathShape shape)
        {
            return Mathf.Abs(sample.Lateral) - (sample.Lateral >= 0f ? shape.Left : shape.Right);
        }

        /// <summary>1 at the edge, easing to 0 at <paramref name="width"/> metres past it (smoothstep); 0 beyond, or at once when the width is 0.</summary>
        public static float Falloff(float distance, float width)
        {
            if (distance <= 0f)
                return 1f;
            if (width <= 0f || distance >= width)
                return 0f;
            float x = distance / width;
            return 1f - x * x * (3f - 2f * x);
        }
    }
}
