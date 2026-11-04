using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammock's cloth: a copy of the plank wood's own material (the game's piece shader, so rain, snow, the build
    /// highlight and a ship's copy work as on any piece), matte, its normal map cleared, wearing a tanned deer hide painted
    /// once at load over the whole cloth: a warm tan, mottled and grained, two hides sewn across the middle, the long
    /// edges hemmed and stitched, the ends darker where they wrap the bars. A dedicated server, which draws nothing,
    /// keeps the plain copy.
    /// </summary>
    public static class KwHammockCloth
    {
        private const int Long = 128, Wide = 64;
        private static readonly Color Hide = new Color(0.74f, 0.62f, 0.47f);
        private static Material? made;

        /// <summary>The cloth's material, made on first use; null without the plank wood.</summary>
        public static Material? Material => made != null ? made : made = Make();

        private static Material? Make()
        {
            if (KwPrefabs.Planks == null)
                return null;
            Material cloth = GameMaterials.Plain(new Material(KwPrefabs.Planks) { name = "BS_hammock_cloth" }, 0.05f);
            if (cloth.HasProperty("_BumpMap"))
                cloth.SetTexture("_BumpMap", null);
            cloth.color = Color.white;
            if (!KwIcons.CanRender)
                return cloth;
            cloth.mainTexture = Paint();
            cloth.mainTextureScale = Vector2.one;
            cloth.mainTextureOffset = Vector2.zero;
            return cloth;
        }

        // The hide, u along the cloth and v across it, mipmapped, then let go from memory.
        private static Texture2D Paint()
        {
            Texture2D texture = new Texture2D(Long, Wide, TextureFormat.RGBA32, true)
            {
                name = "BS_hammock_hide", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            Color32[] pixels = new Color32[Long * Wide];
            System.Random random = new System.Random(11);
            for (int y = 0; y < Wide; y++)
            {
                for (int x = 0; x < Long; x++)
                {
                    Color c = Hide * Shade(x, y, random);
                    pixels[y * Long + x] = new Color(c.r, c.g, c.b, 1f);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        // How light a texel is: the hide's mottle and grain, darker at its seams, hems and ends.
        private static float Shade(int x, int y, System.Random random)
        {
            float mottle = 0.12f * (Mathf.PerlinNoise(x * 0.05f + 3.1f, y * 0.08f + 7.3f) - 0.5f) + 0.06f * (Mathf.PerlinNoise(x * 0.21f + 1.7f, y * 0.27f + 0.4f) - 0.5f);
            float grain = (float)(random.NextDouble() - 0.5) * 0.05f;
            return (1f + mottle + grain) * Seams(x, y);
        }

        // The hem rolled along each long edge with a row of stitches inside it, the ends round the bars, the seam across the middle.
        private static float Seams(int x, int y)
        {
            int edge = Mathf.Min(y, Wide - 1 - y), end = Mathf.Min(x, Long - 1 - x), seam = Mathf.Abs(x - Long / 2);
            if (edge < 3 || end < 3)
                return 0.8f;
            if (edge == 5 && x % 4 < 2 || seam == 2 && y % 4 < 2)
                return 0.72f;
            return seam < 1 ? 0.76f : 1f;
        }
    }
}
