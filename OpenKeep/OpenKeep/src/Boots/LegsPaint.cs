using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The split leggings keep the game's body paint (the trousers' lower legs sit partly inside the body, as the game's
    /// own leggings do, and the paint under them is what keeps them whole), but not on the feet: below the trousers' hem
    /// (0.152 m) the paint is cleared, so bare feet show when no boots are worn (the game painted boots there for several
    /// sets). The masks are the workshop's (<c>ok_body_feetmask_male</c> and <c>_female</c> in the bundle: 512 px, white
    /// where the paint goes, in the body's UV space; the two feet differ by a few texels). A legs texel is cleared when any
    /// mask texel under it is white, so no painted fringe stays at the hem. The cleared copy of a leggings' <c>_LegsTex</c>
    /// is made through the GPU the first time this client draws that leggings on that body, and kept. Without the mask, or
    /// on a machine that cannot read textures, the paint stays whole.
    /// </summary>
    public static class LegsPaint
    {
        private const string MaskName = "ok_body_feetmask_";
        private static readonly int LegsTex = Shader.PropertyToID("_LegsTex");
        private static readonly Dictionary<(BootSet, int), Texture2D> cleared = new Dictionary<(BootSet, int), Texture2D>();
        private static readonly Mask[] masks = new Mask[2];

        /// <summary>After the game painted the body for split leggings: the same paint with the feet cleared.</summary>
        public static void Apply(VisEquipment vis, BootSet set)
        {
            int sex = vis.m_currentModelIndex == 1 ? 1 : 0;
            if (!cleared.TryGetValue((set, sex), out Texture2D texture))
                cleared[(set, sex)] = texture = Guarded(set, sex);
            if (texture != null)
                vis.m_bodyModel.material.SetTexture(LegsTex, texture);
        }

        private static Texture2D Guarded(BootSet set, int sex)
        {
            try
            {
                return Make(set, MaskOf(sex));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: the {set.Key} leggings keep their paint on the feet: {e.Message}");
                return null;
            }
        }

        private static Texture2D Make(BootSet set, Mask mask)
        {
            Material paint = set.LegsPrefab != null ? set.LegsPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_armorMaterial : null;
            Texture source = paint != null && paint.HasProperty(LegsTex) ? paint.GetTexture(LegsTex) : null;
            if (source == null || mask == null)
                return null;
            Color32[] pixels = TexturePixels.Read(source, out int width, out int height);
            if (pixels == null)
                return null;
            mask.Clear(pixels, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = source.name + "_ok_nofeet",
                wrapMode = source.wrapMode,
                filterMode = source.filterMode,
                anisoLevel = source.anisoLevel,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Mask MaskOf(int sex)
        {
            if (masks[sex] == null)
                masks[sex] = Mask.Load(MaskName + (sex == 1 ? "female" : "male"));
            return masks[sex].Readable ? masks[sex] : null;
        }

        /// <summary>One body's feet mask, read once.</summary>
        private sealed class Mask
        {
            private Color32[] texels;
            private int width;
            private int height;

            public bool Readable => texels != null;

            public static Mask Load(string name)
            {
                var mask = new Mask();
                Texture2D texture = BootsBundle.Texture(name);
                if (texture == null || !texture.isReadable)
                {
                    Plugin.Log.LogWarning($"OpenKeep: no readable {name} in the boots bundle: the leggings keep their paint on the feet");
                    return mask;
                }
                mask.texels = texture.GetPixels32();
                mask.width = texture.width;
                mask.height = texture.height;
                return mask;
            }

            /// <summary>Clears (alpha 0) every legs texel with any white mask texel under it.</summary>
            public void Clear(Color32[] pixels, int legsWidth, int legsHeight)
            {
                for (int y = 0; y < legsHeight; y++)
                {
                    for (int x = 0; x < legsWidth; x++)
                    {
                        if (AnyWhite(x * width / legsWidth, Math.Max((x + 1) * width / legsWidth, x * width / legsWidth + 1),
                                y * height / legsHeight, Math.Max((y + 1) * height / legsHeight, y * height / legsHeight + 1)))
                            pixels[y * legsWidth + x].a = 0;
                    }
                }
            }

            private bool AnyWhite(int x0, int x1, int y0, int y1)
            {
                for (int y = y0; y < y1 && y < height; y++)
                {
                    for (int x = x0; x < x1 && x < width; x++)
                    {
                        if (texels[y * width + x].r > 127)
                            return true;
                    }
                }
                return false;
            }
        }
    }
}
