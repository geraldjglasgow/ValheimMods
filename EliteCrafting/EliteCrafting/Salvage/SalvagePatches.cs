using EliteCrafting.Config;
using HarmonyLib;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// The Salvage key (salvage.md section 2): read once per frame after the inventory panel updated, only while it is
    /// visible; the hovered slot of the player's own grid is ground. Local player only (UI).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    internal static class SalvageKeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryGui __instance)
        {
            if (!InventoryGui.IsVisible() || !SalvageInput.KeyPressed())
            {
                return;
            }
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData? item = player != null && !SalvageInput.Blocked(__instance, player) ? SalvageInput.Hovered(__instance) : null;
            if (item != null)
            {
                Grinder.Request(player!, item, SalvageInput.ShiftHeld());
            }
        }
    }

    /// <summary>
    /// Fusing (salvage.md section 6): a right-click on a shard stack in the player's own grid fuses it instead of the
    /// vanilla use, which does nothing for a material from the inventory panel (Humanoid.UseItem finds nothing to
    /// equip and says nothing). Every other item, and any container slot, stays vanilla. Local player only (UI).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    internal static class FuseClickPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (item == null || grid != __instance.m_playerGrid || !Fuser.IsShard(item))
            {
                return true;
            }
            Player player = Player.m_localPlayer;
            if (player != null && !player.IsTeleporting())
            {
                Fuser.Fuse(player, grid.GetInventory(), item, SalvageInput.ShiftHeld());
            }
            return false;
        }
    }
}
