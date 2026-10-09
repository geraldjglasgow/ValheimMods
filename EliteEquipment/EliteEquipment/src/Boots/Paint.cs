using System;
using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Body paint texels in memory (bottom row first, as Unity keeps them), with the sampling of the texture they came
    /// from; laid one over another (<see cref="Over"/>) and made into a texture for the body.
    /// </summary>
    internal sealed class Paint
    {
        private Paint(Color32[] pixels, int width, int height, FilterMode filter, TextureWrapMode wrap, int aniso, bool linear = false)
        {
            Linear = linear;
            Pixels = pixels;
            Width = width;
            Height = height;
            Filter = filter;
            Wrap = wrap;
            Aniso = aniso;
        }

        public Color32[] Pixels { get; }
        public int Width { get; }
        public int Height { get; }
        private FilterMode Filter { get; }
        private TextureWrapMode Wrap { get; }
        private int Aniso { get; }

        /// <summary>A data map (metal, normal): read and written without the sRGB conversion.</summary>
        private bool Linear { get; }

        /// <summary>A texture's texels through the GPU; null without a graphics device.</summary>
        public static Paint Read(Texture texture)
        {
            if (texture == null)
                return null;
            Color32[] pixels = TexturePixels.Read(texture, out int width, out int height);
            return pixels == null ? null : new Paint(pixels, width, height, texture.filterMode, texture.wrapMode, texture.anisoLevel);
        }

        /// <summary>A data map's texels (metal, normal) through the GPU, without the sRGB conversion; null without a graphics device.</summary>
        public static Paint ReadLinear(Texture texture)
        {
            if (texture == null)
                return null;
            Color32[] pixels = TexturePixels.ReadLinear(texture, out int width, out int height);
            return pixels == null ? null : new Paint(pixels, width, height, texture.filterMode, texture.wrapMode, texture.anisoLevel, true);
        }

        public Paint Copy() => new Paint((Color32[])Pixels.Clone(), Width, Height, Filter, Wrap, Aniso, Linear);

        /// <summary>The array index of the texel at <paramref name="x"/>, <paramref name="y"/> counted from the top-left.</summary>
        public int Index(int x, int y) => (Height - 1 - y) * Width + x;

        /// <summary>
        /// <paramref name="top"/> laid over <paramref name="under"/> by its alpha, at the larger size of the two (each
        /// sampled to its nearest texel); the sampling stays the one underneath. Either may be null.
        /// </summary>
        public static Paint Over(Paint top, Paint under)
        {
            if (top == null || under == null)
                return top ?? under;
            int width = Math.Max(top.Width, under.Width);
            int height = Math.Max(top.Height, under.Height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = Over(top.At(x, y, width, height), under.At(x, y, width, height));
            }
            return new Paint(pixels, width, height, under.Filter, under.Wrap, under.Aniso);
        }

        public Texture2D ToTexture(string name)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true, Linear)
            {
                name = name,
                filterMode = Filter,
                wrapMode = Wrap,
                anisoLevel = Aniso,
            };
            texture.SetPixels32(Pixels);
            texture.Apply(true, true);
            return texture;
        }

        private Color32 At(int x, int y, int width, int height) => Pixels[y * Height / height * Width + x * Width / width];

        private static Color32 Over(Color32 top, Color32 under)
        {
            if (top.a == 255 || under.a == 0)
                return top;
            if (top.a == 0)
                return under;
            float a = top.a / 255f;
            float b = under.a / 255f * (1f - a);
            float sum = a + b;
            return new Color32((byte)((top.r * a + under.r * b) / sum), (byte)((top.g * a + under.g * b) / sum),
                (byte)((top.b * a + under.b * b) / sum), (byte)Mathf.RoundToInt(sum * 255f));
        }
    }
}
