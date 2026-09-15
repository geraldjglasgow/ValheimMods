using HarmonyLib;

namespace Lockstep
{
    /// <summary>Where the mod attaches to the game's lifecycle: world start and end, players joining and leaving.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetAwakePatch
    {
        // After the YAML hooks of SyncedConfig, which load the chain file on a dedicated server in their own postfix.
        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        public static void Postfix(ZNet __instance)
        {
            ProgressServer.RegisterRpcs();
            if (__instance.IsServer() && __instance.IsDedicated())
            {
                // A dedicated server never spawns a player, so the YAML apply hook on first spawn never fires.
                Lockstep.Synced.Yaml.ApplyAll();
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    public static class ZNetDestroyPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => ProgressServer.Shutdown();
    }

    /// <summary>The server learns a connecting client's persistent player ID here.</summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PlayerID")]
    public static class ZNetPlayerIdPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNet __instance, ZRpc rpc)
        {
            ZNetPeer peer = __instance.GetPeer(rpc);
            if (peer != null)
                ProgressServer.PlayerSeen(peer.m_playerID, peer.m_playerName);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    public static class ZNetDisconnectPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNetPeer peer)
        {
            if (peer != null)
                ProgressServer.PlayerLeft(peer.m_playerID);
        }
    }

    /// <summary>The hosting player is not a peer of their own server, so they are added to the roster on spawn.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerSpawnedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer() && !ZNet.instance.IsDedicated() && __instance == Player.m_localPlayer)
                ProgressServer.PlayerSeen(__instance.GetPlayerID(), __instance.GetPlayerName());
        }
    }
}
