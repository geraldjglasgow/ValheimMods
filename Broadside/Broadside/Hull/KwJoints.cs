using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The joint table, the same numbers as the workshop's fit kit (ValheimAssets
    /// <c>Assets/Ships/Keelwright/fit_kit/profiles.py</c>): a hull joint is a family (the cross-section's shape) and a size
    /// (beam, keel height, sheer height). Two hull pieces fit when both sides of a joint carry the same family and size; a
    /// taper is the only piece that changes size; a rib of the player's own (<see cref="KwFrame"/>) is any beam and height.
    /// Kit axes: x along the ship (bow +x), y across (port +y), z up, metres
    /// from the ship's datum; <see cref="KwMesh"/> turns them into Unity's.
    /// </summary>
    public static class KwJoints
    {
        public const float Bulwark = 0.9f;

        /// <summary>How far an oval stem's sheer climbs, as a share of the hull's depth (the game's ships, measured).</summary>
        public const float OvalRise = 0.8f;

        // size: beam, keel z, sheer z. Every size hangs from the same keel line (z = 0), so one keel fits under all of them
        // and a taper keeps the keel straight; a bigger size is wider and taller.
        private static readonly Dictionary<int, Vector3> Sizes = new Dictionary<int, Vector3>
        {
            [1] = new Vector3(2f, 0f, 1.2f),
            [2] = new Vector3(3f, 0f, 1.35f),
            [3] = new Vector3(4f, 0f, 1.6f),
            [4] = new Vector3(5f, 0f, 1.9f),
            [5] = new Vector3(6f, 0f, 2.2f),
            [6] = new Vector3(7f, 0f, 2.5f),
            [7] = new Vector3(8f, 0f, 2.8f),
        };

        // half section from the keel to the sheer: (fraction of the half beam, fraction of the depth)
        private static readonly Dictionary<string, float[]> Families = new Dictionary<string, float[]>
        {
            ["viking"] = new[] { 0f, 0f, 0.16f, 0.10f, 0.40f, 0.28f, 0.64f, 0.50f, 0.84f, 0.72f, 0.95f, 0.88f, 1f, 1f },
            ["round"] = new[] { 0f, 0f, 0.34f, 0.03f, 0.64f, 0.12f, 0.86f, 0.30f, 0.96f, 0.55f, 1f, 0.80f, 1f, 1f },
            ["galleon"] = new[] { 0f, 0f, 0.34f, 0.03f, 0.68f, 0.12f, 0.90f, 0.30f, 1f, 0.52f, 0.98f, 0.76f, 0.90f, 1f },
            // the game's longship measured at midship: a keel fin, then a round bilge; eight strakes
            ["rounded"] = new[] { 0f, 0f, 0.05f, 0.10f, 0.28f, 0.25f, 0.55f, 0.36f, 0.74f, 0.48f, 0.87f, 0.60f, 0.95f, 0.72f, 0.99f, 0.86f, 1f, 1f },
            // a barge's flat bottom, a hard chine and sides that flare a little
            ["flat"] = new[] { 0f, 0f, 0.45f, 0.01f, 0.82f, 0.03f, 0.95f, 0.08f, 0.98f, 0.30f, 0.99f, 0.60f, 1f, 1f },
            // a sharp, nearly straight V from the keel to the sheer, for fast hulls
            ["deep"] = new[] { 0f, 0f, 0.18f, 0.15f, 0.36f, 0.30f, 0.55f, 0.46f, 0.72f, 0.62f, 0.86f, 0.78f, 0.95f, 0.90f, 1f, 1f },
            // a wineglass: hollow beside the keel, then a full round bilge, as later ships were built
            ["wineglass"] = new[] { 0f, 0f, 0.06f, 0.14f, 0.20f, 0.24f, 0.45f, 0.31f, 0.70f, 0.42f, 0.88f, 0.58f, 0.97f, 0.78f, 1f, 1f },
        };

        private static readonly Dictionary<string, float> StemRises = new Dictionary<string, float>
        {
            ["viking"] = 1.4f, ["round"] = 0.9f, ["galleon"] = 0.6f, ["rounded"] = 1f, ["flat"] = 0.5f, ["deep"] = 1.2f, ["wineglass"] = 0.8f,
        };

        /// <summary>A joint table size as a frame: its beam and its sheer's height over the keel line.</summary>
        public static KwFrame Preset(int size) => new KwFrame(Sizes[size].x, Sizes[size].z, size);

        public static float StemRise(string family) => StemRises[family];
        public static float MainDeckZ(KwFrame frame) => frame.Height - Bulwark;

        /// <summary>The family's half section scaled to a frame, as (half width, height) from the keel up.</summary>
        public static Vector2[] Section(string family, KwFrame frame) => Shape(family, frame, 1f, 0f, frame.Height);

        /// <summary>The family's half section at a width factor between a keel and a sheer height.</summary>
        // Every section starts at the keel's top edge (the keel's half width out from the centre line), so each hull side
        // is set into the keel's side and the keel shows below the hull; width narrows only what lies outside the keel.
        public static Vector2[] Shape(string family, KwFrame frame, float width, float keel, float sheer)
        {
            float[] uv = Families[family];
            Vector2[] half = new Vector2[uv.Length / 2];
            float reach = Mathf.Max(frame.Beam / 2f - frame.KeelHalf, 0.1f);
            for (int i = 0; i < half.Length; i++)
                half[i] = new Vector2(frame.KeelHalf + uv[2 * i] * reach * width, keel + uv[2 * i + 1] * (sheer - keel));
            return half;
        }

        public static Vector2[] Blend(Vector2[] a, Vector2[] b, float w)
        {
            Vector2[] mixed = new Vector2[a.Length];
            for (int i = 0; i < a.Length; i++)
                mixed[i] = Vector2.Lerp(a[i], b[i], w);
            return mixed;
        }

        public static float Smoothstep(float t) => t * t * (3f - 2f * t);
    }

    /// <summary>One cross-section along a piece: its x and its half column (half width, height) from bottom to top.</summary>
    public readonly struct Station
    {
        public Station(float x, Vector2[] half)
        {
            X = x;
            Half = half;
        }

        public float X { get; }
        public Vector2[] Half { get; }
    }
}
