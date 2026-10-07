using System;
using System.Collections.Generic;

namespace Hearthhold
{
    /// <summary>
    /// What the Shipping Crate pays: per unit max(1, round(food value / 5)) coins, 2 for an item with no food value, times
    /// 1 / 1.25 / 1.5 / 2 for 0 / 1 / 2 / 3 stars, the stack total rounded. Only star items sell; coins never do.
    /// </summary>
    public static class ShippingPrices
    {
        private const float NoValueBase = 2f;
        private static readonly float[] starFactors = { 1f, 1.25f, 1.5f, 2f };

        /// <summary>Whether the crate buys this item.</summary>
        public static bool Sells(ItemDrop.ItemData item) => item?.m_shared != null && item.m_stack > 0 && Stars.IsStarItem(item);

        /// <summary>The coins a whole stack fetches; 0 for an item the crate does not buy.</summary>
        public static int StackPrice(ItemDrop.ItemData item)
        {
            if (!Sells(item))
                return 0;
            float value = Kitchen.FoodValue(item);
            float unit = value > 0f ? Math.Max(1f, Round(value / 5f)) : NoValueBase;
            int stars = Math.Max(0, Math.Min(Stars.Max, Stars.Get(item)));
            return (int)Round(unit * item.m_stack * starFactors[stars]);
        }

        /// <summary>What everything in the inventory that sells would fetch now: (items, coins).</summary>
        public static (int Items, int Coins) Total(Inventory inventory)
        {
            int items = 0, coins = 0;
            if (inventory == null)
                return (0, 0);
            List<ItemDrop.ItemData> all = inventory.GetAllItems();
            for (int i = 0; i < all.Count; i++)
            {
                if (!Sells(all[i]))
                    continue;
                items += all[i].m_stack;
                coins += StackPrice(all[i]);
            }
            return (items, coins);
        }

        private static float Round(float value) => (float)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
