using System.Collections.Generic;
using PackPanel.Layout;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Where each slot is drawn in the slot panel, as (column, row) of its cells under the tab buttons. Two tabs
    /// (<see cref="SlotTabs"/>) of the same size: Gear has Head, Chest, Legs, Back and the Backpack in a column on the
    /// left, the utilities and under them the Trinket slot in a column on the right (six rows with five utilities) and
    /// the stat sheet between them (<see cref="GearStats"/>);
    /// Consumables has a row each of food, mead and ammo from the top. A kind with no slots leaves its place empty in
    /// Gear and no row in Consumables, which is not offered at all without consumable slots. Under both, on a last row
    /// of its own under a divider, the coin purse, right of it the key ring's button (the ring cells are drawn in its
    /// pop-up, <see cref="KeyRingPopup"/>) and right of that the Tacklebox slot (its cells in its own pop-up,
    /// <see cref="Tackle.TacklePopup"/>). Only the shown tab's cells and that row's are in <see cref="Cells"/>; the
    /// others are hidden. Every group holds five at most, so the panel is five cells wide.
    /// </summary>
    public sealed class SlotPanelLayout
    {
        public const int MinColumns = 5;

        /// <summary>The Gear tab's rows at least: the left column's five kinds and room for the sheet; the right column may need one more.</summary>
        public const int GearRows = 5;

        private static readonly SlotKind[] LeftColumn = { SlotKind.Head, SlotKind.Chest, SlotKind.Legs, SlotKind.Back, SlotKind.Backpack };
        private static readonly SlotKind[] RightColumn = { SlotKind.Utility, SlotKind.Trinket };
        private static readonly SlotKind[] ConsumableRows = { SlotKind.Food, SlotKind.Mead, SlotKind.Ammo };

        public Dictionary<int, Vector2Int> Cells { get; } = new Dictionary<int, Vector2Int>();

        /// <summary>The tab drawn: the one asked for, or Gear while there are no consumable slots.</summary>
        public SlotTab Tab { get; private set; }

        public bool HasConsumables { get; private set; }

        public int Columns { get; private set; } = MinColumns;

        /// <summary>The rows above the purse's, the same in both tabs so the panel keeps its size.</summary>
        public int ContentRows { get; private set; } = GearRows;

        public int Rows { get; private set; }

        /// <summary>The purse's row, the last, which also holds the key ring's button; -1 without either.</summary>
        public int PurseRow { get; private set; } = -1;

        /// <summary>Where the key ring's button is drawn; null without ring cells.</summary>
        public Vector2Int? RingCell { get; private set; }

        /// <summary>Where the Tacklebox slot is drawn, right of the ring's button; null without the slot. Its cells hang in its pop-up.</summary>
        public Vector2Int? TackleboxCell { get; private set; }

        public static SlotPanelLayout For(InventoryLayout layout, SlotTab tab)
        {
            SlotPanelLayout panel = new SlotPanelLayout();
            int rows = panel.AddConsumables(layout, tab == SlotTab.Consumables);
            panel.HasConsumables = rows > 0;
            panel.Tab = panel.HasConsumables ? tab : SlotTab.Gear;
            int gear = panel.AddColumn(layout, RightColumn, panel.Columns - 1, panel.Tab == SlotTab.Gear);
            panel.AddColumn(layout, LeftColumn, 0, panel.Tab == SlotTab.Gear);
            panel.ContentRows = Mathf.Max(GearRows, Mathf.Max(gear, rows));
            panel.Rows = panel.ContentRows;
            panel.AddPurseRow(layout);
            return panel;
        }

        /// <summary>A row each for food, mead and ammo that have slots, into <see cref="Cells"/> when shown; the rows used.</summary>
        private int AddConsumables(InventoryLayout layout, bool shown)
        {
            int rows = 0;
            foreach (SlotKind kind in ConsumableRows)
            {
                int column = 0;
                for (int i = 0; i < layout.Slots.Count; i++)
                {
                    if (layout.Slots[i].Kind != kind)
                        continue;
                    if (shown)
                        Cells[i] = new Vector2Int(column, rows);
                    column++;
                }
                Columns = Mathf.Max(Columns, column);
                rows += column > 0 ? 1 : 0;
            }
            return rows;
        }

        /// <summary>
        /// The slots of the kinds top down in one column, in the layout's order, into <see cref="Cells"/> when shown; the
        /// rows used, counted in both tabs so they keep the same size.
        /// </summary>
        private int AddColumn(InventoryLayout layout, SlotKind[] kinds, int column, bool shown)
        {
            int row = 0;
            for (int i = 0; i < layout.Slots.Count; i++)
            {
                if (System.Array.IndexOf(kinds, layout.Slots[i].Kind) < 0)
                    continue;
                if (shown)
                    Cells[i] = new Vector2Int(column, row);
                row++;
            }
            return row;
        }

        /// <summary>
        /// The purse, the key ring's button and the Tacklebox slot, in that order, on a last row of their own under both
        /// tabs; no row without any of them.
        /// </summary>
        private void AddPurseRow(InventoryLayout layout)
        {
            int purses = 0;
            for (int i = 0; i < layout.Slots.Count; i++)
                if (layout.Slots[i].Kind == SlotKind.Purse)
                    Cells[i] = new Vector2Int(purses++, Rows);
            bool ring = layout.CellsOf(SlotKind.Key).Count > 0;
            int box = layout.IndexOf(new Slot(SlotKind.Tacklebox, 1).Id);
            if (purses == 0 && !ring && box < 0)
                return;
            PurseRow = Rows++;
            int column = purses;
            if (ring)
                RingCell = new Vector2Int(column++, PurseRow);
            if (box >= 0)
            {
                TackleboxCell = new Vector2Int(column++, PurseRow);
                Cells[box] = TackleboxCell.Value;
            }
            Columns = Mathf.Max(Columns, column);
        }
    }
}
