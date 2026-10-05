using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// Whether two pieces' drawn boxes (<see cref="FixPiece"/>) touch, as Fix ground's building test decides it: their
    /// heights overlap and their footprints come within <see cref="Touch"/> m (tested as circles first, then as the
    /// turned rectangles along their four axes). Kept here because that test is private to <see cref="GroundFixBuilding"/>.
    /// </summary>
    internal static class CopyTouch
    {
        public const float Touch = 0.15f;

        public static bool Touching(FixPiece a, FixPiece b)
        {
            if (a.Bottom > b.Bottom + b.Height + Touch || b.Bottom > a.Bottom + a.Height + Touch)
                return false;
            if (Flat(a.Centre - b.Centre) > a.Reach + b.Reach + Touch)
                return false;
            return a.Covers(b.Centre.x, b.Centre.z) || b.Covers(a.Centre.x, a.Centre.z) || Separation(a, b) <= Touch;
        }

        /// <summary>Half the box's width along a flat axis.</summary>
        public static float Extent(FixPiece p, Vector3 axis)
        {
            return Mathf.Abs(Vector3.Dot(p.Across, axis)) * p.Half.x + Mathf.Abs(Vector3.Dot(p.Along, axis)) * p.Half.y;
        }

        /// <summary>How far apart the two turned rectangles are along the four axes (0 or less when they overlap).</summary>
        private static float Separation(FixPiece a, FixPiece b)
        {
            float gap = float.MinValue;
            foreach (Vector3 axis in new[] { a.Across, a.Along, b.Across, b.Along })
            {
                float d = Mathf.Abs(Vector3.Dot(b.Centre - a.Centre, axis));
                gap = Mathf.Max(gap, d - Extent(a, axis) - Extent(b, axis));
            }
            return gap;
        }

        private static float Flat(Vector3 v) => Mathf.Sqrt(v.x * v.x + v.z * v.z);
    }
}
