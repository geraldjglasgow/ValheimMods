using System;
using System.Collections.Generic;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Packs a Mímir's Chest into rows from the top-left, with no gaps (user, 2026-10-07: items "so spread out", and
    /// "filtering should show those items at the top in rows"). The stacks keep their reading order; those that pass a
    /// test (the search and quick filter) come first. Only on the chest's owner, which the player with it open is (the
    /// game hands an opened container to the opener), and through the inventory's own change, so it saves to the ZDO and
    /// reaches everyone the game's way. Done when the chest opens and whenever the search or filter changes.
    /// </summary>
    public static class MimirPack
    {
        private static readonly List<ItemDrop.ItemData> order = new List<ItemDrop.ItemData>();

        public static void Pack(Container container, Func<ItemDrop.ItemData, bool> first) => Pack(container, first, null);

        /// <summary>Packs with the stacks in <paramref name="order"/> instead of their reading order (the sort modes).</summary>
        public static void Pack(Container container, Func<ItemDrop.ItemData, bool> first, Comparison<ItemDrop.ItemData> sortBy)
        {
            Inventory inventory = container != null ? container.GetInventory() : null;
            if (inventory == null || container.m_nview == null || !container.m_nview.IsOwner())
                return;
            order.Clear();
            order.AddRange(inventory.GetAllItems());
            order.Sort(sortBy ?? Reading);
            if (first != null)
                Stable(first);
            if (Place(inventory))
                inventory.Changed();
            order.Clear();
        }

        /// <summary>Puts the stacks that pass first, each group in its own reading order.</summary>
        private static void Stable(Func<ItemDrop.ItemData, bool> first)
        {
            List<ItemDrop.ItemData> passed = order.FindAll(item => first(item));
            List<ItemDrop.ItemData> rest = order.FindAll(item => !first(item));
            order.Clear();
            order.AddRange(passed);
            order.AddRange(rest);
        }

        /// <summary>Gives each stack the next cell, row by row; true when any stack moved.</summary>
        private static bool Place(Inventory inventory)
        {
            int width = Math.Max(1, inventory.GetWidth());
            bool moved = false;
            for (int i = 0; i < order.Count; i++)
            {
                Vector2i cell = new Vector2i(i % width, i / width);
                if (order[i].m_gridPos.x == cell.x && order[i].m_gridPos.y == cell.y)
                    continue;
                order[i].m_gridPos = cell;
                moved = true;
            }
            return moved;
        }

        private static int Reading(ItemDrop.ItemData a, ItemDrop.ItemData b) =>
            a.m_gridPos.y != b.m_gridPos.y ? a.m_gridPos.y.CompareTo(b.m_gridPos.y) : a.m_gridPos.x.CompareTo(b.m_gridPos.x);
    }
}
