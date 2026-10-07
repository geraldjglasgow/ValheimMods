using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills' add RPC on cooking stations and the oven (<see cref="Keys.RpcAddItem"/>). The game's CookItem, on
    /// the cook's client, removes the raw item and sends "RPC_AddItem"(item, cheated) to the station's ZDO owner, which
    /// puts it into the first free slot (GetFreeSlot) with SetSlot, shows it to everybody and plays the add effect. Our
    /// RPC carries the same two values plus the cook's level. The owner runs the game's own RPC_AddItem unchanged, then
    /// stores the level in the slot it filled (<see cref="StationSlots"/>). A plain "RPC_AddItem" (a vanilla client,
    /// another mod) takes the same path with level 0. Registered on every CookingStation where the game registers its
    /// own RPCs (Awake, when the ZDO exists), so a kitchen always has it.
    /// </summary>
    public static class StationRpc
    {
        private static CookingStation pendingStation;
        private static float pendingLevel;

        /// <summary>Sends the add to the station's owner, from the cook's client.</summary>
        public static void SendAdd(ZNetView nview, string prefab, bool cheated)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(prefab);
            pkg.Write(cheated);
            pkg.Write(CookLevel.Local());
            nview.InvokeRPC(Keys.RpcAddItem, pkg);
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview != null && nview.GetZDO() != null)
                    nview.Register<ZPackage>(Keys.RpcAddItem, (sender, pkg) => Guard.Run("station add", () => Receive(__instance, sender, pkg)));
            }
        }

        /// <summary>On the owner: the game's own add, with the cook's level remembered for the slot it fills.</summary>
        private static void Receive(CookingStation station, long sender, ZPackage pkg)
        {
            if (station == null || !station.m_nview.IsValid())
                return;
            string prefab = pkg.ReadString();
            bool cheated = pkg.ReadBool();
            pendingLevel = Sane(pkg.ReadSingle());
            pendingStation = station;
            try
            {
                station.RPC_AddItem(sender, prefab, cheated);
            }
            finally
            {
                pendingStation = null;
            }
        }

        /// <summary>A level that came over the network, trusted like every skill level but never negative, NaN or infinite.</summary>
        private static float Sane(float value) => float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value;

        /// <summary>Finds the slot the game's RPC_AddItem fills (free before, holding the item after) and stores the level there.</summary>
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddItem))]
        private static class AddItem
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, out int __state)
            {
                bool tracked = Kitchen.IsKitchen(__instance) && __instance.m_nview != null && __instance.m_nview.IsValid();
                __state = tracked ? __instance.GetFreeSlot() : -1;
            }

            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance, string itemName, int __state)
            {
                if (__state < 0 || !__instance.m_nview.IsValid())
                    return;
                __instance.GetSlot(__state, out string name, out _, out _, out _);
                if (name != itemName)
                    return;
                float level = pendingStation == __instance ? pendingLevel : 0f;
                StationSlots.Write(__instance.m_nview.GetZDO(), __state, level);
            }
        }
    }
}
