using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A look's material: a copy of its chest's worn material (the game's shader, metal and gloss) wearing the look's
    /// atlas (<see cref="FittedAtlas"/>), made from the chest's worn textures and its body paint. Made once per client,
    /// the first time the look is worn; null when the chest or its albedo is not found (logged once), and then the
    /// leggings keep the game's look. A glow map the chest has (flametal's) is cleared: it is laid out for the chest's
    /// mesh, not the atlas.
    /// </summary>
    internal static class FittedMaterial
    {
        private static readonly string[][] Properties =
        {
            new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap" },
            new[] { "_ChestTex", "_ChestBumpMap", "_ChestMetal" },
        };

        private static readonly Dictionary<LegStyle, Material> made = new Dictionary<LegStyle, Material>();

        public static Material Get(LegStyle style)
        {
            if (made.TryGetValue(style, out Material material))
                return material;
            GameObject chest = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(style.Chest) : null;
            Material worn = Worn(chest), paint = chest != null ? chest.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_armorMaterial : null;
            made[style] = material = worn != null && worn.HasProperty("_MainTex") && worn.GetTexture("_MainTex") != null ? Make(style, worn, paint) : null;
            if (material == null)
                Plugin.Log.LogWarning($"EliteEquipment: the game's {style.Chest} material was not found; the {style.Key} leggings keep the game's look");
            return material;
        }

        private static Material Make(LegStyle style, Material worn, Material paint)
        {
            var material = new Material(worn) { name = "EE_Fitted" + style.Key };
            for (int channel = 0; channel < 3; channel++)
            {
                if (channel > 0 && !material.HasProperty(Properties[0][channel]))
                    continue;
                Texture[] sources = { Texture(worn, Properties[0][channel]), Texture(paint, Properties[1][channel]) };
                material.SetTexture(Properties[0][channel], FittedAtlas.Build(style, sources, channel));
            }
            if (material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", Texture2D.blackTexture);
            return material;
        }

        private static Texture Texture(Material material, string property) =>
            material != null && material.HasProperty(property) ? material.GetTexture(property) : null;

        /// <summary>The material of the chest's worn skin.</summary>
        private static Material Worn(GameObject chest)
        {
            Transform skin = chest != null ? chest.transform.Find("attach_skin") : null;
            SkinnedMeshRenderer renderer = skin != null ? skin.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            return renderer != null ? renderer.sharedMaterial : null;
        }
    }
}
