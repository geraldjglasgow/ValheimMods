using HarmonyLib;
using PackPanel.Core;
using PackPanel.Layout;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// Backpack Portal Pass (off by default): while the worn pack is marked <c>portal: true</c> in PackPanel.Backpacks.yml
    /// (the Moosehide Pack), what lies in its slots may go through portals even when the game forbids it (ores, metals,
    /// eggs). The rest of the inventory keeps the game's rule, and so do the items the game never lets through (a tool
    /// tier of 1000 or more, checked first as the game does). The portal's check runs on the player's own client, so
    /// the local player's inventory is all this concerns.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
    public static class BackpackPortal
    {
        [HarmonyPrefix]
        public static bool Prefix(Inventory __instance, bool allowAllItems, ref bool __result)
        {
            if (allowAllItems || !InventoryState.Manages(__instance) || !BackpackSettings.Portal(Backpack.Worn(InventoryState.Player)))
                return true;
            __result = Allowed(__instance, InventoryState.Layout);
            return false;
        }

        private static bool Allowed(Inventory inventory, InventoryLayout layout)
        {
            int firstPackRow = layout.MainRows - layout.BackpackRows;
            bool all = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll);
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_shared.m_toolTier >= 1000)
                    return false;
                bool inPack = layout.IsMain(item.m_gridPos) && item.m_gridPos.y >= firstPackRow;
                if (!item.m_shared.m_teleportable && !inPack && !all)
                    return false;
            }
            return true;
        }
    }
}
