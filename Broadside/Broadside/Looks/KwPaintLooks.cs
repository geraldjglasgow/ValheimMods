using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The paint a band wears (<see cref="KwPaint"/>): a copy of its tier's planking look (<see cref="KwBiomeLooks"/>, the
    /// game's Custom/Piece) whose albedo is the tier's plank texture painted over, texel by texel: the paint's colour
    /// times how light the wood is there against the texture's own middle, so the grain and the dark seams show through,
    /// with a little of the wood's own colour left in. Paint covers metal and glow, so the copy has neither; its normal map
    /// is the tier's, so the planks keep their relief. The wood is read from its embedded file again (the shown texture is
    /// compressed and no longer readable, <see cref="KwTextures"/>), and the painted copy compressed the same way. A tier
    /// whose texture cannot be read is tinted instead (paint times the wood). Made on first use, once per tier and colour,
    /// only where there is a screen; shared, never changed.
    /// </summary>
    public static class KwPaintLooks
    {
        private const float WoodLeft = 0.12f, Gloss = 0.26f;
        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();

        /// <summary>A tier's planking in a colour of the palette; null before the kit's looks are made.</summary>
        public static Material? For(KwBiomeTier tier, int colour)
        {
            colour = Mathf.Clamp(colour, 0, KwPaint.Colours.Length - 1);
            string name = $"kw_{tier.Id}_paint_{KwPaint.Names[colour]}";
            if (made.TryGetValue(name, out Material look))
                return look;
            Material? wood = KwBiomeLooks.For(KwPart.Hull, tier);
            if (wood == null)
                return null;
            look = new Material(wood) { name = name };
            Painted(look, Readable(wood.mainTexture as Texture2D), KwPaint.Colours[colour]);
            Bare(look);
            made[name] = look;
            return look;
        }

        // The wood's albedo as it can be read: the shown texture when it still can, else its embedded file decoded again.
        private static Texture2D? Readable(Texture2D? shown)
        {
            if (shown == null || shown.isReadable)
                return shown;
            return KwTextures.Load(shown.name + ".png", false, shown.wrapModeU, shown.wrapModeV, readable: true);
        }

        // The albedo painted over (a copy decoded for it let go after); a texture that cannot be read leaves the look tinted.
        private static void Painted(Material look, Texture2D? wood, Color paint)
        {
            if (wood == null || !wood.isReadable)
            {
                look.color = paint;
                return;
            }
            Color32[] texels = wood.GetPixels32();
            float middle = Middle(texels);
            for (int i = 0; i < texels.Length; i++)
                texels[i] = Over(texels[i], paint, middle);
            Texture2D painted = new Texture2D(wood.width, wood.height, TextureFormat.RGBA32, true, false) { name = look.name };
            painted.SetPixels32(texels);
            painted.Apply(true);
            (painted.filterMode, painted.anisoLevel, painted.wrapModeU, painted.wrapModeV) = (FilterMode.Trilinear, 8, wood.wrapModeU, wood.wrapModeV);
            KwTextures.Settle(painted);
            if (wood != look.mainTexture)
                Object.Destroy(wood);
            look.mainTexture = painted;
            look.color = Color.white;
        }

        // One texel painted: the paint as light or dark as the wood is against its middle, a little of the wood left in.
        private static Color32 Over(Color32 wood, Color paint, float middle)
        {
            Color w = wood;
            float grain = Mathf.Pow(Mathf.Clamp(Light(w) / middle, 0.25f, 1.6f), 0.8f);
            Color c = Color.Lerp(paint * grain, w, WoodLeft);
            c.a = w.a;
            return c;
        }

        private static float Light(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // The texture's middle lightness (never quite black, so a dark texture still paints).
        private static float Middle(Color32[] texels)
        {
            double sum = 0;
            foreach (Color32 t in texels)
                sum += Light(t);
            return Mathf.Max(0.05f, (float)(sum / Mathf.Max(1, texels.Length)));
        }

        // No metal and no glow under the paint, a little more gloss than bare wood.
        private static void Bare(Material look)
        {
            if (look.HasProperty("_MetallicTex"))
                look.SetTexture("_MetallicTex", null);
            if (look.HasProperty("_Metallic"))
                look.SetFloat("_Metallic", 0f);
            if (look.HasProperty("_MetallicAlphaGloss"))
                look.SetFloat("_MetallicAlphaGloss", 0f);
            if (look.HasProperty("_EmissionMap"))
                look.SetTexture("_EmissionMap", null);
            if (look.HasProperty("_EmissionColor"))
                look.SetColor("_EmissionColor", Color.black);
            if (look.HasProperty("_Glossiness"))
                look.SetFloat("_Glossiness", Gloss);
        }
    }
}
