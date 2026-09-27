using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Putting raw food on a cooking station or the oven. Every way of doing it (Interact, using an item on the
    /// station, the oven's add-food switch) ends in CookingStation.CookItem on the cook's client: it claims an unowned
    /// station, refuses incompatible and unknown items and a full station, removes one item from the inventory and
    /// sends "RPC_AddItem" to the owner. At a kitchen the prefix runs the same steps in the same order and sends
    /// <see cref="StationRpc"/>'s add instead, which carries the input's stars and the cook's level and ID; the return
    /// value is the game's. Other stations (the FrostFoundry) keep the game's own method.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.CookItem))]
    public static class StationPlacement
    {
        [HarmonyPrefix]
        private static bool Prefix(CookingStation __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (!Kitchen.IsKitchen(__instance))
                return true;
            __result = Cook(__instance, user, item);
            return false;
        }

        private static bool Cook(CookingStation station, Humanoid user, ItemDrop.ItemData item)
        {
            string prefab = item.m_dropPrefab.name;
            ZNetView nview = station.m_nview;
            if (!nview.HasOwner())
                nview.ClaimOwnership();
            if (RefuseIncompatible(station, user, item))
                return true;
            if (!station.IsItemAllowed(item) || station.GetFreeSlot() == -1)
                return false;
            int inputStars = Stars.Get(item);
            user.GetInventory().RemoveOneItem(item);
            StationRpc.SendAdd(nview, prefab, item.m_cheated, inputStars);
            return true;
        }

        /// <summary>The game's message for an item this station never takes; true when the item is one of those.</summary>
        private static bool RefuseIncompatible(CookingStation station, Humanoid user, ItemDrop.ItemData item)
        {
            foreach (CookingStation.ItemMessage incompatible in station.m_incompatibleItems)
            {
                string name = incompatible.m_item.m_itemData.m_shared.m_name;
                if (name != item.m_shared.m_name)
                    continue;
                user.Message(MessageHud.MessageType.Center, incompatible.m_message + " " + name);
                return true;
            }
            return false;
        }
    }
}
