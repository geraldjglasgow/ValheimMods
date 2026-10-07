using System.Collections.Generic;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// One sale on the crate's owner: every stack the crate buys (<see cref="ShippingPrices"/>) is taken out and its coins
    /// put in the same inventory. A stack whose coins would not fit stays. Each change goes through the inventory's own
    /// add and remove, so the container's changed callback saves it to the ZDO the game's way and it replicates.
    /// </summary>
    public static class ShippingSale
    {
        private const string Coins = "Coins";

        private static readonly List<ItemDrop.ItemData> stacks = new List<ItemDrop.ItemData>();

        /// <summary>Sells what sells; returns (items, coins) sold, (0, 0) when nothing was.</summary>
        public static (int Items, int Coins) Sell(Inventory inventory)
        {
            ItemDrop coin = Kitchen.ItemPrefab(Coins);
            if (inventory == null || coin == null)
                return (0, 0);
            int items = 0, paid = 0;
            stacks.Clear();
            stacks.AddRange(inventory.GetAllItems());
            foreach (ItemDrop.ItemData stack in stacks)
            {
                int price = ShippingPrices.StackPrice(stack);
                if (price <= 0 || price > Room(inventory, coin) + coin.m_itemData.m_shared.m_maxStackSize)
                    continue;
                int count = stack.m_stack;
                if (!inventory.RemoveItem(stack))
                    continue;
                AddCoins(inventory, coin, price);
                items += count;
                paid += price;
            }
            stacks.Clear();
            return (items, paid);
        }

        /// <summary>Coins that fit now: room left on coin stacks plus every empty slot.</summary>
        private static int Room(Inventory inventory, ItemDrop coin)
        {
            ItemDrop.ItemData.SharedData shared = coin.m_itemData.m_shared;
            return inventory.FindFreeStackSpace(shared.m_name, Game.m_worldLevel) + inventory.GetEmptySlots() * shared.m_maxStackSize;
        }

        private static void AddCoins(Inventory inventory, ItemDrop coin, int amount)
        {
            int max = Mathf.Max(1, coin.m_itemData.m_shared.m_maxStackSize);
            while (amount > 0)
            {
                ItemDrop.ItemData item = coin.m_itemData.Clone();
                item.m_dropPrefab = coin.gameObject;
                item.m_stack = Mathf.Min(amount, max);
                item.m_worldLevel = Game.m_worldLevel;
                amount -= item.m_stack;
                if (!inventory.AddItem(item))
                {
                    Hearthhold.Log.LogWarning($"The Shipping Crate had no room for {amount + item.m_stack} coins.");
                    return;
                }
            }
        }
    }
}
