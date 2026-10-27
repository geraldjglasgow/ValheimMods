using System.IO;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The kit's own textures, painted in the asset workshop (<c>ValheimAssets/Assets/Ships/Keelwright</c>) and embedded
    /// from <c>Broadside/assets</c> as <c>Broadside.assets.&lt;file&gt;</c>: decoded once and mipmapped, a normal map or
    /// mask read as linear, an albedo or glow map as sRGB. Filtered smoothly between texels and mip levels, with
    /// anisotropic filtering, unlike the game's point filtering: the planking and deck textures' thin, dark board seams
    /// (and their grooves in the normal maps) flickered on and off point filtered at the low angle a deck is seen at,
    /// strobing as the view moves and rippling like a mirage on a curved hull (measured 2026-10-24: 12% of a deck's pixels
    /// flickering frame to frame point filtered, under 3% smooth). A big texture (the planking's, 2048 by 512 px) is
    /// compressed on the graphics card as the game's own are and its copy in memory let go (<see cref="Settle"/>), unless
    /// it is asked for readable, to be painted from (<see cref="KwPaintLooks"/>).
    /// </summary>
    public static class KwTextures
    {
        // The longest side a texture keeps uncompressed and readable: the kit's 128 px textures stay as they were.
        private const int Small = 256;

        // The game's PNG decoder, ImageConversion.LoadImage(Texture2D, byte[], bool), found by name: its module cannot be
        // referenced here (it is built against a newer netstandard).
        private static readonly System.Reflection.MethodInfo? Decoder =
            System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        /// <summary>
        /// An embedded texture with its wrap along u and v, a big one settled unless asked for readable; null when it is
        /// not embedded or cannot be read.
        /// </summary>
        public static Texture2D? Load(string file, bool linear, TextureWrapMode wrapU, TextureWrapMode wrapV, bool readable = false)
        {
            using Stream? stream = typeof(KwTextures).Assembly.GetManifestResourceStream("Broadside.assets." + file);
            if (stream == null)
                return null;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) { name = Path.GetFileNameWithoutExtension(file) };
            if (Decoder == null || !(bool)Decoder.Invoke(null, new object[] { texture, bytes, false }))
            {
                Object.Destroy(texture);
                return null;
            }
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 8;
            texture.wrapModeU = wrapU;
            texture.wrapModeV = wrapV;
            if (!readable)
                Settle(texture);
            return texture;
        }

        /// <summary>
        /// A texture longer than 256 px a side compressed (DXT, as the game's own textures are: a quarter of the memory or
        /// less; the quick way, so a tier first worn in play costs a short hitch) and its copy in memory let go, so it can
        /// no longer be read; a smaller one as it is.
        /// </summary>
        public static void Settle(Texture2D texture)
        {
            if (Mathf.Max(texture.width, texture.height) <= Small)
                return;
            texture.Compress(false);
            texture.Apply(false, true);
        }
    }
}
