using System.Collections.Generic;
using HarmonyLib;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The game's inventory steps that read an item's stack size, answered for big stacks (<see cref="MimirStacks"/>).
    /// One patch per method: into a Mímir inventory the add, merge, room and load steps use the 9999 limit
    /// (<see cref="MimirStacksAdd"/>); into any other inventory a stack bigger than its item's normal stack (one coming
    /// out of a Mímir's Chest) goes one normal stack at a time (<see cref="MimirStacksOut"/>). Everything else runs as the
    /// game wrote it. The load step runs on every peer that loads a chest from its ZDO, dedicated server included.
    /// </summary>
    public static class MimirStacksPatches
    {
        /// <summary>Inventory.AddItem(item): the move click, Place stacks, Store all, Quick stack, OpenKeep's store routing.</summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
        private static class AddPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (item == null)
                    return true;
                if (MimirStacks.IsMimir(__instance))
                    __result = MimirStacksAdd.Add(__instance, item, null);
                else if (MimirStacks.Oversized(item))
                    __result = MimirStacksOut.AddPart(__instance, item, null);
                else
                    return true;
                return false;
            }
        }

        /// <summary>Inventory.AddItem(item, cell): the same with a wished cell for the new stack.</summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData), typeof(Vector2i) })]
        private static class AddAtPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
            {
                if (item == null)
                    return true;
                if (MimirStacks.IsMimir(__instance))
                    __result = MimirStacksAdd.Add(__instance, item, pos);
                else if (MimirStacks.Oversized(item))
                    __result = MimirStacksOut.AddPart(__instance, item, pos);
                else
                    return true;
                return false;
            }
        }

        /// <summary>The private add onto one cell: drag and drop (MoveItemToThis), MoveAll (Take all), the load.</summary>
        [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
        private static class AddToCellPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref int amount, int x, int y, bool skipValidPositionCheck, ref bool __result, out bool __state)
            {
                __state = false;
                if (!MimirStacks.Stacks(item))
                    return true;
                if (!MimirStacks.IsMimir(__instance))
                {
                    __state = MimirStacksOut.Cap(item, ref amount);
                    return true;
                }
                return !MimirStacksAdd.TryMerge(__instance, item, amount, x, y, skipValidPositionCheck, out __result);
            }

            /// <summary>A cut add reports the move unfinished, so MoveAll keeps the rest of the stack in its source.</summary>
            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData item, bool __state, ref bool __result)
            {
                if (__state && item.m_stack > 0)
                    __result = false;
            }
        }

        /// <summary>The room check (Quick stack, Store all, routing, Reach's put back) counts 9999 per stack in a Mímir's Chest.</summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
        private static class RoomPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
            {
                if (__result || item == null || !MimirStacks.IsMimir(__instance))
                    return;
                long wanted = stack <= 0 ? item.m_stack : stack;
                long room = __instance.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel)
                    + (long)__instance.GetEmptySlots() * MimirStacks.Limit(__instance, item);
                __result = room >= wanted;
            }
        }

        /// <summary>The free units in the stacks of one item, up to 9999 each in a Mímir's Chest.</summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindFreeStackSpace))]
        private static class FreeSpacePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Inventory __instance, string name, float worldLevel, ref int __result)
            {
                if (!MimirStacks.IsMimir(__instance))
                    return;
                int room = 0;
                foreach (ItemDrop.ItemData item in __instance.GetAllItems())
                {
                    if (item.m_shared.m_name != name || item.m_worldLevel != worldLevel)
                        continue;
                    int limit = MimirStacks.Limit(__instance, item);
                    if (item.m_stack < limit)
                        room += limit - item.m_stack;
                }
                __result = room;
            }
        }

        /// <summary>
        /// The load from the ZDO (Inventory.Load, on every peer) makes each item from its prefab and cuts its stack to the
        /// item's normal size first; for a Mímir inventory the new stack gets its saved count back, up to 9999.
        /// </summary>
        [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(int), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int),
            typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>), typeof(int), typeof(bool), typeof(bool), typeof(bool) })]
        private static class LoadPatch
        {
            [HarmonyPrefix]
            private static void Prefix(Inventory __instance, out int __state)
            {
                __state = MimirStacks.IsMimir(__instance) ? __instance.GetAllItems().Count : -1;
            }

            [HarmonyPostfix]
            private static void Postfix(Inventory __instance, int stack, int __state)
            {
                List<ItemDrop.ItemData> items = __instance.GetAllItems();
                if (__state < 0 || items.Count != __state + 1)
                    return;
                ItemDrop.ItemData added = items[__state];
                if (MimirStacks.Stacks(added) && added.m_stack < stack)
                    added.m_stack = System.Math.Min(stack, MimirStacks.Limit(__instance, added));
            }
        }
    }
}
