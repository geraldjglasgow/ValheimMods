using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Tackle
{
    /// <summary>
    /// The Tacklebox slot's own cell, every frame the grid is drawn: with a box in it, the game's count corner reads how many
    /// of its cells hold something out of how many it has ("3/6"), since the box itself stacks to one and the game shows no
    /// count for it. Empty, the slot shows its caption like any slot (<see cref="Panels.SlotLabels"/>).
    /// </summary>
    public static class TackleboxSlot
    {
        private static InventoryElement shownOn;
        private static int shownUsed = -1;
        private static int shownCells = -1;

        public static void Refresh(InventoryElement element)
        {
            if (element == null || !element.m_used || !Tacklebox.Active)
                return;
            IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(SlotKind.Tackle);
            Inventory inventory = InventoryState.Player.GetInventory();
            int used = 0;
            foreach (Vector2i cell in cells)
                used += inventory.GetItemAt(cell.x, cell.y) != null ? 1 : 0;
            element.m_amount.enabled = true;
            if (element == shownOn && used == shownUsed && cells.Count == shownCells)
                return;
            element.m_amount.text = used + "/" + cells.Count;
            shownOn = element;
            shownUsed = used;
            shownCells = cells.Count;
        }
    }
}
