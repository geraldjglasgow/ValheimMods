using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// Units of a player's stack held out of the inventory while a put to a chest's owner is under way, so they cannot
    /// be dropped, eaten, equipped or moved meanwhile and end up both in the chest and with the player. A whole stack
    /// leaves the inventory as it is (the same item object); a part is taken off the stack. When the answer comes,
    /// <see cref="Return"/> gives back what the chest did not take: the whole stack into its own slot when that is still
    /// free, a part onto its stack when that is still there, else wherever it fits, and what fits nowhere is dropped at
    /// the player's feet. It settles once; a second call does nothing.
    /// </summary>
    internal sealed class Escrow
    {
        private readonly ItemDrop.ItemData item;
        private readonly ItemDrop.ItemData part;
        private readonly int amount;
        private bool settled;

        private Escrow(ItemDrop.ItemData item, ItemDrop.ItemData part, int amount)
        {
            this.item = item;
            this.part = part;
            this.amount = amount;
        }

        /// <summary>Holds <paramref name="amount"/> units of the stack back; null when the stack is no longer in the inventory.</summary>
        public static Escrow Hold(Player player, ItemDrop.ItemData item, int amount)
        {
            Inventory inventory = player.GetInventory();
            if (item == null || !inventory.ContainsItem(item))
                return null;
            amount = Mathf.Clamp(amount, 1, item.m_stack);
            if (amount < item.m_stack)
            {
                ItemDrop.ItemData part = ItemPacket.Copy(item, amount);
                item.m_stack -= amount;
                inventory.Changed();
                return new Escrow(item, part, amount);
            }
            if (item.m_equipped)
                player.UnequipItem(item, false);
            player.RemoveEquipAction(item);
            inventory.RemoveItem(item);
            return new Escrow(item, null, amount);
        }

        /// <summary>The chest took <paramref name="accepted"/> units; the rest goes back to the local player.</summary>
        public void Return(int accepted)
        {
            if (settled)
                return;
            settled = true;
            int rest = amount - Mathf.Clamp(accepted, 0, amount);
            Player player = Player.m_localPlayer;
            if (rest <= 0)
                return;
            if (player == null)
            {
                Plugin.Log.LogWarning($"OpenKeep: {rest} x {item.m_shared.m_name} held for a chest could not be returned: no player");
                return;
            }
            if (part != null)
                ReturnPart(player, rest);
            else
                ReturnWhole(player, rest);
        }

        private void ReturnPart(Player player, int rest)
        {
            Inventory inventory = player.GetInventory();
            if (inventory.ContainsItem(item) && item.m_stack + rest <= item.m_shared.m_maxStackSize)
            {
                item.m_stack += rest;
                inventory.Changed();
                return;
            }
            part.m_stack = rest;
            ChestOps.GiveToPlayer(player, part, item.m_gridPos);
        }

        private void ReturnWhole(Player player, int rest)
        {
            Inventory inventory = player.GetInventory();
            item.m_stack = rest;
            Vector2i slot = item.m_gridPos;
            bool inside = slot.x >= 0 && slot.y >= 0 && slot.x < inventory.GetWidth() && slot.y < inventory.GetHeight();
            if (inside && inventory.GetItemAt(slot.x, slot.y) == null)
            {
                inventory.m_inventory.Add(item);
                inventory.Changed();
                return;
            }
            ChestOps.GiveToPlayer(player, item, ChestOps.NoSlot);
        }
    }
}
