using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a keel, its post, a rib and a post cap sit on their biome tier's keel texture (<see cref="KwBiomeLooks"/>,
    /// 128 px square, repeating along u, 32 px a metre like the game's building pieces). Rows from the texture's top:
    /// 0 to 31 the keel's sides, measured up from its underside (the shoe at row 31, so a deeper keel shows more of the
    /// band, up to a metre); 32 to 43 its top and 44 to 55 its underside, across from the centre line; 56 to 63 a rib's
    /// faces, across each face; 64 to 127 a plain carving swatch the post caps' own planar UVs are moved onto. Along u a
    /// keel and a rib run along their length and a post up its height; its after or fore face (the one at the keel's end)
    /// takes the underside's band, so a shod keel is shod up its post too. Kit axes.
    /// </summary>
    public static class KwKeelUv
    {
        private const float Size = 128f, Density = 32f;

        public static float Along(float metres) => metres * Density / Size;

        /// <summary>v on the keel's sides, a height over its underside.</summary>
        public static float Side(float height) => Row(32f - height * Density, 0f, 32f);

        /// <summary>v on the keel's top, across from its centre line.</summary>
        public static float Top(float across) => Row(38f + across * Density, 32f, 44f);

        /// <summary>v on the keel's underside, across from its centre line.</summary>
        public static float Bottom(float across) => Row(50f + across * Density, 44f, 56f);

        /// <summary>v on a rib's face, a share across it (0 one edge, 1 the other).</summary>
        public static float Rib(float share) => Row(56f + Mathf.Clamp01(share) * 8f, 56f, 64f);

        /// <summary>A post cap's planar UV (two metres a texture) on the carving swatch, at the same density as the rest.</summary>
        public static Vector2 Carving(Vector2 planar) => new Vector2(planar.x * 0.5f, Mathf.Clamp(0.25f + planar.y * 0.5f, 0.5f / Size, 0.5f - 0.5f / Size));

        // A texture row (from the top) kept half a texel inside its band, as v.
        private static float Row(float row, float top, float bottom) => 1f - Mathf.Clamp(row, top + 0.5f, bottom - 0.5f) / Size;

        /// <summary>
        /// A keel segment's UVs (<see cref="Solid.Hexa(MeshBuilder, Vector3[], Func{int, int, Vector3, Vector2})"/>: faces
        /// underside, top, the two sides and the two ends; corners 0 to 3 under, 4 to 7 on top): sides and ends up from the
        /// underside, top and underside across.
        /// </summary>
        public static Func<int, int, Vector3, Vector2> Keel(float depth) => (face, corner, p) => face switch
        {
            0 => new Vector2(Along(p.x), Bottom(p.y)),
            1 => new Vector2(Along(p.x), Top(p.y)),
            2 or 4 => new Vector2(Along(p.x), Side(corner < 4 ? 0f : depth)),
            _ => new Vector2(Along(p.y), Side(corner < 4 ? 0f : depth)),
        };

        /// <summary>
        /// A post's UVs: its sides run up the post, from its outer face (the one at the keel's end, at the post's far x when
        /// outerFar) inward; the outer face takes the underside's band, the inner face and the top the top's.
        /// </summary>
        public static Func<int, int, Vector3, Vector2> Post(bool outerFar, float depth) => (face, corner, p) =>
        {
            bool far = corner % 4 == 1 || corner % 4 == 2;
            return face switch
            {
                0 or 1 => new Vector2(Along(p.x), Top(p.y)),
                2 or 4 => new Vector2(Along(p.z), Side(far == outerFar ? 0f : depth)),
                _ => new Vector2(Along(p.z), (face == 3) == outerFar ? Bottom(p.y) : Top(p.y)),
            };
        };

        /// <summary>
        /// One bar of a rib, from its section point a (sa metres round the frame from the keel) to c (sc): along it on u;
        /// across its fore and aft faces from outside to inside, across its outer and inner faces from fore to aft.
        /// </summary>
        public static Func<int, int, Vector3, Vector2> Bar(float sa, float sc) => (face, corner, p) =>
        {
            int ring = corner % 4;
            float u = Along(ring == 0 || ring == 3 ? sa : sc);
            float share = face <= 1 ? (ring >= 2 ? 1f : 0f) : (corner >= 4 ? 1f : 0f);
            return new Vector2(u, Rib(share));
        };

        /// <summary>A rib's floor timber across the keel: along it (across the ship) on u, across its faces on the rib band.</summary>
        public static Func<int, int, Vector3, Vector2> Floor() => (face, corner, p) =>
            new Vector2(Along(p.y), Rib(face <= 1 ? (p.x > 0f ? 1f : 0f) : (corner >= 4 ? 1f : 0f)));
    }
}
