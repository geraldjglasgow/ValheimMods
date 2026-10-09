using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A look's textures and where things lie on them. Each of the three (albedo, normal map, metal map) is copied
    /// together once per client from the game's chest textures by the look's recipe (<see cref="LegStyle.Recipe"/>):
    /// its mesh textures or its body paint, read through the GPU, the data maps without the sRGB conversion. The game's
    /// art is copied at runtime only, never shipped. Atlas rows count from the top; a step whose source texture is
    /// missing leaves a flat normal and no metal.
    /// </summary>
    internal static class FittedAtlas
    {
        public const int Size = 256;

        private static readonly Color32[] Blank = { new Color32(0, 0, 0, 255), new Color32(128, 128, 255, 255), new Color32(0, 0, 0, 0) };

        /// <summary>Channel 0 albedo, 1 normal map, 2 metal map; <paramref name="sources"/> per <see cref="AtlasSource"/>.</summary>
        public static Texture2D Build(LegStyle style, Texture[] sources, int channel)
        {
            var atlas = new Color32[Size * Size];
            for (int i = 0; i < atlas.Length; i++)
                atlas[i] = Blank[channel];
            var read = new Source[sources.Length];
            foreach (AtlasCopy step in style.Recipe)
            {
                Texture texture = sources[(int)step.Source];
                if (texture == null)
                    continue;
                if (read[(int)step.Source].Pixels == null)
                    read[(int)step.Source] = Source.Of(texture, channel > 0);
                Fill(read[(int)step.Source], step, atlas);
            }
            if (channel == 0)
                Opaque(atlas);
            return ToTexture(atlas, "EE_Fitted" + style.Key + "_" + channel, channel > 0);
        }

        /// <summary>
        /// Every albedo texel solid: some chests cut holes through their alpha (flametal's mail rings), and the leggings
        /// must never be seen through, so the holes draw as the dark they are painted.
        /// </summary>
        private static void Opaque(Color32[] atlas)
        {
            for (int i = 0; i < atlas.Length; i++)
                atlas[i].a = 255;
        }

        /// <summary>The texture round the body: by arc length from the seam, down from the waist.</summary>
        public static Vector2 Field(LegStyle style, float radius, float angle, float height) =>
            Px(style.FieldOrigin.x + angle * radius * style.Density, style.FieldOrigin.y + (LegRegion.Waist - height) * style.Density);

        /// <summary>A region's place for a turn fraction round the body (<paramref name="along"/>) and a fraction across.</summary>
        public static Vector2 Region(RectInt region, float along, float across, bool rotated)
        {
            along = Mathf.Clamp(along, 0f, 1.1f) / 1.1f;
            return rotated
                ? Px(region.x + across * region.width, region.y + along * region.height)
                : Px(region.x + 2f + along * (region.width - 4f), region.y + across * region.height);
        }

        public static Vector2 Px(float x, float y) => new Vector2(x / Size, 1f - y / Size);

        private static void Fill(Source from, AtlasCopy step, Color32[] atlas)
        {
            RectInt f = step.From, to = step.To;
            for (int y = 0; y < to.height; y++)
            {
                for (int x = 0; x < to.width; x++)
                {
                    int sx = Repeat(x, f.width, step.Fill), sy = Repeat(y, f.height, step.Fill);
                    Put(atlas, to.x + x, to.y + y, from.At(f.x + sx, f.y + sy, step.Base));
                }
            }
        }

        private static int Repeat(int i, int size, AtlasFill fill)
        {
            if (fill == AtlasFill.Copy)
                return Mathf.Min(i, size - 1);
            int local = i % size;
            return fill == AtlasFill.Mirror && i / size % 2 == 1 ? size - 1 - local : local;
        }

        private static void Put(Color32[] atlas, int x, int y, Color32 colour)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
                atlas[(Size - 1 - y) * Size + x] = colour;
        }

        private static Texture2D ToTexture(Color32[] atlas, string name, bool data)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, data)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(atlas);
            texture.Apply(true, true);
            return texture;
        }

        /// <summary>A source texture's texels by their place on its base layout, whatever its real size.</summary>
        private readonly struct Source
        {
            private Source(Color32[] pixels, int width, int height)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
            }

            public Color32[] Pixels { get; }
            private int Width { get; }
            private int Height { get; }

            public static Source Of(Texture texture, bool data)
            {
                Color32[] pixels = data ? TexturePixels.ReadLinear(texture, out int w, out int h) : TexturePixels.Read(texture, out w, out h);
                return new Source(pixels ?? new Color32[1], pixels != null ? w : 1, pixels != null ? h : 1);
            }

            public Color32 At(int x, int y, int layout)
            {
                int px = Mathf.Clamp(x * Width / layout, 0, Width - 1), py = Mathf.Clamp(y * Height / layout, 0, Height - 1);
                return Pixels[(Height - 1 - py) * Width + px];
            }
        }
    }
}
