using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The paint colours, as the game's own paint does them. The paint mask's channels are r dirt, g cultivated, b paved
    /// and a vegetation. Colour paints blend r, g and b toward the game's colour and keep the vegetation (alpha), as the
    /// game does; the vegetation paints change only the alpha. Cultivating in the Deep North piles snow instead (the
    /// cultivated channel grows), as the game does there. "Original" blends everything back to the generated mask and,
    /// at full weight, forgets the cell so the world's own ground shows again.
    /// </summary>
    public static class PaintOps
    {
        // The game's paint colours: Heightmap.m_paintMaskDirt, m_paintMaskCultivated, m_paintMaskPaved, m_paintMaskNothing
        // (the game's "reset" paint, grass) and m_paintMaskDeepSnow. The channels are fixed by the terrain shader and the
        // saved terrain data, so the values are spelled out here; only r, g and b are used, alpha is kept.
        public static readonly Color Dirt = new Color(1f, 0f, 0f, 1f);
        public static readonly Color Cultivated = new Color(0f, 1f, 0f, 1f);
        public static readonly Color Paved = new Color(0f, 0f, 1f, 1f);
        public static readonly Color Grass = new Color(0f, 0f, 0f, 1f);
        public static readonly Color DeepSnow = new Color(1f, 1f, 1f, 1f);

        /// <summary>The colour after painting <paramref name="from"/> by <paramref name="weight"/>; false for no paint.</summary>
        public static bool Blend(PaintOp op, Color from, Color original, float weight, float density, bool snow, out Color to, out bool forget)
        {
            forget = false;
            to = from;
            switch (op)
            {
                case PaintOp.Dirt: to = Rgb(from, Dirt, weight); return true;
                case PaintOp.Cultivated: to = snow ? Snow(from, weight) : Rgb(from, Cultivated, weight); return true;
                case PaintOp.Paved: to = Rgb(from, Paved, weight); return true;
                case PaintOp.Grass: to = Rgb(from, Grass, weight); return true;
                case PaintOp.DeepSnow: to = Rgb(from, DeepSnow, weight); return true;
                case PaintOp.ClearVegetation: to.a = Mathf.Lerp(from.a, 0f, weight); return true;
                case PaintOp.Vegetation: to.a = Mathf.Lerp(from.a, density, weight); return true;
                case PaintOp.Original:
                    to = Color.Lerp(from, original, weight);
                    forget = weight >= 0.999f;
                    return true;
                default: return false;
            }
        }

        private static Color Rgb(Color from, Color target, float weight)
        {
            return new Color(Mathf.Lerp(from.r, target.r, weight), Mathf.Lerp(from.g, target.g, weight), Mathf.Lerp(from.b, target.b, weight), from.a);
        }

        /// <summary>The game's Deep North cultivating: the cultivated channel (snow depth there) grows by the weight.</summary>
        private static Color Snow(Color from, float weight)
        {
            Color to = from;
            to.g = Mathf.Clamp01(from.g + weight);
            return to;
        }
    }
}
