using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Every crop plant in the game, read from the scene's prefabs (<see cref="PrefabIndex"/>) when ZNetScene wakes (last, so plants other mods
    /// register in their own Awake postfix count too): a Plant whose grown prefab has a Pickable and no TreeBase (tree
    /// saplings) or Vine (vines grow segments of their own and are left to the game). Looked up by the plant's prefab
    /// name or hash, and by the pickable it grows. A pickable grown by several plants maps to the first found.
    /// </summary>
    public static class CropCatalog
    {
        private static readonly Dictionary<string, CropPlant> byPlant = new Dictionary<string, CropPlant>();
        private static readonly Dictionary<int, CropPlant> byHash = new Dictionary<int, CropPlant>();
        private static readonly Dictionary<string, CropPlant> byPickable = new Dictionary<string, CropPlant>();

        private sealed class Known
        {
            public CropPlant Crop;
        }

        private static readonly ConditionalWeakTable<Plant, Known> known = new ConditionalWeakTable<Plant, Known>();

        public static IEnumerable<CropPlant> All => byPlant.Values;

        public static CropPlant OfPlant(string prefab) => prefab != null && byPlant.TryGetValue(prefab, out CropPlant crop) ? crop : null;

        /// <summary>
        /// The crop plant of a plant in the world, remembered per instance: the plant hooks run on every slow-update pass,
        /// and the prefab name would be a new string each time. Plants only exist once the catalog is read (ZNetScene.Awake).
        /// </summary>
        public static CropPlant OfPlant(Plant plant) =>
            plant == null ? null : known.GetValue(plant, instance => new Known { Crop = OfPlant(Utils.GetPrefabName(instance.gameObject)) }).Crop;

        /// <summary>Whether the plant grows a vine (Vine): vines are left to the game.</summary>
        public static bool GrowsVine(Plant plant)
        {
            if (plant?.m_grownPrefabs == null)
                return false;
            foreach (GameObject grown in plant.m_grownPrefabs)
            {
                if (grown != null && grown.GetComponent<Vine>() != null)
                    return true;
            }
            return false;
        }

        public static CropPlant OfHash(int prefabHash) => byHash.TryGetValue(prefabHash, out CropPlant crop) ? crop : null;

        /// <summary>The plant that grows a pickable prefab, or null for anything that is not a Farming crop.</summary>
        public static CropPlant OfPickable(string prefab) => prefab != null && byPickable.TryGetValue(prefab, out CropPlant crop) ? crop : null;

        /// <summary>
        /// The plant a ripe crop grew from: the one written on it when it ripened (<see cref="CropKeys.From"/>), else the
        /// plant that grows its prefab (wild crops, crops that ripened before Farming).
        /// </summary>
        public static CropPlant OfPickable(Pickable pickable)
        {
            if (pickable == null)
                return null;
            CropPlant planted = OfHash(CropKeys.From(pickable.m_nview));
            return planted ?? OfPickable(Utils.GetPrefabName(pickable.gameObject));
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                foreach (Plant plant in PrefabIndex.Scene().Plants)
                    Add(plant);
                CropStarItems.Discover();
            }
        }

        private static void Add(Plant plant)
        {
            if (plant?.m_grownPrefabs == null || byPlant.ContainsKey(plant.name))
                return;
            foreach (GameObject grown in plant.m_grownPrefabs)
            {
                Pickable pickable = grown != null ? grown.GetComponent<Pickable>() : null;
                if (pickable == null || grown.GetComponent<TreeBase>() != null || grown.GetComponent<Vine>() != null)
                    continue;
                Register(Describe(plant, grown.name, pickable));
                return;
            }
        }

        private static CropPlant Describe(Plant plant, string pickablePrefab, Pickable pickable)
        {
            Piece piece = plant.GetComponent<Piece>();
            CropPlant crop = new CropPlant
            {
                Prefab = plant.name,
                PrefabHash = plant.name.GetStableHashCode(),
                Plant = plant,
                Piece = piece,
                Seed = piece != null && piece.m_resources != null && piece.m_resources.Length > 0 ? piece.m_resources[0].m_resItem : null,
                Crop = CropPlant.ItemOf(pickable.m_itemPrefab),
                Pickable = pickablePrefab,
            };
            AddExtras(crop, pickable.m_extraDrops);
            crop.Kind = CropPlant.KindOf(crop.Seed, crop.Crop);
            return crop;
        }

        private static void AddExtras(CropPlant crop, DropTable table)
        {
            if (table?.m_drops == null)
                return;
            foreach (DropTable.DropData drop in table.m_drops)
            {
                ItemDrop item = CropPlant.ItemOf(drop.m_item);
                if (item != null && item != crop.Crop && !crop.Extras.Contains(item))
                    crop.Extras.Add(item);
            }
        }

        private static void Register(CropPlant crop)
        {
            byPlant[crop.Prefab] = crop;
            byHash[crop.PrefabHash] = crop;
            if (!byPickable.ContainsKey(crop.Pickable))
                byPickable[crop.Pickable] = crop;
        }
    }
}
