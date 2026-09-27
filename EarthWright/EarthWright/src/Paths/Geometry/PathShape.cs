using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The cross-section of a ramp or road: how far its flat surface reaches to each side of the centre line, how wide
    /// the shoulders are over which it blends into the ground beside it, and whether (and over how many metres) its
    /// ends blend into the ground past them. Left and right are seen in the direction of travel.
    /// </summary>
    public struct PathShape
    {
        /// <summary>Surface metres to the left of the centre line.</summary>
        public float Left;

        /// <summary>Surface metres to the right of the centre line.</summary>
        public float Right;

        /// <summary>Metres beside the surface over which it blends into the ground.</summary>
        public float Shoulder;

        /// <summary>Metres past each end over which it blends into the ground, when <see cref="BlendEnds"/>.</summary>
        public float EndBlend;

        public bool BlendEnds;

        /// <summary>The full surface width.</summary>
        public float Width => Left + Right;

        /// <summary>How far from the centre line a vertex may still be changed (one metre spare for rounding).</summary>
        public float Reach => Mathf.Max(Left, Right) + Mathf.Max(0f, Shoulder) + 1f;

        /// <summary>How far past the ends a vertex may still be changed.</summary>
        public float EndReach => BlendEnds ? Mathf.Max(0f, EndBlend) : 0f;

        /// <summary>
        /// A shape of <paramref name="width"/> metres: centred when <paramref name="side"/> is 0, else all of it to the
        /// left (side &gt; 0) or to the right (side &lt; 0) of the centre line.
        /// </summary>
        public static PathShape Make(float width, int side, float shoulder, float endBlend, bool blendEnds)
        {
            width = Mathf.Max(0f, width);
            return new PathShape
            {
                Left = side == 0 ? width / 2f : side > 0 ? width : 0f,
                Right = side == 0 ? width / 2f : side < 0 ? width : 0f,
                Shoulder = Mathf.Max(0f, shoulder),
                EndBlend = Mathf.Max(0f, endBlend),
                BlendEnds = blendEnds,
            };
        }
    }
}
