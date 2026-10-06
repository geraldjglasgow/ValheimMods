using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Worn
{
    /// <summary>
    /// Auto Equip (the user's request, 2026-10-05: "if gear is in the gear tab it auto equips. unequipping should move it
    /// to inventory if there is room, or throw it on the ground if not"), in the local player's frame. A piece taken off
    /// that found no free main cell (<see cref="WornPlacement.OnTakenOff"/>) is dropped at the player's feet; an unworn
    /// piece lying in its worn slot is put on (one dragged in from a chest, which the game copies so that
    /// <see cref="DragSettle"/> never sees it; one left after an attack or a swim refused it). The drop waits for the
    /// frame: the game takes a piece off before it removes it itself (an upgrade at a station, a drop, a click move into
    /// a chest), and dropping inside that call would leave a copy on the ground. A piece whose equip another mod refuses
    /// is not tried again until it leaves its slot. Backpacks keep their own rule (<see cref="Backpacks.BackpackEquip"/>).
    /// </summary>
    public static class GearKeep
    {
        private static readonly SlotKind[] Kinds = { SlotKind.Head, SlotKind.Chest, SlotKind.Legs, SlotKind.Back, SlotKind.Utility, SlotKind.Trinket };
        private static readonly List<ItemDrop.ItemData> leaving = new List<ItemDrop.ItemData>();
        private static readonly List<ItemDrop.ItemData> refused = new List<ItemDrop.ItemData>();

        public static bool On => InventoryState.Active && InventorySettings.AutoEquip.Value;

        /// <summary>A piece waits to be dropped, so the next frame must look (<see cref="Core.WearGate"/>).</summary>
        public static bool Pending => leaving.Count > 0;

        /// <summary>A piece taken off with no free main cell: dropped at the next frame if it still lies there unworn.</summary>
        public static void Leave(ItemDrop.ItemData item)
        {
            if (!leaving.Contains(item))
                leaving.Add(item);
        }

        public static void Tick(Player player)
        {
            if (!On || player.IsDead())
            {
                leaving.Clear();
                refused.Clear();
                return;
            }
            if (WornPlacement.Suspended || Dragging())
                return;
            SendOff(player);
            Forget(player);
            WearAll(player);
        }

        private static void SendOff(Player player)
        {
            if (leaving.Count == 0)
                return;
            ItemDrop.ItemData[] items = leaving.ToArray();
            leaving.Clear();
            foreach (ItemDrop.ItemData item in items)
            {
                if (GearOut.LiesUnworn(player, item, out SlotKind kind))
                    GearOut.Away(player, item, kind);
            }
        }

        /// <summary>A refused piece that left its slot (or was put on another way) may be tried again.</summary>
        private static void Forget(Player player)
        {
            for (int i = refused.Count - 1; i >= 0; i--)
            {
                if (!GearOut.LiesUnworn(player, refused[i], out _))
                    refused.RemoveAt(i);
            }
        }

        private static void WearAll(Player player)
        {
            Inventory inventory = player.GetInventory();
            foreach (SlotKind kind in Kinds)
            {
                IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(kind);
                for (int i = 0; i < cells.Count; i++)
                    Wear(player, inventory.GetItemAt(cells[i].x, cells[i].y), kind);
            }
        }

        private static void Wear(Player player, ItemDrop.ItemData item, SlotKind kind)
        {
            if (item == null || SlotRules.WornKindOf(item) != kind || player.IsItemEquiped(item) || refused.Contains(item))
                return;
            if (!WearCheck.Guards(player, item, tell: false) || !WearCheck.Beside(player, item))
                return;
            if (!player.EquipItem(item))
                refused.Add(item);
        }

        private static bool Dragging() => InventoryGui.instance != null && InventoryGui.instance.m_dragGo != null;
    }
}
