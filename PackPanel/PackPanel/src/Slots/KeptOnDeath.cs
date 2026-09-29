using System.Collections.Generic;
using PackPanel.Core;

namespace PackPanel.Slots
{
    /// <summary>
    /// Keep Slots On Death (the user's request): the items in the gear, backpack, utility, food, mead, ammo and tacklebox
    /// slots stay with the player when they die. Just before the game makes the tombstone they are taken out of the inventory (so
    /// neither its grave nor a world modifier that deletes items at death sees them), with whether each was worn; right
    /// after, they go back into their slots, worn ones marked worn. The game saves the character when the respawn comes
    /// (<c>Game._RequestRespawn</c>, after the tombstone), and its load wears what is marked worn, so the armour and the
    /// utilities are back on at the respawn. The coin purse, the key ring, the tacklebox's cells and the grid follow the
    /// game's rules: keys and bait go to the grave, as they would from anywhere else in the inventory. Local player only.
    /// </summary>
    public sealed class KeptOnDeath
    {
        private readonly List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
        private readonly HashSet<ItemDrop.ItemData> worn = new HashSet<ItemDrop.ItemData>();

        /// <summary>Takes the kept slots' items out of the inventory; null when nothing is kept.</summary>
        public static KeptOnDeath Take(Player player)
        {
            if (!InventorySettings.KeepSlotsOnDeath.Value || !InventoryState.Active)
                return null;
            KeptOnDeath kept = new KeptOnDeath();
            Inventory inventory = player.GetInventory();
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                Slot slot = InventoryState.Layout.SlotAt(item.m_gridPos);
                if (slot == null || !Kept(slot.Kind))
                    continue;
                kept.items.Add(item);
                if (item.m_equipped)
                    kept.worn.Add(item);
                inventory.m_inventory.Remove(item);
            }
            return kept.items.Count > 0 ? kept : null;
        }

        /// <summary>
        /// The slots whose items stay: not the purse, the key ring or the tacklebox's cells (the box itself stays, as a
        /// backpack does, and its bait goes to the grave like a backpack's cells), nor a retired slot.
        /// </summary>
        private static bool Kept(SlotKind kind) =>
            kind != SlotKind.Purse && kind != SlotKind.Key && kind != SlotKind.Tackle && kind != SlotKind.Retired;

        /// <summary>Puts every kept item back in its cell, worn ones marked worn for the respawn's load.</summary>
        public void Return(Player player)
        {
            Inventory inventory = player.GetInventory();
            foreach (ItemDrop.ItemData item in items)
            {
                item.m_equipped = worn.Contains(item);
                if (!inventory.ContainsItem(item))
                    inventory.m_inventory.Add(item);
            }
            inventory.Changed();
            Plugin.Log.LogInfo($"kept {items.Count} slot items through death ({worn.Count} worn)");
        }
    }
}
