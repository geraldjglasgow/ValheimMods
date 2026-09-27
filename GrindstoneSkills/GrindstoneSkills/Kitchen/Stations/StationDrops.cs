using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A kitchen station that breaks drops what it holds. The game's OnDestroyed calls DropAllItems on the ZDO owner:
    /// it drops the fuel, then for each slot in order the done dish, the burnt item or the raw input, creating each
    /// with ItemDrop.OnCreateNew, and empties the slot. The prefix queues each slot's stars in that order before
    /// anything is dropped (<see cref="StationSpawnStars"/>): a done dish keeps its rolled stars and a raw input the
    /// stars it went on with (dough, unbaked pies), so breaking a station costs no stars.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.DropAllItems))]
    public static class StationDrops
    {
        [HarmonyPrefix]
        private static void Prefix(CookingStation __instance, out bool __state)
        {
            __state = Kitchen.IsKitchen(__instance) && __instance.m_nview != null && __instance.m_nview.IsValid();
            if (!__state)
                return;
            StationSpawnStars.Begin();
            ZDO zdo = __instance.m_nview.GetZDO();
            for (int slot = 0; slot < __instance.m_slots.Length; slot++)
            {
                __instance.GetSlot(slot, out string name, out _, out CookingStation.Status status, out _);
                if (name == "" || status == CookingStation.Status.Burnt)
                    continue;
                int stars = status == CookingStation.Status.Done ? StationSlots.DishStars(zdo, slot) : StationSlots.RawStars(zdo, slot);
                StationSpawnStars.Add(name, stars);
            }
        }

        [HarmonyFinalizer]
        private static void Finalizer(bool __state)
        {
            if (__state)
                StationSpawnStars.End();
        }
    }
}
