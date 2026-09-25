using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>The Stow module's sprites. The slot marks are small white shapes drawn in code, coloured by the Image
    /// that shows them: a star, a cross and a hollow border (nine-sliced). The trash can's icon is a PNG embedded in
    /// the mod (<c>assets/trash.png</c>, 128 px) and keeps its own colours.</summary>
    public static class StowSprites
    {
        private const int MarkSize = 16;
        private const string TrashResource = "OpenKeep.assets.trash.png";

        private static Sprite star;
        private static Sprite cross;
        private static Sprite border;
        private static Sprite bin;

        public static Sprite Star => star != null ? star : (star = Make(MarkSize, StarPixel, Vector4.zero));

        public static Sprite Cross => cross != null ? cross : (cross = Make(MarkSize, CrossPixel, Vector4.zero));

        public static Sprite Border => border != null ? border : (border = Make(MarkSize, BorderPixel, new Vector4(3f, 3f, 3f, 3f)));

        /// <summary>The trash can's icon; null when the embedded image cannot be read, which is logged.</summary>
        public static Sprite Bin => bin != null ? bin : (bin = Load(TrashResource));

        private static Sprite Make(int size, Func<int, int, int, bool> inside, Vector4 slices)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, inside(x, y, size) ? Color.white : Color.clear);
            }
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, slices);
            sprite.name = "OpenKeep_sprite";
            return sprite;
        }

        /// <summary>A sprite from a PNG embedded in the mod, with mipmaps so it stays smooth at any interface scale.</summary>
        private static Sprite Load(string resource)
        {
            byte[] bytes = ReadResource(resource);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (bytes == null || !Decode(texture, bytes))
            {
                Plugin.Log.LogWarning($"could not read the embedded image {resource}");
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            Plugin.Log.LogInfo($"{resource}: {texture.width}x{texture.height}, {texture.mipmapCount} mip levels");
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "OpenKeep_sprite";
            return sprite;
        }

        /// <summary>The game's PNG decoder, <c>ImageConversion.LoadImage(Texture2D, byte[], bool)</c>, which also builds
        /// the mipmaps. Its module is built against netstandard 2.1 (for a <c>ReadOnlySpan</c> overload), which this
        /// net48 project cannot reference (CS1705), so the method is found at runtime; the texture is marked
        /// non-readable afterwards.</summary>
        private static bool Decode(Texture2D texture, byte[] png)
        {
            MethodInfo loadImage = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
            return loadImage != null && (bool)loadImage.Invoke(null, new object[] { texture, png, true });
        }

        private static byte[] ReadResource(string resource)
        {
            using (Stream stream = typeof(StowSprites).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                    return null;
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        /// <summary>A five pointed star: the radius at an angle swings between the inner and the outer radius.</summary>
        private static bool StarPixel(int x, int y, int size)
        {
            float half = size / 2f;
            float dx = x + 0.5f - half;
            float dy = y + 0.5f - half;
            float distance = Mathf.Sqrt(dx * dx + dy * dy) / half;
            float turns = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.25f) * 5f;
            float toPoint = 1f - Mathf.Abs(turns - Mathf.Floor(turns) - 0.5f) * 2f;
            float radius = 0.42f + (0.98f - 0.42f) * (1f - toPoint);
            return distance <= radius;
        }

        private static bool CrossPixel(int x, int y, int size)
        {
            const float inset = 2f;
            const float thickness = 1.6f;
            if (x < inset || y < inset || x >= size - inset || y >= size - inset)
                return false;
            return Mathf.Abs(x - y) <= thickness || Mathf.Abs(x + y - (size - 1)) <= thickness;
        }

        private static bool BorderPixel(int x, int y, int size)
        {
            const int width = 2;
            return x < width || y < width || x >= size - width || y >= size - width;
        }
    }
}
