using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A normal map made from an embedded relief (<c>_h.png</c>, 256 greys) the way the asset workshop makes its normal
    /// maps (<c>ValheimAssets/Assets/Ships/Keelwright/tier_common.py</c>, <c>normal_from</c>): a texel's relief is the
    /// albedo's value times its strength plus the painted relief, stored as a grey of (relief + 0.5) / <see cref="Scale"/>;
    /// the normal leans against the relief's slope between each texel's neighbours (wrapped both ways), each slope held
    /// to <see cref="Steepest"/>. The long planking textures (2048 by 512 px) carry their relief this way, a quarter of
    /// what their normal maps' files would weigh (1 to 1.8 MB each); the map is made once, when its look is first worn.
    /// </summary>
    public static class KwRelief
    {
        /// <summary>The relief a grey stands for: grey / 255 times this (the base the workshop adds cancels in the slopes).</summary>
        public const float Scale = 5.5f;

        private const float Steepest = 1.4f;

        /// <summary>The normal map of an embedded relief, with its wrap along u and v, settled; null when it is not embedded.</summary>
        public static Texture2D? Normals(string file, TextureWrapMode wrapU, TextureWrapMode wrapV)
        {
            Texture2D? relief = KwTextures.Load(file, true, wrapU, wrapV, readable: true);
            if (relief == null)
                return null;
            Color32[] height = relief.GetPixels32();
            int width = relief.width, rows = relief.height;
            Object.Destroy(relief);
            Color32[] normals = new Color32[height.Length];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < width; x++)
                    normals[y * width + x] = Normal(height, width, rows, x, y);
            }
            Texture2D map = new Texture2D(width, rows, TextureFormat.RGBA32, true, true)
                { name = file.Replace("_h.png", "_n"), filterMode = FilterMode.Trilinear, anisoLevel = 8, wrapModeU = wrapU, wrapModeV = wrapV };
            map.SetPixels32(normals);
            map.Apply(true);
            KwTextures.Settle(map);
            return map;
        }

        // The normal at texel (x, y): the slope along u from the texels either side, and along v from those above and below
        // (Unity counts rows up the image, the workshop down it, so the workshop's next row is Unity's previous one).
        private static Color32 Normal(Color32[] height, int width, int rows, int x, int y)
        {
            int row = y * width;
            float su = Slope(height[row + (x + 1) % width].r - height[row + (x + width - 1) % width].r);
            float sv = -Slope(height[(y + rows - 1) % rows * width + x].r - height[(y + 1) % rows * width + x].r);
            Vector3 n = new Vector3(-su, -sv, 1f).normalized;
            return new Color32(Byte(n.x), Byte(n.y), Byte(n.z), 255);
        }

        private static float Slope(int greys) => Mathf.Clamp(greys * Scale / 255f / 2f, -Steepest, Steepest);

        private static byte Byte(float unit) => (byte)Mathf.Clamp(Mathf.RoundToInt((unit * 0.5f + 0.5f) * 255f), 0, 255);
    }
}
