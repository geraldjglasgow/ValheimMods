using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The shape a patch is drawn in, made here once per kind rather than shipped: a solid pool with a ragged, soft
    /// edge, white so the kind's colour tints it. Mud is lumpy - darker clots and thinner film across it, so it reads
    /// as thick, uneven ground; ice is smooth with a pale sheen across it, so it reads as a glossy sheet; fire is a dim
    /// char shot through with bright embers; roots are thin tangled strands with the ground showing between them. The
    /// game's own decal textures are scatters of droplets, which would make a trail of splashes rather than of ground.
    /// </summary>
    internal static class PatchTexture
    {
        private const int Size = 128;

        /// <summary>Where the pool's edge starts to thin and where it is gone, as a share of the half-width.</summary>
        private const float EdgeIn = 0.7f;
        private const float EdgeOut = 0.95f;

        private static readonly Texture2D?[] Made = new Texture2D?[TrailKind.All.Length];

        /// <summary>The kind's patch texture, made on first use.</summary>
        public static Texture2D For(TrailKind kind)
        {
            Texture2D? texture = Made[kind.Index];
            if (texture == null)
            {
                texture = Make(kind);
                Made[kind.Index] = texture;
            }
            return texture;
        }

        private static Texture2D Make(TrailKind kind)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = $"ecr_{kind.Name}_patch",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            Color32[] texels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    texels[y * Size + x] = Texel(kind, x / (Size - 1f), y / (Size - 1f));
                }
            }
            texture.SetPixels32(texels);
            texture.Apply(true, true);
            return texture;
        }

        // One texel at (u, v), each 0 to 1: how much pool is there (alpha) and how bright it is (grey).
        private static Color32 Texel(TrailKind kind, float u, float v)
        {
            float seed = kind.Index * 17.3f;
            float dx = u * 2f - 1f;
            float dy = v * 2f - 1f;
            float ragged = (Mathf.PerlinNoise(u * 4f + seed, v * 4f + seed) - 0.5f) * 0.35f;
            float reach = Mathf.Sqrt(dx * dx + dy * dy) + ragged;
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(EdgeIn, EdgeOut, reach));
            float grain = Mathf.PerlinNoise(u * 11f + seed, v * 11f - seed);
            Vector2 look = Look(kind, u, v, dx, dy, grain);
            byte g = (byte)(255f * Mathf.Clamp01(look.x));
            return new Color32(g, g, g, (byte)(255f * Mathf.Clamp01(alpha * look.y)));
        }

        // How bright a texel is (x) and how much of the pool shows there (y), by what the patch is.
        private static Vector2 Look(TrailKind kind, float u, float v, float dx, float dy, float grain)
        {
            float seed = kind.Index * 17.3f;
            if (kind.Feel == TrailFeel.Burn)
            {
                return new Vector2(Embers(u, v, seed), 0.85f + 0.15f * grain);
            }
            if (kind.Feel == TrailFeel.Root)
            {
                float strand = Strands(u, v, seed);
                return new Vector2(0.45f + 0.55f * strand, 0.2f + 0.8f * strand);
            }
            return kind.Slips
                ? new Vector2(Sheen(dx, dy, grain), 0.9f + 0.1f * grain)
                : new Vector2(0.7f + 0.3f * grain, 0.8f + 0.2f * grain);
        }

        // Fire: a dim char shot through with bright, hot embers.
        private static float Embers(float u, float v, float seed)
        {
            float hot = Mathf.PerlinNoise(u * 19f + seed, v * 19f + seed);
            return 0.4f + 1.1f * hot * hot;
        }

        // Roots: thin strands along the middles of two ridged noises, crossing each other, and ground between them.
        private static float Strands(float u, float v, float seed)
        {
            float a = 1f - Mathf.Abs(Mathf.PerlinNoise(u * 6f + seed, v * 6f) * 2f - 1f);
            float b = 1f - Mathf.Abs(Mathf.PerlinNoise(u * 9f - seed, v * 9f + seed) * 2f - 1f);
            return Mathf.Pow(Mathf.Max(a, b), 6f);
        }

        // Ice: nearly even, with a soft bright band across it where it catches the light.
        private static float Sheen(float dx, float dy, float grain)
        {
            float band = (dx + dy) * 0.7f - 0.15f;
            return 0.78f + 0.07f * grain + 0.2f * Mathf.Exp(-band * band * 18f);
        }
    }
}
