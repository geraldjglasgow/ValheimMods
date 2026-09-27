using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The set-filter RPC (<see cref="Keys.RpcSetFilter"/>, one int), registered on every kitchen's ZNetView on every
    /// machine: in a postfix on CookingStation.Awake, and on CraftingStation.Start, since a crafting station has no
    /// Awake and finds its ZNetView (which it may lack) in Start. Placement ghosts and other copies without a ZDO are
    /// skipped, as vanilla skips its own registrations there. A client sends to the ZDO's owner, claiming ownership
    /// first when nobody owns it (as vanilla's add-food does); only the owner writes the ZDO, which then replicates.
    /// </summary>
    public static class FilterRpc
    {
        /// <summary>Asks the kitchen's owner to set its minimum stars.</summary>
        public static void Send(ZNetView nview, int minStars)
        {
            if (!nview.HasOwner())
                nview.ClaimOwnership();
            nview.InvokeRPC(Keys.RpcSetFilter, minStars);
        }

        private static void Register(ZNetView nview)
        {
            if (nview == null || nview.GetZDO() == null || nview.m_functions.ContainsKey(Keys.RpcSetFilter.GetStableHashCode()))
                return;
            nview.Register<int>(Keys.RpcSetFilter, (sender, minStars) => Receive(nview, minStars));
        }

        private static void Receive(ZNetView nview, int minStars)
        {
            if (nview != null && nview.IsOwner())
                nview.GetZDO().Set(Keys.MinStars, Mathf.Clamp(minStars, 0, Stars.Max));
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake))]
        private static class CookingStationAwake
        {
            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance)
            {
                if (Kitchen.IsKitchen(__instance))
                    Register(__instance.m_nview);
            }
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        private static class CraftingStationStart
        {
            [HarmonyPostfix]
            private static void Postfix(CraftingStation __instance)
            {
                if (Kitchen.IsKitchen(__instance))
                    Register(__instance.m_nview);
            }
        }
    }
}
