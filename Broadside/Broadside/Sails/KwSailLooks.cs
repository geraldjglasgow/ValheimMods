using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The materials sails wear, their patterns painted on (<see cref="KwSailPattern"/>): a plain sail wears its cloth's own
    /// material (<see cref="KwSailCloth"/>); a patterned one (and raw hide always, worn, <see cref="KwSailWear"/>) a copy of it wearing a texture of its own, the cloth's texture
    /// read at <see cref="Size"/> pixels with the pattern painted over it (<see cref="KwSailPaint"/>), so the cloth's
    /// stitched panels, its hide or linen and its normal map stay under the colour. Made the first time a cloth and pattern
    /// (and, for a border or an emblem, the sail's proportions, <see cref="KwSailFrame"/>) are asked for and kept, so every
    /// sail alike, on the dock, on every launched ship and in the hammer's ghosts, shares one; a launched ship's copies for
    /// moving (<see cref="KwShipMaterials"/>) are made from these as from any other. A dedicated server draws nothing and
    /// paints nothing: it keeps the plain cloth. See-through copies for the tools' previews (<see cref="Glass"/>).
    /// </summary>
    public static class KwSailLooks
    {
        public const int Size = 256;
        private const float Opacity = 0.35f;
        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
        private static readonly HashSet<string> failed = new HashSet<string>();
        private static readonly Dictionary<Material, Material> glass = new Dictionary<Material, Material>();

        /// <summary>A mast's sail's material, from its spec: its cloth, its pattern and its proportions.</summary>
        public static Material? Of(KwSailSpec spec) => Of(spec.Cloth, spec.Pattern, KwSailFrame.Of(spec.Shape, spec.Width, spec.Height));

        /// <summary>
        /// A sail's material: its cloth's own when plain and whole (or nothing can be painted here), else its cloth with the
        /// pattern painted on and, raw hide, its wear (<see cref="KwSailWear"/>).
        /// </summary>
        public static Material? Of(string cloth, KwSailPattern pattern, KwSailFrame frame)
        {
            Material? plain = KwSailCloth.Of(cloth);
            if ((pattern.IsPlain && !KwSailWear.Worn(cloth)) || plain == null || !KwSailPixels.Drawn)
                return plain;
            string key = Key(cloth, pattern, frame);
            if (made.TryGetValue(key, out Material look) && look != null)
                return look;
            if (failed.Contains(key))
                return plain;
            Material? painted = Painted(plain, cloth, pattern, frame);
            if (painted == null)
            {
                failed.Add(key);
                return plain;
            }
            return made[key] = painted;
        }

        // What a painting depends on: the cloth, the pattern, the sail's shape (its proportions too for a border or an
        // emblem or a worn cloth's wear, and for a jib always, as its triangle's place in the texture goes by them) and an
        // emblem's picture file.
        private static string Key(string cloth, KwSailPattern pattern, KwSailFrame frame) =>
            cloth + "|" + pattern + "|" + (pattern.Shaped || frame.Jib || KwSailWear.Worn(cloth) ? frame.Key : frame.Shape.ToString()) + "|" + (pattern.HasEmblem ? KwSailEmblems.Stamp(pattern.Art) : "");

        // The cloth's material copied, wearing its texture with the pattern painted on; null when it cannot be (said once).
        private static Material? Painted(Material plain, string cloth, KwSailPattern pattern, KwSailFrame frame)
        {
            try
            {
                Texture? source = plain.mainTexture;
                if (source == null)
                    return null;
                Color32[] pixels = Pixels(source, cloth, pattern, frame);
                Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false)
                {
                    name = "broadside_sail_" + pattern, wrapMode = TextureWrapMode.Clamp, filterMode = source.filterMode, anisoLevel = source.anisoLevel,
                };
                texture.SetPixels32(pixels);
                texture.Apply(true, true);
                return new Material(plain) { name = plain.name + " (" + pattern + ")", mainTexture = texture };
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the sail pattern {pattern} could not be painted ({e.Message}); the sail is plain.");
                return null;
            }
        }

        // The cloth's texture read, its pattern painted on, and a worn cloth's wear over both (holes cut through the dye).
        private static Color32[] Pixels(Texture source, string cloth, KwSailPattern pattern, KwSailFrame frame)
        {
            Color32[] own = KwSailPixels.Read(source, Size, Size);
            KwEmblemArt? art = pattern.HasEmblem ? KwSailEmblems.Art(pattern.Art) : null;
            Color32[] painted = pattern.IsPlain ? own : KwSailPaint.Paint(own, Size, pattern, frame, art);
            return KwSailWear.Worn(cloth) ? KwSailWear.Wear(painted, own, Size, frame) : painted;
        }

        /// <summary>A sail material's see-through copy for a preview: its texture on a sprite shader at a third of its opacity.</summary>
        public static Material? Glass(Material? look)
        {
            if (look == null)
                return null;
            if (glass.TryGetValue(look, out Material cached) && cached != null)
                return cached;
            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
                return look;
            return glass[look] = new Material(shader) { mainTexture = look.mainTexture, color = new Color(1f, 1f, 1f, Opacity), renderQueue = 3000 };
        }

        /// <summary>Every painted sail let go, for a new world's (its scene and cloths are new).</summary>
        public static void Clear()
        {
            foreach (Material look in made.Values)
            {
                if (look == null)
                    continue;
                UnityEngine.Object.Destroy(look.mainTexture);
                UnityEngine.Object.Destroy(look);
            }
            foreach (Material see in glass.Values)
            {
                if (see != null)
                    UnityEngine.Object.Destroy(see);
            }
            made.Clear();
            failed.Clear();
            glass.Clear();
            KwSailEmblems.Clear();
        }
    }
}
