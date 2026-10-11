using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The Raiders Chest holds coins and nothing else (features/raids.md section 2), and nothing at all while its raid is
    /// on (section 4.1: the chest stays locked until the raid ends). Its inventory is known by its name (the chest's
    /// container name, one string comparison, so every other inventory in the game pays only that) and found back to its
    /// chest through a weak table filled as each chest wakes. The coin is the game's own Coins item, by its name token as
    /// the trader reads it. Counting and taking the stake happen on the chest's owner (<see cref="ChestSounding"/>).
    /// </summary>
    internal static class ChestCoins
    {
        private const string CoinPrefab = "Coins";
        private const string GameToken = "$item_coins";

        private static readonly ConditionalWeakTable<Inventory, RaidChest> Chests = new ConditionalWeakTable<Inventory, RaidChest>();
        private static string? _token;

        /// <summary>The coin's name token, read from the item list once it is up.</summary>
        public static string Token => _token ?? Resolve();

        public static bool IsCoin(ItemDrop.ItemData? item) => item?.m_shared != null && item.m_shared.m_name == Token;

        /// <summary>True for a Raiders Chest's inventory.</summary>
        public static bool IsChest(Inventory? inventory) =>
            inventory != null && string.Equals(inventory.GetName(), ChestPrefab.ContainerName);

        /// <summary>Whether a Raiders Chest's inventory takes this item: a coin, while no raid is on.</summary>
        public static bool Takes(Inventory chest, ItemDrop.ItemData? item) => IsCoin(item) && !Locked(chest);

        /// <summary>Every coin in the inventory, whatever world level it was found at.</summary>
        public static int Count(Inventory inventory) => inventory.CountItems(Token, -1, false);

        /// <summary>Takes the stake out. The chest's owner only; the container saves itself on the change.</summary>
        public static void Take(Inventory inventory, int coins) => inventory.RemoveItem(Token, coins, -1, false);

        /// <summary>A chest's inventory, as the chest wakes in the world.</summary>
        public static void Remember(Inventory inventory, RaidChest chest)
        {
            Chests.Remove(inventory);
            Chests.Add(inventory, chest);
        }

        /// <summary>
        /// "Move everything" into a Raiders Chest moves its coins only, each through the game's own add (which stacks them
        /// and saves). Whatever is not a coin stays where it was.
        /// </summary>
        public static void MoveCoins(Inventory chest, Inventory from)
        {
            if (Locked(chest))
            {
                return;
            }
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(from.GetAllItems()))
            {
                if (IsCoin(item))
                {
                    chest.MoveItemToThis(from, item);
                }
            }
        }

        private static bool Locked(Inventory chest) =>
            Chests.TryGetValue(chest, out RaidChest owner) && owner != null && owner.Live && Raid.IsRunning(owner.View);

        private static string Resolve()
        {
            ObjectDB db = ObjectDB.instance;
            GameObject? prefab = db != null ? db.GetItemPrefab(CoinPrefab) : null;
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null || drop.m_itemData?.m_shared == null)
            {
                return GameToken; // the item list is not up yet: the game's token, not kept
            }
            _token = drop.m_itemData.m_shared.m_name;
            return _token;
        }
    }
}
