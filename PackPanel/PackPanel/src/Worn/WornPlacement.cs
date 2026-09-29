using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;

namespace PackPanel.Worn
{
    /// <summary>
    /// Worn slots show what is worn: an item the player puts on moves into a free slot of its kind (swapping with an
    /// unworn item lying there), an item taken off moves out to a free main cell (it stays when the grid is full).
    /// Suspended while the game moves items itself: during a drag and drop (<see cref="DragSettle"/> sorts the result
    /// out afterwards), while the character loads (the layout is applied after) and while a tombstone is made (so
    /// the armour keeps its slots in the grave and comes back into them).
    /// </summary>
    public static class WornPlacement
    {
        private static int suspended;
        private static int equipping;

        public static bool Suspended => suspended > 0;

        /// <summary>
        /// Inside the game's EquipItem, which takes the old piece off first: the old piece stays in its slot until the
        /// new one arrives and swaps with it, so it lands where the new piece was rather than in the first free cell.
        /// </summary>
        public static void BeginEquip() => equipping++;

        public static void EndEquip() => equipping = equipping > 0 ? equipping - 1 : 0;

        public static void Suspend() => suspended++;

        public static void Resume() => suspended = suspended > 0 ? suspended - 1 : 0;

        /// <summary>After the game (or <see cref="ExtraUtilities"/>) put an item on.</summary>
        public static void OnWorn(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (!Suspended && item != null && InventoryState.IsLocal(humanoid) && InventoryState.Manages(humanoid.GetInventory()))
                Place(humanoid.GetInventory(), item);
        }

        /// <summary>After the game took an item off: out of its worn slot, into the main grid.</summary>
        public static void OnTakenOff(Humanoid humanoid, ItemDrop.ItemData item)
        {
            Inventory inventory = humanoid.GetInventory();
            if (Suspended || equipping > 0 || item == null || item.m_equipped || !InventoryState.IsLocal(humanoid) || !InventoryState.Manages(inventory))
                return;
            Slot slot = InventoryState.Layout.SlotAt(item.m_gridPos);
            if (slot == null || !SlotRules.IsWorn(slot.Kind) || !inventory.ContainsItem(item))
                return;
            Vector2i free = MainCells.FindEmpty(inventory, InventoryState.Layout, topFirst: false);
            if (free.x < 0)
                return;
            item.m_gridPos = free;
            inventory.Changed();
        }

        /// <summary>Every worn item into its slot, after a layout was applied; extra utilities without a slot come off.</summary>
        public static void SettleAll(Humanoid humanoid)
        {
            Inventory inventory = humanoid.GetInventory();
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (item.m_equipped)
                    Place(inventory, item);
            }
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(ExtraUtilities.Worn))
            {
                Slot slot = InventoryState.Layout.SlotAt(item.m_gridPos);
                if (slot == null || slot.Kind != SlotKind.Utility)
                    humanoid.UnequipItem(item, triggerEquipEffects: false);
            }
        }

        private static void Place(Inventory inventory, ItemDrop.ItemData item)
        {
            SlotKind? kind = SlotRules.WornKindOf(item);
            Slot current = InventoryState.Layout.SlotAt(item.m_gridPos);
            if (kind == null || (current != null && current.Kind == kind.Value))
                return;
            Vector2i? cell = CellFor(inventory, kind.Value);
            if (cell == null)
                return;
            ItemDrop.ItemData occupant = inventory.GetItemAt(cell.Value.x, cell.Value.y);
            if (occupant != null)
                occupant.m_gridPos = item.m_gridPos;
            item.m_gridPos = cell.Value;
            inventory.Changed();
        }

        /// <summary>A free slot of the kind, else one holding an unworn item, else none.</summary>
        private static Vector2i? CellFor(Inventory inventory, SlotKind kind)
        {
            Vector2i? unworn = null;
            foreach (Vector2i cell in InventoryState.CellsOf(kind))
            {
                ItemDrop.ItemData occupant = inventory.GetItemAt(cell.x, cell.y);
                if (occupant == null)
                    return cell;
                if (unworn == null && !occupant.m_equipped)
                    unworn = cell;
            }
            return unworn;
        }
    }
}
