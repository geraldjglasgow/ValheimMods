using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The shape of a brush stroke on the ground, evaluated in world metres so a square stays a square at any rotation
    /// (no diamond distortion). Every point gets a weight 0..1: full inside the hard core, then a smoothstep falloff to 0
    /// at the rim. The normalized distance t runs from 0 at the core line (the centre; the middle of a ring or frame band)
    /// to 1 at the nearest rim, so the feather has the same width in metres along every edge of the shape.
    /// Local axes are the world axes turned by <c>Quaternion.Euler(0, rotation, 0)</c>: u along the turned X axis, v
    /// along the turned Z axis (<see cref="LocalToWorld"/>); the preview draws outlines with the same convention.
    /// </summary>
    public struct Footprint
    {
        /// <summary>Points this far outside the rim (metres) still count as inside, so grid-snapped edges land on vertices.</summary>
        private const float Inclusive = 0.001f;

        public float CenterX;
        public float CenterZ;
        public BrushShape Shape;

        /// <summary>Circle and ring outer radius, square and frame half side, rectangle half width.</summary>
        public float Outer;

        /// <summary>Rectangle half depth, ring inner radius, frame band width (normalized by <see cref="Create"/>).</summary>
        public float Inner;

        public float Hardness;

        /// <summary>Grid mode: every point inside gets the full weight, no fade.</summary>
        public bool Flat;

        private float cos;
        private float sin;
        private float core;

        public static Footprint Create(BrushShape shape, Vector3 center, float outer, float inner, float rotation, float hardness, bool flat)
        {
            Footprint f = new Footprint
            {
                CenterX = center.x, CenterZ = center.z, Shape = shape, Outer = Mathf.Max(outer, 0.05f),
                Hardness = Mathf.Clamp01(hardness), Flat = flat,
            };
            bool turns = shape == BrushShape.Square || shape == BrushShape.Rectangle || shape == BrushShape.Frame;
            float radians = turns ? rotation * Mathf.Deg2Rad : 0f;
            f.cos = Mathf.Cos(radians);
            f.sin = Mathf.Sin(radians);
            f.Normalize(inner);
            return f;
        }

        /// <summary>Clamps the second size to what the shape can use and works out the core distance (t = 0 to t = 1).</summary>
        private void Normalize(float inner)
        {
            switch (Shape)
            {
                case BrushShape.Rectangle:
                    Inner = inner > 0f ? inner : Outer;
                    core = Mathf.Min(Outer, Inner);
                    break;
                case BrushShape.Ring:
                    Inner = Mathf.Clamp(inner, 0f, Outer - 0.05f);
                    core = (Outer - Inner) * 0.5f;
                    break;
                case BrushShape.Frame:
                    Inner = inner > 0f ? Mathf.Min(inner, Outer) : Outer;
                    core = Inner * 0.5f;
                    break;
                default:
                    Inner = 0f;
                    core = Outer;
                    break;
            }
            core = Mathf.Max(core, 0.01f);
        }

        /// <summary>The weight 0..1 of a world point (only X and Z matter).</summary>
        public float Weight(float x, float z)
        {
            float dx = x - CenterX;
            float dz = z - CenterZ;
            float u = dx * cos - dz * sin;
            float v = dx * sin + dz * cos;
            float depth = Depth(u, v);
            if (depth < -Inclusive)
                return NearestPoint(dx, dz);
            if (Flat)
                return 1f;
            return Falloff(1f - Mathf.Max(depth, 0f) / core, Hardness);
        }

        /// <summary>Metres from the point to the nearest rim, positive inside the shape.</summary>
        private float Depth(float u, float v)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            switch (Shape)
            {
                case BrushShape.Square:
                    return Outer - Mathf.Max(au, av);
                case BrushShape.Rectangle:
                    return Mathf.Min(Outer - au, Inner - av);
                case BrushShape.Ring:
                    float r = Mathf.Sqrt(u * u + v * v);
                    return Mathf.Min(Outer - r, r - Inner);
                case BrushShape.Frame:
                    float c = Mathf.Max(au, av);
                    return Mathf.Min(Outer - c, c - (Outer - Inner));
                default:
                    return Outer - Mathf.Sqrt(u * u + v * v);
            }
        }

        /// <summary>
        /// A filled shape smaller than the vertex spacing could miss every vertex; the point nearest to the centre (within
        /// half a metre on both axes) always gets the full effect, so a click never silently does nothing.
        /// </summary>
        private float NearestPoint(float dx, float dz)
        {
            bool filled = Shape == BrushShape.Circle || Shape == BrushShape.Square || Shape == BrushShape.Rectangle;
            return filled && Mathf.Abs(dx) <= 0.5f && Mathf.Abs(dz) <= 0.5f ? 1f : 0f;
        }

        /// <summary>1 up to the hardness, then a smoothstep down to 0 at t = 1.</summary>
        public static float Falloff(float t, float hardness)
        {
            if (t <= hardness)
                return 1f;
            if (t >= 1f)
                return 0f;
            float s = (t - hardness) / (1f - hardness);
            return 1f - s * s * (3f - 2f * s);
        }

        /// <summary>Half the width (X) and depth (Z) of the world-aligned box around the shape, in metres.</summary>
        public void Extent(out float halfX, out float halfZ)
        {
            float ac = Mathf.Abs(cos);
            float asn = Mathf.Abs(sin);
            switch (Shape)
            {
                case BrushShape.Square:
                case BrushShape.Frame:
                    halfX = halfZ = Outer * (ac + asn);
                    break;
                case BrushShape.Rectangle:
                    halfX = Outer * ac + Inner * asn;
                    halfZ = Outer * asn + Inner * ac;
                    break;
                default:
                    halfX = halfZ = Outer;
                    break;
            }
            halfX = Mathf.Max(halfX, 0.5f) + Inclusive;
            halfZ = Mathf.Max(halfZ, 0.5f) + Inclusive;
        }

        /// <summary>A point given in the footprint's local axes (u along the turned X axis, v along the turned Z axis) in world space.</summary>
        public static Vector3 LocalToWorld(Vector3 center, float rotation, float u, float v)
        {
            float radians = rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return new Vector3(center.x + u * c + v * s, center.y, center.z - u * s + v * c);
        }
    }
}
