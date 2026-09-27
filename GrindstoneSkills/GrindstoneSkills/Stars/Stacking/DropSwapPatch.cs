using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Dropping a whole stack onto a stack of the same dish with other stars swaps the two, as dropping it onto any
    /// other item does. InventoryGrid.DropItem swaps when the names differ, or the qualities differ on an upgradable
    /// item, or the target cannot stack, and only when the whole stack is dropped; otherwise it merges through
    /// MoveItemToThis, which <see cref="SameTypePatch"/> now refuses for different stars, so the drop would do nothing.
    /// For that case this runs the game's own three swap calls in its place. A part of a stack (a split) still only
    /// tries to merge, which does nothing for different stars, as for any two different items.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    public static class DropSwapPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos,
            ref bool __result)
        {
            Inventory inventory = __instance.m_inventory;
            ItemDrop.ItemData target = inventory?.GetItemAt(pos.x, pos.y);
            if (fromInventory == null || !StarsDiffer(target, item, amount))
                return true;
            fromInventory.RemoveItem(item);
            fromInventory.MoveItemToThis(inventory, target, target.m_stack, item.m_gridPos.x, item.m_gridPos.y);
            inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
            __result = true;
            return false;
        }

        /// <summary>A whole stack dropped on the same kitchen item with another quality.</summary>
        private static bool StarsDiffer(ItemDrop.ItemData target, ItemDrop.ItemData item, int amount)
        {
            return target != null && item != null && target != item && item.m_stack == amount
                && target.m_shared.m_name == item.m_shared.m_name && target.m_quality != item.m_quality
                && Kitchen.IsKitchenItem(item);
        }
    }
}
