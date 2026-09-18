using HarmonyLib;
using Party.Client;
using Party.Server;

namespace Party.Hooks
{
    /// <summary>Reports this player's own death to the party. Runs only on the dying player's own client.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class DeathPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !PartyClientState.InParty || ZRoutedRpc.instance == null)
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcDeath, __instance.transform.position);
        }
    }
}
