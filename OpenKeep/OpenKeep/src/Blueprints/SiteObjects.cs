using System.Text.RegularExpressions;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Which world objects a blueprint clears from its site, by the game components they carry (so modded trees and
    /// rocks count too): trees, logs, stumps, shrubs, rocks and natural pickables. Never ore (a rock, vein or pickable
    /// that drops ore, scrap, obsidian, tar and the like), crops on cultivated ground, boss offerings, quest items and
    /// treasure, player-built pieces, containers, creature nests or the Leviathan.
    /// </summary>
    public static class SiteObjects
    {
        private static readonly string[] OreWords = { "Ore", "Scrap", "Obsidian", "Softtissue", "Tar", "Sulfur" };

        private static readonly Regex Kept = new Regex(
            "^(Pickable_DragonEgg|goblin_totempole|Pickable_.*CoreStand|Pickable_Swordpiece.*|Pickable_VoltureEgg|Pickable_Charredskull|" +
            "Pickable_FrostCoreHanger|Morkhalla_Eye.*|Pickable_Dvergr.*|Lured.*|Pickable_Fishingrod|Pickable_RoyalJelly|" +
            "Pickable_ForestCryptRemains.*|Pickable_MountainRemains.*)$");

        public static bool Clearable(GameObject go)
        {
            if (go == null || Protected(go))
                return false;
            Pickable pickable = go.GetComponent<Pickable>();
            if (pickable != null)
                return ClearablePickable(pickable);
            if (go.GetComponent<TreeBase>() != null || go.GetComponent<TreeLog>() != null)
                return true;
            if (go.GetComponent<MineRock5>() != null)
                return !IsOre(go.GetComponent<MineRock5>().m_dropItems);
            if (go.GetComponent<MineRock>() != null)
                return !IsOre(go.GetComponent<MineRock>().m_dropItems);
            Destructible destructible = go.GetComponent<Destructible>();
            return destructible != null && ClearableDestructible(destructible);
        }

        private static bool Protected(GameObject go)
        {
            Piece piece = go.GetComponent<Piece>();
            if (piece != null && piece.IsPlacedByPlayer())
                return true;
            return go.GetComponent<Leviathan>() != null || go.GetComponent<Container>() != null || go.GetComponent<SpawnArea>() != null
                || go.GetComponent<Character>() != null || go.GetComponent<ItemDrop>() != null;
        }

        private static bool ClearablePickable(Pickable pickable)
        {
            if (Kept.IsMatch(Utils.GetPrefabName(pickable.gameObject)) || OnFarmland(pickable.transform.position))
                return false;
            return pickable.m_itemPrefab == null || !IsOreItem(pickable.m_itemPrefab.name);
        }

        private static bool OnFarmland(Vector3 position)
        {
            Heightmap map = Heightmap.FindHeightmap(position);
            return map != null && map.m_paintMask != null && map.IsCultivated(position);
        }

        /// <summary>
        /// Stumps, bushes, small trees, old logs and boulders are all Destructible: a boulder that breaks into a mineable
        /// rock goes by that rock, otherwise the prefab name tells; anything else cut like wood is a small tree.
        /// </summary>
        private static bool ClearableDestructible(Destructible destructible)
        {
            GameObject spawned = destructible.m_spawnWhenDestroyed;
            if (spawned != null && (spawned.GetComponent<MineRock5>() != null || spawned.GetComponent<MineRock>() != null))
                return Clearable(spawned);
            string name = Utils.GetPrefabName(destructible.gameObject).ToLowerInvariant();
            if (name.Contains("rock"))
                return !IsOre(destructible.GetComponent<DropOnDestroyed>()?.m_dropWhenDestroyed);
            if (name.Contains("stub") || name.Contains("stump") || name.Contains("bush") || name.Contains("shrub") || name.Contains("log") || name.Contains("branch"))
                return true;
            return destructible.m_destructibleType == DestructibleType.Tree;
        }

        private static bool IsOre(DropTable drops)
        {
            if (drops?.m_drops == null)
                return false;
            foreach (DropTable.DropData drop in drops.m_drops)
            {
                if (drop.m_item != null && IsOreItem(drop.m_item.name))
                    return true;
            }
            return false;
        }

        /// <summary>Case matters, so "Ore" finds CopperOre but not SurtlingCore.</summary>
        private static bool IsOreItem(string item)
        {
            foreach (string word in OreWords)
            {
                if (item.Contains(word))
                    return true;
            }
            return false;
        }
    }
}
