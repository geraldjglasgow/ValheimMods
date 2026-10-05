using System.Collections.Generic;
using HarmonyLib;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// Backdrops in the item cells: every inventory grid (the player's with its slots, a container's, any other mod's)
    /// and the hotbar. Both run after the game's own per-frame icon update, while they are shown: the cells holding a
    /// magic item get their backdrop, emptied cells lose it.
    /// </summary>
    internal static class SlotBackdrops
    {
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private static class GridPatch
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGrid __instance)
            {
                Inventory? inventory = __instance.GetInventory();
                if (inventory == null)
                {
                    return;
                }
                int width = inventory.GetWidth();
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    InventoryElement? element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
                    if (element != null)
                    {
                        IconBackdrop.Set(element.m_icon, item);
                    }
                }
                foreach (InventoryElement element in __instance.m_elements)
                {
                    if (element != null && !element.m_used)
                    {
                        IconBackdrop.Hide(element.m_icon);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private static class HotbarPatch
        {
            [HarmonyPostfix]
            private static void Postfix(HotkeyBar __instance)
            {
                List<HotkeyBar.ElementData> elements = __instance.m_elements;
                foreach (ItemDrop.ItemData item in __instance.m_items)
                {
                    int slot = item.m_gridPos.x;
                    if (slot >= 0 && slot < elements.Count)
                    {
                        IconBackdrop.Set(elements[slot].m_icon, item);
                    }
                }
                foreach (HotkeyBar.ElementData element in elements)
                {
                    if (!element.m_used)
                    {
                        IconBackdrop.Hide(element.m_icon);
                    }
                }
            }
        }
    }
}
