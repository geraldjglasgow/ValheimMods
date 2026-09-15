using System;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>Small white sprites drawn in code (no asset bundle): a star, a cross, a hollow border (nine-sliced)
    /// and a trash bin. The Image that shows them supplies the colour.</summary>
    public static class StowSprites
    {
        private const int MarkSize = 16;
        private const int BinSize = 24;

        private static Sprite star;
        private static Sprite cross;
        private static Sprite border;
        private static Sprite bin;

        public static Sprite Star => star != null ? star : (star = Make(MarkSize, StarPixel, Vector4.zero));

        public static Sprite Cross => cross != null ? cross : (cross = Make(MarkSize, CrossPixel, Vector4.zero));

        public static Sprite Border => border != null ? border : (border = Make(MarkSize, BorderPixel, new Vector4(3f, 3f, 3f, 3f)));

        public static Sprite Bin => bin != null ? bin : (bin = Make(BinSize, BinPixel, Vector4.zero));

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

        /// <summary>A bin with a lid and a handle, the body striped by two gaps.</summary>
        private static bool BinPixel(int x, int y, int size)
        {
            bool body = x >= 5 && x <= 18 && y >= 2 && y <= 14;
            bool gap = body && (x == 9 || x == 14) && y >= 4 && y <= 12;
            bool lid = x >= 3 && x <= 20 && y >= 16 && y <= 18;
            bool handle = x >= 9 && x <= 14 && y >= 19 && y <= 20;
            return (body && !gap) || lid || handle;
        }
    }
}
