using UnityEngine;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The inventory operations of a shared chest, written once and used by both sides: the owner applies them to
    /// its live chest when a request is granted, and the synchronous path of <see cref="ChestWriter"/> applies
    /// them locally when the client may change the chest at once. Everything goes through the game's own
    /// inventory methods (<c>AddItem</c>, <c>RemoveItem</c>, <c>MoveItemToThis</c>), so counts never change and
    /// every change fires the inventory's change callback.
    /// </summary>
    public static class ChestOps
    {
        public static readonly Vector2i NoSlot = new Vector2i(-1, -1);

        /// <summary>Removes <paramref name="amount"/> units of the stack and returns a copy holding exactly them.</summary>
        public static ItemDrop.ItemData Take(Inventory inventory, ItemDrop.ItemData item, int amount)
        {
            ItemDrop.ItemData taken = ItemPacket.Copy(item, amount);
            inventory.RemoveItem(item, taken.m_stack);
            return taken;
        }

        /// <summary>
        /// Moves up to <paramref name="amount"/> units of a stack into another inventory, into the slot when it
        /// is free or holds the same kind, else anywhere; the source loses exactly what arrived. Returns the units moved.
        /// </summary>
        public static int Transfer(Inventory from, ItemDrop.ItemData item, int amount, Inventory to, Vector2i slot)
        {
            amount = Mathf.Min(amount, item.m_stack);
            if (amount <= 0 || !from.ContainsItem(item))
                return 0;
            ItemDrop.ItemData copy = ItemPacket.Copy(item, amount);
            int added = Add(to, copy, slot);
            if (added > 0)
                from.RemoveItem(item, added);
            return added;
        }

        /// <summary>
        /// Adds an item into an inventory: into the slot when it is free or holds the same kind with room, the
        /// rest anywhere. Returns the units added; the item's stack is reduced to what did not fit.
        /// </summary>
        public static int Add(Inventory inventory, ItemDrop.ItemData item, Vector2i slot)
        {
            int wanted = item.m_stack;
            if (slot.x >= 0 && SlotTakes(inventory, item, slot))
                inventory.AddItem(item, item.m_stack, slot.x, slot.y);
            if (item.m_stack <= 0)
                return wanted;
            if (inventory.AddItem(item))
                return wanted;
            return wanted - item.m_stack;
        }

        /// <summary>
        /// The player receives an item the chest's owner handed over: into the slot or anywhere; what no longer
        /// fits is dropped in front of the player, the way the game drops what a player cannot carry.
        /// </summary>
        public static void GiveToPlayer(Player player, ItemDrop.ItemData item, Vector2i slot)
        {
            int wanted = item.m_stack;
            int added = Add(player.GetInventory(), item, slot);
            if (added >= wanted)
                return;
            int rest = wanted - added;
            Transform at = player.transform;
            ItemDrop drop = ItemDrop.DropItem(item, rest, at.position + at.forward + at.up, at.rotation);
            if (drop != null)
                drop.OnPlayerDrop();
            player.Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + item.m_shared.m_name, rest, item.GetIcon());
            Plugin.Log.LogDebug($"OpenKeep: {rest} x {item.m_shared.m_name} did not fit the inventory any more and were dropped");
        }

        /// <summary>
        /// A sent copy goes into the chest: at the slot when it is free or holds the same kind with room (a
        /// request never swaps stacks), without a slot anywhere. Returns the units accepted; the copy keeps the rest.
        /// </summary>
        public static int Put(Inventory chest, ItemDrop.ItemData copy, Vector2i slot)
        {
            int wanted = copy.m_stack;
            if (slot.x >= 0)
            {
                if (!SlotTakes(chest, copy, slot))
                    return 0;
                chest.AddItem(copy, copy.m_stack, slot.x, slot.y);
                return wanted - copy.m_stack;
            }
            return chest.AddItem(copy) ? wanted : wanted - copy.m_stack;
        }

        /// <summary>The slot lies in the grid and is empty or holds a stack of the same kind with room.</summary>
        public static bool SlotTakes(Inventory inventory, ItemDrop.ItemData item, Vector2i slot)
        {
            if (slot.x < 0 || slot.y < 0 || slot.x >= inventory.GetWidth() || slot.y >= inventory.GetHeight())
                return false;
            ItemDrop.ItemData at = inventory.GetItemAt(slot.x, slot.y);
            return at == null || (at.IsSameType(item) && at.m_stack < at.m_shared.m_maxStackSize);
        }

        /// <summary>
        /// Moves within one inventory like the game's drag and drop: onto an empty slot, onto the same kind
        /// (merge), or a swap when a whole stack lands on a different item. With <paramref name="expectedName"/>
        /// the stack must still be the one the requester saw.
        /// </summary>
        public static bool Move(Inventory inventory, Vector2i from, Vector2i to, int amount, string expectedName)
        {
            ItemDrop.ItemData item = inventory.GetItemAt(from.x, from.y);
            if (item == null || (expectedName != null && item.m_shared.m_name != expectedName))
                return false;
            if (from == to)
                return true;
            if (to.x < 0 || to.y < 0 || to.x >= inventory.GetWidth() || to.y >= inventory.GetHeight())
                return false;
            amount = Mathf.Clamp(amount, 1, item.m_stack);
            ItemDrop.ItemData target = inventory.GetItemAt(to.x, to.y);
            if (target == null || target.IsSameType(item))
                return inventory.MoveItemToThis(inventory, item, amount, to.x, to.y);
            if (amount != item.m_stack)
                return false;
            Vector2i position = item.m_gridPos;
            item.m_gridPos = target.m_gridPos;
            target.m_gridPos = position;
            inventory.Changed();
            return true;
        }

        /// <summary>Removes what the owner accepted from the requester's own stack; a stack that changed meanwhile is logged.</summary>
        public static void RemoveFromPlayer(Inventory inventory, ItemDrop.ItemData item, int accepted)
        {
            if (accepted <= 0 || item == null)
                return;
            if (!inventory.ContainsItem(item))
            {
                Plugin.Log.LogWarning($"OpenKeep: the chest accepted {accepted} x {item.m_shared.m_name} but the stack left the inventory meanwhile");
                return;
            }
            if (item.m_stack < accepted)
            {
                Plugin.Log.LogWarning($"OpenKeep: the chest accepted {accepted} x {item.m_shared.m_name} but only {item.m_stack} are left in the inventory");
                accepted = item.m_stack;
            }
            inventory.RemoveItem(item, accepted);
        }
    }
}
