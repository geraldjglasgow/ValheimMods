using PackPanel.Core;

namespace PackPanel.Slots
{
    /// <summary>
    /// Arrows, bolts and other equipable ammo added to the player's inventory without a cell (a pickup, crafting, a
    /// trader, a click move from a chest) go into the Ammo slots first (the user's request, 2026-09-28), the way coins go
    /// to the purse: onto a stack of the same ammo in an Ammo slot up to the stack size, left to right, then into an empty
    /// Ammo slot whole (<see cref="SlotFill"/>). What the slots cannot hold goes on through the game's own add (another
    /// stack, then a free main cell). Bait is the tacklebox's (<see cref="Tackle.TackleRouting"/>): the game's baits are
    /// ammo too, so they are left out here. A take all is sorted out after it (<see cref="TakeAllRouting"/>). Called from
    /// the one AddItem patch (<see cref="AddRouting"/>), after the purse, the key ring and the tacklebox.
    /// </summary>
    public static class AmmoRouting
    {
        public static bool IsAmmo(ItemDrop.ItemData item) =>
            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo && !Tackle.TackleRules.IsBait(item);

        /// <summary>Moves what fits of the ammo into the Ammo slots; true when none is left outside them.</summary>
        public static bool TakeIn(Inventory inventory, ItemDrop.ItemData item) =>
            SlotFill.TakeIn(inventory, item, InventoryState.CellsOf(SlotKind.Ammo));
    }
}
