using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Rolls a dish's stars when it turns done on a cooking station or the oven. Only the game's UpdateCooking turns a
    /// slot done, on the station's ZDO owner: it sets the recipe's output with status Done and plays the done effect
    /// there. The prefix notes which slots are still cooking; the postfix rolls each one that turned done from the level
    /// and input stars stored with the slot. A dish the kitchen's trash filter refuses is cleared the way the game's
    /// RPC_RemoveDoneItem clears a slot (SetSlot empty, then "RPC_SetSlotVisual" to everybody), with the station's
    /// burnt effect played at the slot the way the game plays it, and the cook is credited the take-off experience.
    /// Kept dishes get their stars in the slot. A slot that goes straight from cooking to burnt, and an output that
    /// cannot carry stars, gets nothing.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    public static class StationRoll
    {
        [HarmonyPrefix]
        private static void Prefix(CookingStation __instance, out bool[] __state) => __state = Cooking(__instance);

        [HarmonyPostfix]
        private static void Postfix(CookingStation __instance, bool[] __state)
        {
            if (__state == null || !__instance.m_nview.IsOwner())
                return;
            for (int slot = 0; slot < __state.Length; slot++)
            {
                if (!__state[slot])
                    continue;
                __instance.GetSlot(slot, out string name, out _, out CookingStation.Status status, out _);
                if (status == CookingStation.Status.Done && name != "" && Kitchen.IsKitchenItem(name))
                    Finish(__instance, slot, name);
            }
        }

        /// <summary>On the owner of a kitchen, which slots hold food still cooking; null when none do.</summary>
        private static bool[] Cooking(CookingStation station)
        {
            ZNetView nview = station.m_nview;
            if (!Kitchen.IsKitchen(station) || nview == null || !nview.IsOwner())
                return null;
            bool[] cooking = null;
            for (int slot = 0; slot < station.m_slots.Length; slot++)
            {
                station.GetSlot(slot, out string name, out _, out CookingStation.Status status, out _);
                if (name == "" || status != CookingStation.Status.NotDone)
                    continue;
                cooking = cooking ?? new bool[station.m_slots.Length];
                cooking[slot] = true;
            }
            return cooking;
        }

        private static void Finish(CookingStation station, int slot, string dish)
        {
            ZDO zdo = station.m_nview.GetZDO();
            float level = StationSlots.Level(zdo, slot);
            int stars = StarOdds.Roll(CookLevel.Effective(level, StationSlots.InputStars(zdo, slot)));
            if (KitchenFilter.Trashes(station.m_nview, stars))
                Trash(station, slot, dish, StationSlots.CookId(zdo, slot));
            else
                StationSlots.SetStars(zdo, slot, stars);
        }

        private static void Trash(CookingStation station, int slot, string dish, long cookId)
        {
            station.SetSlot(slot, "", 0f, CookingStation.Status.NotDone, cheated: false);
            station.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", slot, "");
            station.m_overcookedEffect.Create(station.m_slots[slot].position, Quaternion.identity);
            CompostTrash.Add(station.m_slots[slot].position, 1);
            if (cookId != 0L)
                CookCredit.Send(cookId, dish);
        }
    }
}
