using System.Collections.Generic;
using HarmonyLib;

namespace OpenKeep.Reach
{
    /// <summary>
    /// What the reachable containers hold, counted in one walk and then answered from two tables: per shared name, and
    /// per shared name and quality (the game counts each quality on its own for recipes). A stack goes in when the
    /// container's allow / deny lists accept it and the world level rule holds, the filter every requirement count
    /// uses. The walk is repeated before the next answer whenever the tables could be wrong: the reach list is another
    /// instance (a container came or went, moved in or out of range, was opened by another player, or the quarter
    /// second refresh and the rules' apply, see <see cref="ReachChests"/>), any inventory changed (the game's
    /// <c>Inventory.Changed</c>, which every add, remove, move, sort and load from the ZDO ends in, so a craft paid from
    /// a chest, a drag between the inventory and a chest and another player's change arriving by ZDO all count at once)
    /// or the world level changed. A panel asking for every requirement every frame walks the containers once per
    /// change instead of once per row.
    /// </summary>
    public static class StorageIndex
    {
        private static readonly Dictionary<string, int> byName = new Dictionary<string, int>();
        private static readonly Dictionary<(string, int), int> byQuality = new Dictionary<(string, int), int>();
        private static List<Container> countedList;
        private static int countedChange = -1;
        private static int countedWorldLevel = int.MinValue;
        private static int change;

        /// <summary>Any inventory changed: the next answer walks the containers again.</summary>
        public static void MarkChanged() => change++;

        /// <summary>Items of a shared name (any quality below 0) in the reachable containers that pass the world level rule.</summary>
        public static int Count(string name, int quality)
        {
            Refresh();
            int count;
            if (quality < 0)
                return byName.TryGetValue(name, out count) ? count : 0;
            return byQuality.TryGetValue((name, quality), out count) ? count : 0;
        }

        /// <summary>Walks the containers again when the list, an inventory or the world level changed since the last walk.</summary>
        private static void Refresh()
        {
            List<Container> containers = ReachChests.List();
            int worldLevel = Game.m_worldLevel;
            if (ReferenceEquals(containers, countedList) && change == countedChange && worldLevel == countedWorldLevel)
                return;
            int seen = change;
            byName.Clear();
            byQuality.Clear();
            foreach (Container container in containers)
            {
                if (container != null)
                    Add(container);
            }
            countedList = containers;
            countedChange = seen;
            countedWorldLevel = worldLevel;
        }

        private static void Add(Container container)
        {
            Inventory inventory = container.GetInventory();
            if (inventory == null)
                return;
            ContainerRule rule = ReachRules.RuleFor(container);
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null || item.m_shared.m_name == null)
                    continue;
                if (item.m_worldLevel < Game.m_worldLevel || !rule.Accepts(item))
                    continue;
                Bump(byName, item.m_shared.m_name, item.m_stack);
                Bump(byQuality, (item.m_shared.m_name, item.m_quality), item.m_stack);
            }
        }

        private static void Bump<TKey>(Dictionary<TKey, int> table, TKey key, int amount)
        {
            table.TryGetValue(key, out int sum);
            table[key] = sum + amount;
        }
    }

    /// <summary>Every inventory change marks the storage index; a prefix, so it runs even when a change callback throws.</summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    public static class InventoryChangedPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => StorageIndex.MarkChanged();
    }
}
