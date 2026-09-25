using BepInEx.Configuration;
using EliteCrafting.Config;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// Input for grinding (salvage.md section 2): the per-player Salvage key, Shift, the guards, and the hovered slot of
    /// the player's own grid. Read through the game's own input layer (ZInput), which also serves the key as rebound
    /// in the .cfg. Local player only.
    /// </summary>
    internal static class SalvageInput
    {
        /// <summary>The key went down this frame with its own modifiers held; Shift is the confirm, so it is not a conflict.</summary>
        public static bool KeyPressed()
        {
            KeyboardShortcut shortcut = ModSettings.SalvageKey?.Value ?? KeyboardShortcut.Empty;
            if (shortcut.MainKey == KeyCode.None || !ZInput.GetKeyDown(shortcut.MainKey, false))
            {
                return false;
            }
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(modifier, false))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ShiftHeld() => ZInput.GetKey(KeyCode.LeftShift, false) || ZInput.GetKey(KeyCode.RightShift, false);

        /// <summary>An item being dragged, a dialog or split popup, text focus (console, chat, a text field), a panel over the grid, teleporting.</summary>
        public static bool Blocked(InventoryGui gui, Player player)
        {
            bool dialog = (gui.m_splitDialog != null && gui.m_splitDialog.IsActive) || UnifiedPopup.IsVisible();
            bool typing = Console.IsVisible() || TextInput.IsVisible() || Minimap.InTextInput()
                || (Chat.instance != null && Chat.instance.HasFocus());
            return gui.m_dragGo != null || dialog || typing || PanelOpen(gui) || player.IsTeleporting();
        }

        // A panel of the inventory screen drawn over the grid (the pointer test would find the hidden slot beneath it).
        private static bool PanelOpen(InventoryGui gui) =>
            gui.m_trophiesPanel.activeSelf || gui.m_achievementsPanel.gameObject.activeSelf
            || gui.m_skillsDialog.gameObject.activeSelf || gui.m_textsDialog.gameObject.activeSelf
            || gui.m_variantDialog.gameObject.activeSelf;

        /// <summary>The item under the pointer in the player's own grid (not a container, not the hotbar HUD); null on an empty slot.</summary>
        public static ItemDrop.ItemData? Hovered(InventoryGui gui)
        {
            InventoryGrid grid = gui.m_playerGrid;
            InventoryElement? element = grid != null ? grid.GetHoveredElement() : null;
            if (element == null)
            {
                return null;
            }
            Vector2i pos = grid!.GetElementPos(element);
            return pos.x < 0 ? null : grid.GetInventory().GetItemAt(pos.x, pos.y);
        }
    }
}
