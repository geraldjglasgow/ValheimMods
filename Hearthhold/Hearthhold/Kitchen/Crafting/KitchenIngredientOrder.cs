using System.Collections.Generic;
using System.Linq;

namespace Hearthhold
{
    /// <summary>
    /// Better ingredients make better food, so a kitchen craft uses the crafter's best first. The game pays with
    /// Inventory.RemoveItem(name, amount, quality), which takes from the inventory's m_inventory list in list order, and
    /// list order means nothing else to the inventory grid (each item keeps its own grid position). Before the craft the
    /// list is stable-sorted so items with more stars come first; same-named stacks without stars keep their order
    /// behind them. Afterwards the list goes back to its old order, items the craft added at the end, so nothing else
    /// (which stack a pickup fills first, another mod) sees a changed inventory. Local to the crafter.
    /// </summary>
    public static class KitchenIngredientOrder
    {
        private static readonly List<ItemDrop.ItemData> original = new List<ItemDrop.ItemData>();
        private static Inventory sorted;

        public static void BestFirst(Inventory inventory)
        {
            Restore();
            List<ItemDrop.ItemData> items = inventory?.m_inventory;
            if (items == null || !items.Any(item => Stars.Get(item) > 0))
                return;
            original.AddRange(items);
            List<ItemDrop.ItemData> ordered = items.OrderByDescending(Stars.Get).ToList();
            items.Clear();
            items.AddRange(ordered);
            sorted = inventory;
        }

        /// <summary>The order from before <see cref="BestFirst"/>: the items still there, then the ones added since.</summary>
        public static void Restore()
        {
            Inventory inventory = sorted;
            sorted = null;
            if (inventory != null)
                Reorder(inventory.m_inventory);
            original.Clear();
        }

        private static void Reorder(List<ItemDrop.ItemData> items)
        {
            HashSet<ItemDrop.ItemData> now = new HashSet<ItemDrop.ItemData>(items);
            HashSet<ItemDrop.ItemData> before = new HashSet<ItemDrop.ItemData>(original);
            List<ItemDrop.ItemData> result = original.Where(now.Contains).Concat(items.Where(item => !before.Contains(item))).ToList();
            items.Clear();
            items.AddRange(result);
        }
    }
}
