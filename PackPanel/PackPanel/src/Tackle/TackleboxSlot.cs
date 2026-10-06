using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Slots;
using TMPro;

namespace PackPanel.Tackle
{
    /// <summary>
    /// The Tacklebox slot's own cell, every frame the grid is drawn: with a box in it, the game's count corner reads how many
    /// of its cells hold something out of how many it has ("3/6"), since the box itself stacks to one and the game shows no
    /// count for it. Empty, the slot shows its caption like any slot (<see cref="Panels.SlotLabels"/>) and no count. The
    /// game's own writes to that count go to a stand-in (<see cref="Panels.GridHold"/>), so it is written here, only when
    /// what it shows changes.
    /// </summary>
    public static class TackleboxSlot
    {
        private static InventoryElement shownOn;
        private static int shownUsed = -1;
        private static int shownCells = -1;
        private static string shownText;

        public static void Refresh(InventoryElement element)
        {
            if (element == null || !Tacklebox.Active)
                return;
            TMP_Text amount = element.m_amount;
            if (amount.enabled != element.m_used)
                amount.enabled = element.m_used;
            if (!element.m_used)
                return;
            IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(SlotKind.Tackle);
            Inventory inventory = InventoryState.Player.GetInventory();
            int used = 0;
            for (int i = 0; i < cells.Count; i++)
                used += inventory.GetItemAt(cells[i].x, cells[i].y) != null ? 1 : 0;
            if (element == shownOn && used == shownUsed && cells.Count == shownCells && amount.text == shownText)
                return;
            shownText = used + "/" + cells.Count;
            amount.text = shownText;
            shownOn = element;
            shownUsed = used;
            shownCells = cells.Count;
        }
    }
}
