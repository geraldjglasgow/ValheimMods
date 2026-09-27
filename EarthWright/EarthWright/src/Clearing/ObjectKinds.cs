using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Sorts a world object into a <see cref="ClearCategory"/> by the game components it carries, so modded trees and
    /// rocks count as well: TreeBase is a tree, TreeLog a log, MineRock and MineRock5 a rock (an ore deposit when a drop
    /// is ore), a Pickable a pickable. Destructible covers several things; stumps, bushes and small rocks are told apart
    /// by what they turn into and by their prefab names. Anything a player built, creature nests, containers and the
    /// Leviathan are never cleared.
    /// </summary>
    public static class ObjectKinds
    {
        /// <summary>Items of pickables that count as loose debris (Pickable_Stone, Pickable_Branch, Pickable_Flint, their snow and ash kin).</summary>
        private static readonly HashSet<string> DebrisItems = new HashSet<string> { "Stone", "Wood", "Flint", "Frostwood", "Grausten", "StoneRock" };

        public static ClearCategory Classify(GameObject go)
        {
            if (go == null || IsProtected(go))
                return ClearCategory.None;
            Pickable pickable = go.GetComponent<Pickable>();
            if (pickable != null)
                return OfPickable(pickable);
            if (go.GetComponent<TreeBase>() != null)
                return ClearCategory.Trees;
            if (go.GetComponent<TreeLog>() != null)
                return ClearCategory.Logs;
            ClearCategory rock = OfRock(go);
            if (rock != ClearCategory.None)
                return rock;
            Destructible destructible = go.GetComponent<Destructible>();
            return destructible != null ? OfDestructible(destructible) : ClearCategory.None;
        }

        /// <summary>The lowest tool tier that can damage the object (0 when any will do).</summary>
        public static int MinToolTier(GameObject go)
        {
            TreeBase tree = go.GetComponent<TreeBase>();
            if (tree != null)
                return tree.m_minToolTier;
            TreeLog log = go.GetComponent<TreeLog>();
            if (log != null)
                return log.m_minToolTier;
            int rock = RockTier(go);
            if (rock >= 0)
                return rock;
            Destructible destructible = go.GetComponent<Destructible>();
            if (destructible == null)
                return 0;
            int spawned = destructible.m_spawnWhenDestroyed != null ? RockTier(destructible.m_spawnWhenDestroyed) : -1;
            return Mathf.Max(destructible.m_minToolTier, spawned);
        }

        private static bool IsProtected(GameObject go)
        {
            Piece piece = go.GetComponent<Piece>();
            if (piece != null && piece.IsPlacedByPlayer())
                return true;
            return go.GetComponent<Leviathan>() != null || go.GetComponent<Container>() != null || go.GetComponent<SpawnArea>() != null;
        }

        /// <summary>
        /// Crops on cultivated ground are someone's farm and the "Keep Pickables" (boss offerings, quest items,
        /// treasure) stay; ore lying about counts as an ore deposit, stones and branches as debris.
        /// </summary>
        private static ClearCategory OfPickable(Pickable pickable)
        {
            string name = Utils.GetPrefabName(pickable.gameObject);
            if (OnFarmland(pickable.transform.position) || ObjectFilter.Matches(ClearingSettings.KeptPickables(), name))
                return ClearCategory.None;
            string item = pickable.m_itemPrefab != null ? pickable.m_itemPrefab.name : "";
            if (IsOreItem(item, ClearingSettings.OreKeywords()))
                return ClearCategory.Ores;
            return DebrisItems.Contains(item) ? ClearCategory.Debris : ClearCategory.Pickables;
        }

        private static bool OnFarmland(Vector3 position)
        {
            Heightmap map = Heightmap.FindHeightmap(position);
            return map != null && map.m_paintMask != null && map.IsCultivated(position);
        }

        private static ClearCategory OfRock(GameObject go)
        {
            MineRock5 big = go.GetComponent<MineRock5>();
            if (big != null)
                return RockKind(big.m_dropItems);
            MineRock small = go.GetComponent<MineRock>();
            return small != null ? RockKind(small.m_dropItems) : ClearCategory.None;
        }

        private static int RockTier(GameObject go)
        {
            MineRock5 big = go.GetComponent<MineRock5>();
            if (big != null)
                return big.m_minToolTier;
            MineRock small = go.GetComponent<MineRock>();
            return small != null ? small.m_minToolTier : -1;
        }

        /// <summary>
        /// Stumps, bushes, small trees, old logs and boulders are all Destructible. A boulder that breaks into a
        /// mineable rock is a rock (or ore); otherwise the prefab name tells (stub/stump, bush/shrub, rock, log,
        /// branch), and anything else that is cut like wood is a small tree. Everything else (props, nests, remains) stays.
        /// </summary>
        private static ClearCategory OfDestructible(Destructible destructible)
        {
            ClearCategory spawned = destructible.m_spawnWhenDestroyed != null ? OfRock(destructible.m_spawnWhenDestroyed) : ClearCategory.None;
            if (spawned != ClearCategory.None)
                return spawned;
            string name = Utils.GetPrefabName(destructible.gameObject).ToLowerInvariant();
            if (name.Contains("stub") || name.Contains("stump"))
                return ClearCategory.Stumps;
            if (name.Contains("bush") || name.Contains("shrub"))
                return ClearCategory.Shrubs;
            if (name.Contains("rock"))
                return RockKind(DropsOf(destructible.gameObject));
            if (name.Contains("log"))
                return ClearCategory.Logs;
            if (name.Contains("branch"))
                return ClearCategory.Debris;
            return destructible.m_destructibleType == DestructibleType.Tree ? ClearCategory.Trees : ClearCategory.None;
        }

        private static DropTable DropsOf(GameObject go)
        {
            DropOnDestroyed drops = go.GetComponent<DropOnDestroyed>();
            return drops != null ? drops.m_dropWhenDestroyed : null;
        }

        private static ClearCategory RockKind(DropTable drops) => IsOre(drops) ? ClearCategory.Ores : ClearCategory.Rocks;

        /// <summary>A drop table with an item whose name holds one of the "Ore Drops" words.</summary>
        private static bool IsOre(DropTable drops)
        {
            if (drops == null || drops.m_drops == null)
                return false;
            string[] words = ClearingSettings.OreKeywords();
            foreach (DropTable.DropData drop in drops.m_drops)
            {
                if (drop.m_item != null && IsOreItem(drop.m_item.name, words))
                    return true;
            }
            return false;
        }

        /// <summary>Case matters, so "Ore" finds CopperOre but not SurtlingCore.</summary>
        private static bool IsOreItem(string item, string[] words)
        {
            foreach (string word in words)
            {
                if (item.Contains(word))
                    return true;
            }
            return false;
        }
    }
}
