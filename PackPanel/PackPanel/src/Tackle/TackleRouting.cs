using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Tackle
{
    /// <summary>
    /// Bait added to the player's inventory without a cell (pickups, a trader, a click move from a chest) goes into the
    /// tacklebox first, the way keys go to the ring and coins to the purse: onto a stack of the same bait in the box up to
    /// the stack size, then into an empty cell whole. What the box cannot hold goes on through the game's own add (another
    /// stack, then a free main cell). A take all is sorted out after it (<see cref="TakeAllRouting"/>). Called from the one
    /// AddItem patch (<see cref="AddRouting"/>), after the purse and the key ring.
    /// </summary>
    public static class TackleRouting
    {
        /// <summary>
        /// Moves what fits of an item into the box's cells (<see cref="SlotFill"/>); true when none is left outside it. The
        /// item is either not in the inventory yet (an add) or in a main cell (a take all).
        /// </summary>
        public static bool TakeIn(Inventory inventory, ItemDrop.ItemData item) =>
            SlotFill.TakeIn(inventory, item, InventoryState.CellsOf(SlotKind.Tackle));
    }
}
