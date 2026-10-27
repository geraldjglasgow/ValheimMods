using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Holes shot through a launched ship's sails (the user, 2026-10-26: "Holes through sails"), as the tattered raw hide
    /// sail cuts its own (<see cref="KwSailWear"/>): each hole cut out of the cloth's texture under the sail material's
    /// cutout, ragged and with a darker frayed rim (<see cref="KwWearHole"/>), where the shot passed, sized by its ball
    /// (<see cref="KwRigDamageCuts.HoleRadius"/>). A hole's place is kept as a place in the cloth's texture
    /// (<see cref="Uv"/>), so it moves with the cloth as the sail is reefed or furled, and every peer that draws cuts the
    /// same (each hole's outline from its own seed). A holed sail wears its own copy of its material (<see cref="Holed"/>),
    /// its texture read back from the one it wore and cut; mended, it wears that one again.
    /// </summary>
    public static class KwSailDamageHoles
    {
        /// <summary>
        /// Where a point of a sail's cloth (in its mast's kit axes, or its swing's for a sail along the ship) is in its
        /// texture at hoist f; false outside the cloth. A square sail: across its head and down from it; a sail along the
        /// ship: aft along its foot and up, the upper share f of its texture standing over the boom (<see cref="KwSailCanvas"/>).
        /// </summary>
        public static bool Uv(KwSailLayout s, Vector3 kit, float f, out Vector2 uv)
        {
            Vector3 cloth = kit - s.ClothOrigin;
            f = Mathf.Clamp(f, 0.05f, 1f);
            if (s.Square)
            {
                float down = -cloth.z;
                uv = new Vector2(cloth.y / s.Width + 0.5f, 1f - Mathf.Min(down, f * s.Height) / s.Height);
                return uv.x >= 0f && uv.x <= 1f && down >= -0.1f && down <= f * s.Height + 0.5f;
            }
            float u = -cloth.x / Mathf.Max(s.Width, 0.1f), rise = cloth.z - s.Bundle(f);
            float top = s.Spec.Shape == 2 ? KwSailCanvas.Gaff(Mathf.Clamp01(u)) : 1f;
            uv = new Vector2(u, top * (1f - f) + rise / s.Height);
            return u >= 0f && u <= 1f && rise >= -0.1f && Inside(s, uv);
        }

        // Whether a place in the cloth's texture is on the cloth: under a gaff sail's head, under a triangle's diagonal.
        private static bool Inside(KwSailLayout s, Vector2 uv) =>
            uv.y <= (s.Spec.Shape == 2 ? KwSailCanvas.Gaff(uv.x) : s.Spec.Shape == 1 ? 1f - uv.x : 1f) + 0.02f;

        /// <summary>
        /// A cloth's pixels (size by size, bottom row first) with its holes cut, as a cloth of this frame lies in them: each
        /// hole at its place in the texture, its radius in heights of the sail.
        /// </summary>
        public static Color32[] Cut(Color32[] pixels, int size, KwSailFrame frame, float height, List<KwRigMark> holes)
        {
            List<KwWearHole> cut = holes.ConvertAll(h => new KwWearHole(frame.On(h.A, h.B), h.C / Mathf.Max(height, 0.1f), 1.15f, h.Seed % 180, h.Seed));
            Color32[] made = (Color32[])pixels.Clone();
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    KwWearTexel t = new KwWearTexel(pixels, size, i, j, frame.On((i + 0.5f) / size, (j + 0.5f) / size));
                    made[j * size + i] = Texel(made[j * size + i], t, cut);
                }
            }
            return made;
        }

        // A texel cut out where a hole takes it, darkened on a hole's frayed rim.
        private static Color32 Texel(Color32 c, KwWearTexel t, List<KwWearHole> holes)
        {
            float shade = 1f;
            bool gone = false;
            foreach (KwWearHole hole in holes)
                gone |= hole.Cut(t, ref shade);
            return shade >= 1f && !gone ? c : KwWearNoise.Shade(c, new Vector3(shade, shade, shade), gone ? (byte)0 : c.a);
        }

        /// <summary>A copy of a sail's material wearing its texture with its holes cut; null when it cannot be read.</summary>
        public static Material? Holed(Material worn, KwSailSpec spec, List<KwRigMark> holes)
        {
            Texture? source = worn.mainTexture;
            if (source == null || !KwSailPixels.Drawn)
                return null;
            int size = KwSailLooks.Size;
            KwSailFrame frame = KwSailFrame.Of(spec.Shape, spec.Width, spec.Height);
            Color32[] pixels = Cut(KwSailPixels.Read(source, size, size), size, frame, spec.Height, holes);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "broadside_sail_holed", wrapMode = TextureWrapMode.Clamp, filterMode = source.filterMode, anisoLevel = source.anisoLevel,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return new Material(worn) { name = worn.name + " (holed)", mainTexture = texture };
        }
    }
}
