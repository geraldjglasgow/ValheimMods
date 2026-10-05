using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;

namespace PackPanel.Worn
{
    /// <summary>
    /// Where a piece goes when it comes off: a free main cell (bottom row first, as the game places what it picks up; for
    /// a backpack none of its own cells, which go with it), and with Auto Equip, when there is none, the ground at the
    /// player's feet through the game's own drop (networked like any dropped item).
    /// </summary>
    public static class GearOut
    {
        public static Vector2i FreeCell(Inventory inventory, SlotKind kind) => kind == SlotKind.Backpack
            ? MainCells.FindEmptyOffPack(inventory, InventoryState.Layout)
            : MainCells.FindEmpty(inventory, InventoryState.Layout, topFirst: false);

        /// <summary>Out of its worn slot: into a free main cell, else dropped. The piece is no longer worn.</summary>
        public static void Away(Humanoid humanoid, ItemDrop.ItemData item, SlotKind kind)
        {
            Inventory inventory = humanoid.GetInventory();
            Vector2i free = FreeCell(inventory, kind);
            if (free.x < 0)
            {
                humanoid.DropItem(inventory, item, item.m_stack);
                return;
            }
            item.m_gridPos = free;
            inventory.Changed();
        }

        /// <summary>Whether the item lies, unworn, in a worn slot of its own kind in the local player's inventory.</summary>
        public static bool LiesUnworn(Humanoid humanoid, ItemDrop.ItemData item, out SlotKind kind)
        {
            kind = SlotKind.Retired;
            if (!humanoid.GetInventory().ContainsItem(item) || humanoid.IsItemEquiped(item))
                return false;
            Slot slot = InventoryState.Layout.SlotAt(item.m_gridPos);
            if (slot == null || !SlotRules.IsWorn(slot.Kind) || SlotRules.WornKindOf(item) != slot.Kind)
                return false;
            kind = slot.Kind;
            return true;
        }
    }
}
