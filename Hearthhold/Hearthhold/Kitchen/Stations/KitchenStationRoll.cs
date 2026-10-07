using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Rolls a dish's stars when it turns done on a kitchen cooking station or the oven. Only the game's UpdateCooking
    /// turns a slot done, on the station's ZDO owner: it runs every second on every peer, and on the owner adds the
    /// elapsed time to each slot and, past the recipe's cook time, sets the recipe's output with status Done (past twice
    /// the cook time, the burnt item with status Burnt). The prefix notes, as a bit mask, which slots hold food still
    /// cooking; the postfix rolls each of them that turned done, from the cook's level and the raw food's stars stored
    /// with the slot (<see cref="KitchenSlots"/>): level + <see cref="StarOdds.IngredientBonus"/>, never below the
    /// Gourmet floor. A slot that went straight to burnt gets nothing. Cheap: one owner and kitchen test, then reads by
    /// precomputed key hashes, no allocation; on other peers and on stations with nothing cooking the postfix returns
    /// at once. GrindstoneSkills' cooking-speed prefix on the same method only moves cooked times; done and burnt stay
    /// the game's decision, which this reads afterwards.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    public static class KitchenStationRoll
    {
        /// <summary>Slots a mask can hold; a station with more rolls its first 32.</summary>
        private const int MaskSlots = 32;

        private static int cooking;

        [HarmonyPrefix]
        private static void Prefix(CookingStation __instance, out int __state)
        {
            cooking = 0;
            if (Kitchen.IsKitchen(__instance))
                HookGuard.Run("station cooking", static station => cooking = Cooking(station), __instance);
            __state = cooking;
        }

        [HarmonyPostfix]
        private static void Postfix(CookingStation __instance, int __state)
        {
            if (__state != 0)
                HookGuard.Run("station roll", static args => RollDone(args.Item1, args.Item2), (__instance, __state));
        }

        /// <summary>On the owner, the slots holding food not yet done; 0 elsewhere.</summary>
        private static int Cooking(CookingStation station)
        {
            ZNetView nview = station.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return 0;
            ZDO zdo = nview.GetZDO();
            int mask = 0;
            int slots = System.Math.Min(station.m_slots.Length, MaskSlots);
            for (int slot = 0; slot < slots; slot++)
            {
                if (KitchenSlots.Status(zdo, slot) == CookingStation.Status.NotDone && KitchenSlots.Name(zdo, slot).Length > 0)
                    mask |= 1 << slot;
            }
            return mask;
        }

        private static void RollDone(CookingStation station, int cookingMask)
        {
            ZNetView nview = station.m_nview;
            if (!nview.IsValid() || !nview.IsOwner())
                return;
            ZDO zdo = nview.GetZDO();
            int slots = System.Math.Min(station.m_slots.Length, MaskSlots);
            for (int slot = 0; slot < slots; slot++)
            {
                if ((cookingMask & (1 << slot)) == 0 || KitchenSlots.Status(zdo, slot) != CookingStation.Status.Done)
                    continue;
                if (Kitchen.IsProduct(KitchenSlots.Name(zdo, slot)))
                    KitchenSlots.SetStars(zdo, slot, Roll(zdo, slot));
            }
        }

        private static int Roll(ZDO zdo, int slot)
        {
            float level = KitchenSlots.Level(zdo, slot);
            float input = KitchenSlots.InputStars(zdo, slot);
            int floor = Mathf.Max(Professions.Floor(StarSource.Dish, level), StarOdds.IngredientFloor(input));
            return StarOdds.Roll(level, StarOdds.IngredientBonus(input), floor);
        }
    }
}
