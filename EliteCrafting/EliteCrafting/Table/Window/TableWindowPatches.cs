using HarmonyLib;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// Ties the Rune Table's window to the game's inventory screen: it closes when the screen hides or when the screen
    /// is shown for anything else (a chest, the inventory key), and it is checked and redrawn on the screen's own update.
    /// Local player only.
    /// </summary>
    [HarmonyPatch]
    internal static class TableWindowPatches
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        private static void Hidden() => TableWindow.CloseFading();

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        [HarmonyPrefix]
        private static void Showing() => TableWindow.BeforeShow();

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        [HarmonyPostfix]
        private static void Updated() => TableWindow.Tick();
    }
}
