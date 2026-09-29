using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// A backpack and the grave. A grave made while a pack was worn holds the pack's slots as main cells and the slot
    /// rows that many rows further down; the player who wakes has neither, so the game's take all (which puts each item
    /// back at its old cell, <see cref="GravePatches"/>) would miss every slot. So before a take all from the player's own
    /// grave the pack that was worn (the one lying in the Backpack slot's cell of the grave's layout) goes back into its
    /// slot first and the layout grows to match, and the grave's easy fit check counts the slots it brings. A spare pack
    /// carried in the grid is not mistaken for it. With Keep Slots On Death the pack never reaches the grave.
    /// </summary>
    public static class BackpackGrave
    {
        /// <summary>Free cells <see cref="FreeCellPatches"/> adds to the count while the easy fit check runs.</summary>
        public static int ExtraRoom { get; private set; }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
        public static class EasyFit
        {
            [HarmonyPrefix]
            public static void Prefix(TombStone __instance, Player player)
            {
                Container container = __instance.GetComponent<Container>();
                ItemDrop.ItemData pack = InventoryState.IsLocal(player) && container != null ? Waiting(container.GetInventory()) : null;
                ExtraRoom = BackpackSettings.Slots(BackpackCatalog.Of(pack));
            }

            [HarmonyFinalizer]
            public static void Finalizer() => ExtraRoom = 0;
        }

        /// <summary>
        /// Called by <see cref="GravePatches"/> before a take all from a grave (the take all's reply has made this client
        /// the grave's owner): the worn pack into its empty slot, then the layout.
        /// </summary>
        public static void WearFirst(Inventory inventory, Inventory grave)
        {
            ItemDrop.ItemData pack = Waiting(grave);
            IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(SlotKind.Backpack);
            if (pack == null || cells.Count == 0 || inventory.GetItemAt(cells[0].x, cells[0].y) != null)
                return;
            if (!inventory.AddItem(pack, pack.m_stack, cells[0].x, cells[0].y))
                return;
            grave.RemoveItem(pack);
            LayoutApply.Apply(InventoryState.Player, dropOverflow: true);
        }

        /// <summary>
        /// The rows the pack waiting in a grave adds, 0 when none waits: every slot cell of the grave lies that much lower
        /// than in the layout in use (<see cref="Tackle.TackleboxGrave"/> looks for its box there).
        /// </summary>
        public static int WaitingRows(Inventory grave)
        {
            BackpackKind kind = BackpackCatalog.Of(Waiting(grave));
            int width = InventoryState.Layout != null ? InventoryState.Layout.Width : 1;
            return kind != null ? (BackpackSettings.Slots(kind) + width - 1) / width : 0;
        }

        /// <summary>The pack in the grave that was worn, while the player now wears none, or null.</summary>
        private static ItemDrop.ItemData Waiting(Inventory grave)
        {
            if (grave == null || !InventoryState.Active || InventoryState.Layout.BackpackSlots > 0 || !BackpackSettings.Active)
                return null;
            foreach (ItemDrop.ItemData item in grave.GetAllItems())
            {
                BackpackKind kind = BackpackCatalog.Of(item);
                if (kind != null && item.m_gridPos == SlotCellWearing(kind))
                    return item;
            }
            return null;
        }

        /// <summary>Where the Backpack slot was in the grave: the layout in use, pushed down by the rows this pack adds.</summary>
        private static Vector2i SlotCellWearing(BackpackKind kind)
        {
            InventoryLayout layout = InventoryState.Layout;
            int index = layout.IndexOf(new Slot(SlotKind.Backpack, 1).Id);
            if (index < 0)
                return new Vector2i(-1, -1);
            int rows = (BackpackSettings.Slots(kind) + layout.Width - 1) / layout.Width;
            Vector2i cell = layout.CellOf(index);
            return new Vector2i(cell.x, cell.y + rows);
        }
    }
}
