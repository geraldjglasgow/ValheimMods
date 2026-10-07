using HarmonyLib;
using Hotkeys;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Find in chest: while a Mímir's Chest is open, Ctrl + right click on a stack in the player's own inventory searches
    /// the chest for that item (its name in the game's language), so the chest shows where it is kept. A plain right
    /// click still uses or equips the item as the game does; Ctrl + right click does nothing in the game.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    public static class MimirFind
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item)
        {
            Container open = __instance.m_currentContainer;
            if (item == null || grid != __instance.m_playerGrid || open == null || open.m_name != MimirPrefab.ContainerName)
                return true;
            if (!Hotkey.KeyHeld(KeyCode.LeftControl) && !Hotkey.KeyHeld(KeyCode.RightControl))
                return true;
            MimirSearch.Search(ItemNames.DisplayName(item));
            return false;
        }
    }
}
