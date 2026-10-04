using System;
using HarmonyLib;
using PackPanel.Core;

namespace PackPanel.Panels
{
    /// <summary>
    /// <c>InventoryGui.Show(Container, int)</c> prefix: the crafting panel's size (<see cref="CraftingPanel"/>) is set
    /// before the game builds the recipe list (Show calls <c>UpdateCraftingPanel</c>), so the rows are made at the new
    /// width; it follows the screen, the inventory's own panels and the settings each time the inventory opens. A change
    /// of Crafting Panel Width or Height applies at once to an open inventory.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    public static class CraftingPanelPatch
    {
        private static bool hooked;

        [HarmonyPrefix]
        public static void Prefix(InventoryGui __instance)
        {
            Hook();
            CraftingPanel.Apply(__instance);
        }

        private static void Hook()
        {
            if (hooked)
                return;
            hooked = true;
            InventorySettings.CraftingWidth.SettingChanged += Changed;
            InventorySettings.CraftingHeight.SettingChanged += Changed;
        }

        /// <summary>Logged, never thrown back into the setting's change (a cfg reload must go on).</summary>
        private static void Changed(object sender, EventArgs args)
        {
            try
            {
                InventoryGui gui = InventoryGui.instance;
                if (gui == null || !InventoryGui.IsVisible() || Player.m_localPlayer == null)
                    return;
                CraftingPanel.Apply(gui);
                gui.UpdateCraftingPanel();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"PackPanel: resizing the crafting panel failed: {e}");
            }
        }
    }
}
