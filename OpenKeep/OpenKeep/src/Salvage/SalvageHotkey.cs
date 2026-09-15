using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The Salvage Key: while the inventory is open and no popup shows, the hovered stack of the player grid (the
    /// gamepad selection when nothing is under the pointer) is salvaged after the game's yes/no popup.
    /// Signature verified: InventoryGui.Update().
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    public static class SalvageHotkey
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            if (!SalvageSettings.Enabled.Value || !Keys.InventoryOpen || UnifiedPopup.IsVisible())
                return;
            if (__instance.m_dragItem != null || !Keys.Pressed(SalvageSettings.SalvageKey))
                return;
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            ItemDrop.ItemData item = Hovered(__instance.m_playerGrid);
            if (item == null)
                return;
            SalvageActions.Confirm(player, item, () => RefreshPanel(__instance));
        }

        /// <summary>The stack under the pointer in the player grid, or the gamepad selection when a gamepad is in use.</summary>
        public static ItemDrop.ItemData Hovered(InventoryGrid grid)
        {
            if (grid == null)
                return null;
            Vector3 pointer = ZInput.pointerPosition;
            ItemDrop.ItemData item = grid.GetItem(new Vector2i((int)pointer.x, (int)pointer.y));
            if (item == null && ZInput.IsGamepadActive())
                item = grid.GetGamepadSelectedItem();
            return item;
        }

        private static void RefreshPanel(InventoryGui gui)
        {
            if (gui != null && Keys.InventoryOpen)
                gui.UpdateCraftingPanel();
        }
    }
}
