using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Room for an egg counts only stacks with its own stars. Inventory.CanAddItem adds the free room of every stack
    /// with the item's name (FindFreeStackSpace ignores quality) to the room in empty slots, while AddItem only fills
    /// stacks of the same quality (FindFreeStackItem). With a full inventory and room in a 1-star stack, the player's
    /// auto pickup would keep pulling a 0-star egg in and failing with "no room". For star items the stack room is
    /// counted in stacks with the same name, quality and world level only.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
    public static class StackRoomPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
        {
            if (!__result || !Stars.IsStarItem(item))
                return;
            __result = Room(__instance, item) >= (stack <= 0 ? item.m_stack : stack);
        }

        private static int Room(Inventory inventory, ItemDrop.ItemData item)
        {
            int room = inventory.GetEmptySlots() * item.m_shared.m_maxStackSize;
            foreach (ItemDrop.ItemData other in inventory.GetAllItems())
            {
                if (other.m_shared.m_name == item.m_shared.m_name && other.m_quality == item.m_quality && other.m_worldLevel == item.m_worldLevel)
                    room += Mathf.Max(0, other.m_shared.m_maxStackSize - other.m_stack);
            }
            return room;
        }
    }
}
