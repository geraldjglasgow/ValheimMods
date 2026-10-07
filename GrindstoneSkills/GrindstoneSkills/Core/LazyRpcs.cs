using System;
using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills' RPCs on the game's most numerous objects (plants, pickables, chunked rocks, tameables), registered on an
    /// object's view when the first one arrives for it rather than on every one that wakes. Thousands of those wake on
    /// every machine, a dedicated server included, and almost none ever receives one; a registration each was a closure,
    /// a handler and a dictionary entry per object for nothing.
    /// <list type="bullet">
    /// <item>The game hands every routed RPC for an object to its view (ZNetView.HandleRoutedRPC) on the machine it is
    /// addressed to: the object's owner for all of these, a client or the server alike, and the sender itself when it
    /// owns the object. A prefix there sees one of these RPCs that the view has no handler for yet, has the feature
    /// register its handlers on that view, and the game's own lookup right after finds them, so the first call is
    /// delivered like every later one, in the order it came.</item>
    /// <item>Any other RPC passes with one dictionary lookup.</item>
    /// </list>
    /// </summary>
    public static class LazyRpcs
    {
        private static readonly Dictionary<int, Action<ZNetView>> Registrars = new Dictionary<int, Action<ZNetView>>
        {
            { Keys.RpcTend.GetStableHashCode(), PlantRpcs.RegisterOn },
            { Keys.RpcFertilize.GetStableHashCode(), PlantRpcs.RegisterOn },
            { Keys.RpcCleanStrike.GetStableHashCode(), CleanStrikeMarks.RegisterOn },
            { Keys.RpcPet.GetStableHashCode(), Petting.RegisterOn },
        };

        /// <summary>Whether the view already has a handler for this RPC.</summary>
        public static bool Has(ZNetView view, string rpc) => view.m_functions.ContainsKey(rpc.GetStableHashCode());

        [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.HandleRoutedRPC))]
        private static class Arrival
        {
            [HarmonyPrefix]
            private static void Prefix(ZNetView __instance, ZRoutedRpc.RoutedRPCData rpcData)
            {
                int hash = rpcData.m_methodHash;
                if (Registrars.TryGetValue(hash, out Action<ZNetView> register) && !__instance.m_functions.ContainsKey(hash))
                    HookGuard.Run("object rpc registration", register, __instance);
            }
        }
    }
}
