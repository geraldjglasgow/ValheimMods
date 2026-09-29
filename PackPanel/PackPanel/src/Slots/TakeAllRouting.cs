using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Tackle;

namespace PackPanel.Slots
{
    /// <summary>
    /// A take all into the player's inventory (<c>Inventory.MoveAll</c>) puts each item back at its old cell when that cell
    /// is free, so a key, a bait or an arrow from a chest would land in whichever main cell matches its cell in the chest.
    /// Once the take all is done, every key that came in and sits in a main cell goes to its ring cell, every bait into the
    /// tacklebox and every arrow or bolt into the Ammo slots, as a pickup does (<see cref="KeyRouting"/>,
    /// <see cref="TackleRouting"/>, <see cref="AmmoRouting"/>). What comes back from the player's own grave is back in its
    /// ring, box or Ammo cell already.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
    public static class TakeAllRouting
    {
        [HarmonyPrefix]
        public static void Prefix(Inventory __instance, out HashSet<ItemDrop.ItemData> __state)
        {
            bool routes = InventoryState.Manages(__instance) && (KeyRing.Active || Tacklebox.Active || InventoryState.CellsOf(SlotKind.Ammo).Count > 0);
            __state = routes ? new HashSet<ItemDrop.ItemData>(__instance.GetAllItems()) : null;
        }

        [HarmonyPostfix]
        public static void Postfix(Inventory __instance, HashSet<ItemDrop.ItemData> __state)
        {
            if (__state == null)
                return;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(__instance.GetAllItems()))
            {
                if (__state.Contains(item) || !InventoryState.Layout.IsMain(item.m_gridPos))
                    continue;
                if (Route(__instance, item) && item.m_stack <= 0)
                    __instance.RemoveItem(item);
            }
        }

        private static bool Route(Inventory inventory, ItemDrop.ItemData item)
        {
            if (KeyRing.IsKey(item))
                return KeyRouting.TakeIn(inventory, item);
            if (AmmoRouting.IsAmmo(item))
                return AmmoRouting.TakeIn(inventory, item);
            return Tacklebox.Active && TackleRules.IsTackle(item) && TackleRouting.TakeIn(inventory, item);
        }
    }
}
