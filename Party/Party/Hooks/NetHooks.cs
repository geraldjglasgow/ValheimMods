using HarmonyLib;
using Party.Client;
using Party.Server;

namespace Party.Hooks
{
    /// <summary>RPC registration, world start/end, and player identity on join/disconnect.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetAwakePatch
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        public static void Postfix()
        {
            PartyRpcServer.RegisterRpcs();
            PartyRpcClient.RegisterRpcs();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    public static class ZNetDestroyPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            PartyManager.Shutdown();
            PartyClientState.Reset();
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PlayerID")]
    public static class ZNetPlayerIdPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNet __instance, ZRpc rpc)
        {
            ZNetPeer peer = __instance.GetPeer(rpc);
            if (peer != null)
                NetHooks.PlayerSeen(peer.m_playerID, peer.m_playerName);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    public static class ZNetDisconnectPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNetPeer peer)
        {
            if (peer != null)
                NetHooks.PlayerLeft(peer.m_playerID);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            if (ZNet.instance != null && ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
                NetHooks.PlayerSeen(__instance.GetPlayerID(), __instance.GetPlayerName());
        }
    }

    /// <summary>The shared roster-touch logic the patches above call into.</summary>
    public static class NetHooks
    {
        public static void PlayerSeen(long id, string name)
        {
            if (!Identity.IsServer || id == 0)
                return;
            PartyRecord party = PartyManager.FindPartyOf(id);
            if (party == null)
                return;
            PartyManager.Touch(party, id, name);
            PartyManager.SaveAndPublish(party);
        }

        public static void PlayerLeft(long id)
        {
            if (!Identity.IsServer || id == 0)
                return;
            PartyRecord party = PartyManager.FindPartyOf(id);
            if (party != null)
                PartyPublisher.Publish(party);
        }
    }
}
