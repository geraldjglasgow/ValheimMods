using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// One crop plant as the game defines it, read once from its prefab: a Plant whose grown prefab has a Pickable and is
    /// neither a tree nor a vine (<see cref="CropCatalog"/>). The seed is what the plant's piece costs; the crops are the
    /// pickable's item and its extra drops. The prefab's own components keep the game's values (grow radius, tolerance,
    /// biomes) for <see cref="PlantTraits"/> to start from.
    /// </summary>
    public sealed class CropPlant
    {
        public string Prefab;
        public int PrefabHash;
        public Plant Plant;
        public Piece Piece;
        public ItemDrop Seed;
        public ItemDrop Crop;
        public readonly List<ItemDrop> Extras = new List<ItemDrop>();
        public string Pickable;

        /// <summary>Every item the plant uses or gives: the seed, the crop and the extra drops.</summary>
        public IEnumerable<ItemDrop> Items()
        {
            if (Seed != null)
                yield return Seed;
            if (Crop != null)
                yield return Crop;
            foreach (ItemDrop extra in Extras)
                yield return extra;
        }

        /// <summary>The crop's name as players read it ("Turnip"), for callouts.</summary>
        public string CropName()
        {
            string token = Crop != null ? Crop.m_itemData.m_shared.m_name : Plant != null ? Plant.m_name : Prefab;
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }

        /// <summary>Whether this plant grows where the given biome is.</summary>
        public bool GrowsIn(Heightmap.Biome biome) => Plant != null && (Plant.m_biome & biome) != 0;

        internal static ItemDrop ItemOf(GameObject prefab) => prefab != null ? prefab.GetComponent<ItemDrop>() : null;
    }
}
