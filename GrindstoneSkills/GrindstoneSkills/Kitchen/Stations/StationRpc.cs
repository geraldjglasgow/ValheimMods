using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills' add RPC on cooking stations and the oven (<see cref="Keys.RpcAddItem"/>). The game's CookItem, on
    /// the cook's client, removes the raw item and sends "RPC_AddItem"(item, cheated) to the station's ZDO owner, which
    /// puts it into the first free slot (GetFreeSlot) with SetSlot, shows it to everybody and plays the add effect. Our
    /// RPC carries the same two values plus the input's stars and the cook's level and player ID. The owner runs the
    /// game's own RPC_AddItem unchanged, then stores the cook in the slot it filled (<see cref="StationSlots"/>). A
    /// plain "RPC_AddItem" (a vanilla client, another mod) takes the same path with an unknown cook. Registered on every
    /// CookingStation where the game registers its own RPCs (Awake, when the ZDO exists), so a kitchen always has it.
    /// </summary>
    public static class StationRpc
    {
        private static CookingStation pendingStation;
        private static StationSlots.Cook pendingCook;

        /// <summary>Sends the add to the station's owner, from the cook's client.</summary>
        public static void SendAdd(ZNetView nview, string prefab, bool cheated, int inputStars)
        {
            Player player = Player.m_localPlayer;
            ZPackage pkg = new ZPackage();
            pkg.Write(prefab);
            pkg.Write(cheated);
            pkg.Write((float)inputStars);
            pkg.Write(CookLevel.Local());
            pkg.Write(player != null ? player.GetPlayerID() : 0L);
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

        /// <summary>On the owner: the game's own add, with the cook remembered for the slot it fills.</summary>
        private static void Receive(CookingStation station, long sender, ZPackage pkg)
        {
            if (station == null || !station.m_nview.IsValid())
                return;
            string prefab = pkg.ReadString();
            bool cheated = pkg.ReadBool();
            pendingCook = ReadCook(pkg);
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

        /// <summary>The rest of the payload, in <see cref="SendAdd"/>'s order. The level is trusted like every skill level.</summary>
        private static StationSlots.Cook ReadCook(ZPackage pkg) => new StationSlots.Cook
        {
            InputStars = UnityEngine.Mathf.Clamp(Sane(pkg.ReadSingle()), 0f, Stars.Max),
            Level = Sane(pkg.ReadSingle()),
            PlayerId = pkg.ReadLong(),
        };

        private static float Sane(float value) => float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value;

        /// <summary>Finds the slot the game's RPC_AddItem fills (free before, holding the item after) and stores the cook there.</summary>
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
                StationSlots.Cook cook = pendingStation == __instance ? pendingCook : default;
                StationSlots.Write(__instance.m_nview.GetZDO(), __state, cook);
            }
        }
    }
}
