using OpenKeep.Core;

namespace OpenKeep.Stow
{
    /// <summary><c>Dump Key</c> outside the inventory: polled after the local player's update, never while the
    /// inventory, the menu or the console is up.</summary>
    public static class DumpKeyPatch
    {
        /// <summary>After the local player's update (<see cref="PlayerUpdatePatch"/>).</summary>
        public static void Tick(Player __instance)
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
