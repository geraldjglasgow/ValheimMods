using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hammer icon made from the same view drawn twice, on black and on white: a sample's alpha is how little it changed
    /// between the two and its colour the black one's (the subject over nothing), so the background comes out clear and
    /// the subject keeps its edges' softness. The samples are averaged square by square down to the icon's size, the clear
    /// pixels take the subject's mean colour (so its mipmaps do not darken its outline), and the icon is a mipmapped,
    /// smoothly filtered sprite like the painted ones. Null when nothing was drawn.
    /// </summary>
    public static class KwIconPicture
    {
        private const float Clear = 0.02f;

        /// <summary>
        /// The sprite from two square renders of width samples, step samples to the icon's pixel, and its pixels (bottom
        /// row first) for the icon cache (<see cref="KwIconCache"/>).
        /// </summary>
        public static Sprite? Sprite(Color32[] black, Color32[] white, int width, int step, string name, out Color32[]? kept)
        {
            int size = width / step;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Pixel(black, white, width, x * step, y * step, step);
            }
            kept = null;
            if (!Fill(pixels))
                return null;
            kept = System.Array.ConvertAll(pixels, c => (Color32)c);
            return Make(kept, size, name);
        }

        /// <summary>An icon's sprite from its pixels (bottom row first): mipmapped, smoothly filtered, not kept readable.</summary>
        public static Sprite Make(Color32[] pixels, int size, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name + "_icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            Sprite sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name + "_icon";
            return sprite;
        }

        // The average of a square of samples, colour taken over alpha.
        private static Color Pixel(Color32[] black, Color32[] white, int width, int x0, int y0, int step)
        {
            Color sum = Color.clear;
            for (int y = y0; y < y0 + step; y++)
            {
                for (int x = x0; x < x0 + step; x++)
                    sum += Sample(black[y * width + x], white[y * width + x]);
            }
            sum /= step * step;
            if (sum.a < Clear)
                return Color.clear;
            return new Color(Mathf.Clamp01(sum.r / sum.a), Mathf.Clamp01(sum.g / sum.a), Mathf.Clamp01(sum.b / sum.a), sum.a);
        }

        // One sample with its colour times its alpha: on black that is just what was drawn.
        private static Color Sample(Color32 black, Color32 white)
        {
            int change = Mathf.Max(white.r - black.r, Mathf.Max(white.g - black.g, white.b - black.b));
            float alpha = Mathf.Clamp01(1f - change / 255f);
            return new Color(black.r / 255f, black.g / 255f, black.b / 255f, alpha);
        }

        // The clear pixels take the subject's mean colour; false when there is no subject at all.
        private static bool Fill(Color[] pixels)
        {
            Color sum = Color.clear;
            foreach (Color pixel in pixels)
                sum += new Color(pixel.r * pixel.a, pixel.g * pixel.a, pixel.b * pixel.a, pixel.a);
            if (sum.a < 1f)
                return false;
            Color mean = new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, 0f);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < Clear)
                    pixels[i] = mean;
            }
            return true;
        }
    }
}
