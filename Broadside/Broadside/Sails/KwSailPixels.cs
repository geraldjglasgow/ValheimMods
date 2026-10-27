using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// Reading pictures for the sails' patterns (<see cref="KwSailLooks"/>, <see cref="KwSailEmblems"/>): a PNG decoded with
    /// the game's own decoder, and any texture's pixels at a size, read back from the graphics card when the texture keeps
    /// no copy of its own (the game's are not readable). Only where there is a screen.
    /// </summary>
    public static class KwSailPixels
    {
        // The game's PNG decoder, ImageConversion.LoadImage(Texture2D, byte[], bool), found by name: its module cannot be
        // referenced here (it is built against a newer netstandard).
        private static readonly MethodInfo? Decoder =
            System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        /// <summary>A PNG's texture, readable, with mipmaps (a normal map read as linear); null when it does not decode.</summary>
        public static Texture2D? Decode(byte[] bytes, string name, bool linear)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) { name = name };
            if (Decoder != null && (bool)Decoder.Invoke(null, new object[] { texture, bytes, false }))
                return texture;
            Object.Destroy(texture);
            return null;
        }

        /// <summary>A texture's pixels at width by height (bottom row first): its own copy when it keeps one at that size, else drawn and read back.</summary>
        public static Color32[] Read(Texture texture, int width, int height)
        {
            if (texture is Texture2D own && own.isReadable && own.width == width && own.height == height)
                return own.GetPixels32();
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture was = RenderTexture.active;
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            Texture2D read = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            read.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(target);
            Color32[] pixels = read.GetPixels32();
            Object.Destroy(read);
            return pixels;
        }

        /// <summary>Whether this game draws (false on a dedicated server, which paints nothing).</summary>
        public static bool Drawn => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    }
}
