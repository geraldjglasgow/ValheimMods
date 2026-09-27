using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Experience at cooking stations and the oven. CookingStation.OnInteract runs on the interacting player's client
    /// and does one of two things: when a slot holds a done item, it asks the owner to take off the first such slot
    /// (RPC_RemoveDoneItem picks it the same way) and raises Cooking by 0.6; otherwise it puts on the first raw item
    /// FindCookableItem finds in the player's inventory and raises 0.4. The prefix picks the same dish the same way
    /// and opens an <see cref="XpScope"/> around the call. Taking a dish off counts as making it, for the discovery
    /// bonus; putting food on does not, and neither does a burnt item (the station's overcooked item).
    /// </summary>
    public static class StationXp
    {
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
        private static class InteractPatch
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, Humanoid user, out bool __state) =>
                __state = Kitchen.IsKitchen(__instance) && Open(__instance, user);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => XpScope.End(__state);
        }

        private static bool Open(CookingStation station, Humanoid user)
        {
            if (station.m_nview == null || !station.m_nview.IsValid())
                return false;
            string done = DoneItem(station);
            if (done != null)
                return XpScope.Begin(ItemData(done), IsDish(station, done) ? done : null, 1);
            ItemDrop.ItemData raw = user == null ? null : station.FindCookableItem(user.GetInventory());
            return raw != null && XpScope.Begin(Output(station, raw), null, 1);
        }

        /// <summary>The prefab name of the item OnInteract takes off: the first slot holding a done item. Null when none.</summary>
        private static string DoneItem(CookingStation station)
        {
            for (int slot = 0; slot < station.m_slots.Length; slot++)
            {
                station.GetSlot(slot, out string name, out _, out _, out _);
                if (name != "" && station.IsItemDone(name))
                    return name;
            }
            return null;
        }

        private static bool IsDish(CookingStation station, string name) =>
            station.m_overCookedItem == null || name != station.m_overCookedItem.name;

        /// <summary>What the raw item turns into at this station, or null.</summary>
        private static ItemDrop.ItemData Output(CookingStation station, ItemDrop.ItemData raw)
        {
            CookingStation.ItemConversion conversion = raw.m_dropPrefab == null ? null : station.GetItemConversion(raw.m_dropPrefab.name);
            ItemDrop to = conversion?.m_to;
            return to == null ? null : to.m_itemData;
        }

        private static ItemDrop.ItemData ItemData(string prefabName)
        {
            ItemDrop drop = Kitchen.ItemPrefab(prefabName);
            return drop == null ? null : drop.m_itemData;
        }
    }
}
