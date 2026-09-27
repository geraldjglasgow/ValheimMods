using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Taking a dish off a cooking station or the oven. The taker's client runs OnInteract: with a done slot it rolls
    /// the game's bonus yield (chance InventoryGui.m_craftBonusChance times the taker's skill factor, adding
    /// m_craftBonusAmount) and sends "RPC_RemoveDoneItem"(position, amount) to the owner. At a kitchen OnInteract runs
    /// with GrindstoneSkills' Extra Food chance and amount in those two fields (<see cref="ExtraFood"/>). The owner calls
    /// SpawnItem(name, slot, ...) once per dish for the first done slot and then clears it; each spawned dish gets that
    /// slot's rolled stars through <see cref="StationSpawnStars"/>. Burnt output (the station's overcooked item) gets
    /// none.
    /// </summary>
    public static class StationTakeOff
    {
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
        private static class Interact
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, out ExtraFood.State __state) =>
                __state = Kitchen.IsKitchen(__instance) ? ExtraFood.Apply() : default;

            [HarmonyFinalizer]
            private static void Finalizer(ExtraFood.State __state) => ExtraFood.Restore(__state);
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SpawnItem))]
        private static class Spawn
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, string name, int slot, out bool __state)
            {
                __state = Kitchen.IsKitchen(__instance) && __instance.m_nview.IsValid();
                if (!__state)
                    return;
                StationSpawnStars.Begin();
                __instance.GetSlot(slot, out _, out _, out CookingStation.Status status, out _);
                if (status == CookingStation.Status.Done)
                    StationSpawnStars.Add(name, StationSlots.DishStars(__instance.m_nview.GetZDO(), slot));
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                    StationSpawnStars.End();
            }
        }
    }
}
