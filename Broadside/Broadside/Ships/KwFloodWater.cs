using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the water in a launched ship's hold is drawn in (<see cref="KwFloodSurface"/>). Its top: a copy of the game's
    /// own sea material (<see cref="Sea"/>: <c>Custom/Water</c>, its colours, ripples, foam where it meets the hull and
    /// clearness over the hold's floor), still (no waves: depth nought and no wind) and drawn just before the water mask
    /// (<see cref="Queue"/>), so the mask, which keeps the sea out of the dry hold above it, never hides it; its ripples
    /// move with the sea's own clock (<see cref="Tick"/>). Until the sea has loaded somewhere, and for its underside and
    /// its ends at the bulkheads: <see cref="Under"/>, a copy of the game's wood floor material (the building shader,
    /// <c>Custom/Piece</c>; <c>Shader.Find("Standard")</c> finds nothing here, measured 2026-10-07) with no texture, bumps,
    /// metal, glow or snow, one deep blue-green, glossy as water, drawn on its front faces only, so the underside is seen
    /// from under the water alone. Copies only: the sea and its material are never changed.
    /// </summary>
    public static class KwFloodWater
    {
        /// <summary>The render queue of the top: after everything solid, before the water mask (2899) and the sea (2900).</summary>
        public const int Queue = 2898;

        private const float Gloss = 0.9f;
        private static readonly Color Colour = new Color(0.06f, 0.17f, 0.18f, 1f);
        private static readonly int WaterTime = Shader.PropertyToID("_WaterTime");
        private static Material? under, sea;
        private static int ticked = -1;

        /// <summary>The top's water: the sea's material, still; null until a sea has loaded.</summary>
        public static Material? Sea()
        {
            if (sea != null)
                return sea;
            Material? from = Source();
            if (from == null)
                return null;
            sea = new Material(from) { name = "kw_flood_sea", renderQueue = Queue };
            sea.SetFloatArray("_depth", new float[4]);
            sea.SetFloat("_UseGlobalWind", 0f);
            return sea;
        }

        /// <summary>The sea's clock into the top's water, once a frame, so its ripples move as the sea's do.</summary>
        public static void Tick()
        {
            if (sea == null || ticked == Time.frameCount)
                return;
            ticked = Time.frameCount;
            sea.SetFloat(WaterTime, WaterVolume.s_waterTime);
        }

        /// <summary>The underside's and the ends' water (and the top's, until a sea has loaded): opaque, its front faces only.</summary>
        public static Material Under()
        {
            if (under != null)
                return under;
            Material? floor = BundlePrefabs.GameMaterials.Borrow(ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("wood_floor") : null);
            under = floor != null ? Still(BundlePrefabs.GameMaterials.Plain(floor, Gloss)) : new Material(Shader.Find("Sprites/Default")) { color = Colour };
            under.name = "kw_flood_water";
            if (under.HasProperty("_Cull"))
                under.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            return under;
        }

        // A loaded sea's material, any: they differ only in their waves' depth, wind and clock, set on the copy.
        private static Material? Source()
        {
            foreach (WaterVolume volume in WaterVolume.Instances)
            {
                if (volume != null && volume.m_waterSurface != null && volume.m_waterSurface.sharedMaterial != null)
                    return volume.m_waterSurface.sharedMaterial;
            }
            return null;
        }

        // The floor's wood grain, bumps, snow and texture mapping off: one colour over the whole surface.
        private static Material Still(Material material)
        {
            (material.mainTexture, material.color) = (Texture2D.whiteTexture, Colour);
            if (material.HasProperty("_BumpMap"))
                material.SetTexture("_BumpMap", null);
            foreach (string off in new[] { "_AddSnow", "_TriplanarMap" })
            {
                if (material.HasProperty(off))
                    material.SetFloat(off, 0f);
            }
            foreach (string keyword in new[] { "_NORMALMAP", "_ADDSNOW_ON", "_TRIPLANARMAP_ON", "_PARALLAXMAP" })
                material.DisableKeyword(keyword);
            return material;
        }
    }
}
