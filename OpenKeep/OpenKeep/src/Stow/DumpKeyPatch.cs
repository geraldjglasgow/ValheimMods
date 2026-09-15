using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Stow
{
    /// <summary><c>Dump Key</c> outside the inventory: polled after the local player's update, never while the
    /// inventory, the menu or the console is up.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class DumpKeyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !StowSettings.Enabled.Value || Keys.InventoryOpen)
                return;
            if (Menu.IsVisible() || Console.IsVisible() || UnifiedPopup.IsVisible())
                return;
            if (Keys.Pressed(StowSettings.DumpKey))
                StowActions.Dump();
        }
    }
}
