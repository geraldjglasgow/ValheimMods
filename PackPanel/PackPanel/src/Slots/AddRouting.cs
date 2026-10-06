using HarmonyLib;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Tackle;

namespace PackPanel.Slots
{
    /// <summary>
    /// PackPanel's one patch on the game's <c>Inventory.AddItem(item)</c> (an add without a cell): the player's inventory is
    /// checked once, then the item is offered to each slot route in a fixed order, the purse (<see cref="CoinPurse"/>), the
    /// key ring (<see cref="KeyRouting"/>), the tacklebox (<see cref="TackleRouting"/>) and the Ammo slots
    /// (<see cref="AmmoRouting"/>). The first that takes it whole ends the add; what none takes whole goes on through the
    /// game's own add. An add another mod's prefix already made is left alone, or the same item would also land in a slot.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
    public static class AddRouting
    {
        [HarmonyPrefix]
        public static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result, bool __runOriginal)
        {
            if (!__runOriginal)
                return false;
            if (item == null || !InventoryState.Manages(__instance) || !Routed(__instance, item))
                return true;
            __result = true;
            return false;
        }

        private static bool Routed(Inventory inventory, ItemDrop.ItemData item)
        {
            if (SlotRules.IsCoins(item) && CoinPurse.TakeIn(inventory, item))
                return true;
            if (KeyRouting.TakeIn(inventory, item))
                return true;
            if (Tacklebox.Active && TackleRules.IsTackle(item) && TackleRouting.TakeIn(inventory, item))
                return true;
            return AmmoRouting.IsAmmo(item) && AmmoRouting.TakeIn(inventory, item);
        }
    }
}
