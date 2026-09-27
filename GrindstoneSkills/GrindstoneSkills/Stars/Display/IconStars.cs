using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars on item icons in every InventoryGrid: the player's inventory, the open container, and any other mod's grid
    /// built on InventoryGrid. UpdateGui runs every frame while the inventory is open, fills each slot from the item at
    /// its grid position and marks that slot m_used; afterwards every slot gets its item's stars, or none. The Stars On
    /// Icons setting is read every time, so switching it takes effect at once.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class IconStars
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryGrid __instance)
        {
            Inventory inventory = __instance.m_inventory;
            if (inventory == null)
                return;
            foreach (InventoryElement element in __instance.m_elements)
                if (!element.m_used)
                    StarBadge.Apply(element.gameObject, 0);
            bool on = DisplaySettings.StarsOnIcons.Value;
            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                InventoryElement element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
                if (element != null)
                    StarBadge.Apply(element.gameObject, on ? Stars.Get(item) : 0);
            }
        }
    }
}
