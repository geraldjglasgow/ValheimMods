using EliteCreaturesReborn.Patches;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Keeps everything but coins out of a Raiders Chest (<see cref="ChestCoins"/>), wherever an item could come from:
    /// a drag and drop or a swap in the chest's window, Shift + click, "stack all" and any other mod's quick stack through
    /// the game's inventory calls. The refused item stays where it was. Patched are the game's public ways in -
    /// <c>CanAddItem</c>, both <c>AddItem</c>s taking an item, <c>MoveItemToThis</c> to a cell and <c>MoveAll</c> - never
    /// the private add a container's load goes through, so a chest's saved contents always load whole. The window's drop
    /// is checked before the game moves anything, because a swap takes the dragged item out of its inventory first and
    /// would lose it if the chest then refused it. <c>CanAddItem</c> runs for every item a player walks over: for every
    /// inventory that is not a Raiders Chest each patch costs one string comparison.
    /// </summary>
    [HarmonyPatch]
    internal static class ChestCoinsPatch
    {
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
        [HarmonyPrefix]
        private static bool BeforeCanAdd(Inventory __instance, ItemDrop.ItemData item, ref bool __result) =>
            Admit(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
        [HarmonyPrefix]
        private static bool BeforeAdd(Inventory __instance, ItemDrop.ItemData item, ref bool __result) =>
            Admit(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
        [HarmonyPrefix]
        private static bool BeforeAddAt(Inventory __instance, ItemDrop.ItemData item, ref bool __result) =>
            Admit(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData),
            typeof(int), typeof(int), typeof(int))]
        [HarmonyPrefix]
        private static bool BeforeMoveTo(Inventory __instance, ItemDrop.ItemData item, ref bool __result) =>
            Admit(__instance, item, ref __result);

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
        [HarmonyPrefix]
        private static bool BeforeMoveAll(Inventory __instance, Inventory fromInventory)
        {
            if (!ChestCoins.IsChest(__instance))
            {
                return true;
            }
            SafeCall.Run("Raiders Chest move all", static (chest, from) => ChestCoins.MoveCoins(chest, from), __instance, fromInventory);
            return false;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        [HarmonyPrefix]
        private static bool BeforeDrop(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos,
            ref bool __result)
        {
            Inventory to = __instance.GetInventory();
            if (!Refused(to, item) && !SwapRefused(to, fromInventory, item, pos))
            {
                return true;
            }
            __result = false;
            return false;
        }

        private static bool Admit(Inventory inventory, ItemDrop.ItemData item, ref bool result)
        {
            if (!Refused(inventory, item))
            {
                return true;
            }
            result = false;
            return false;
        }

        private static bool Refused(Inventory? inventory, ItemDrop.ItemData? item) =>
            item != null && ChestCoins.IsChest(inventory) && !ChestCoins.Takes(inventory!, item);

        // Dropping a chest's coin on another item in the player's grid swaps them: that item would go into the chest.
        private static bool SwapRefused(Inventory? to, Inventory? from, ItemDrop.ItemData? item, Vector2i pos)
        {
            if (to == null || ReferenceEquals(to, from) || !ChestCoins.IsChest(from))
            {
                return false;
            }
            ItemDrop.ItemData? there = to.GetItemAt(pos.x, pos.y);
            return there != null && !ReferenceEquals(there, item) && !ChestCoins.Takes(from!, there);
        }
    }
}
