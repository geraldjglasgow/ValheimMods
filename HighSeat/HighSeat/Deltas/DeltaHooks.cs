using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Where the server starts hearing the delta calls (<see cref="DeltaPeers"/>) and lets go of them. A connection
    /// opening (ZNet.OnNewConnection, where the game registers its own first calls such as "PeerInfo"): the server
    /// listens from the start, so a player's hello, sent as their game opens the connection, is heard before the game
    /// accepts them and the first package can already be in the delta format. ZDOMan.AddPeer (the game's own peer
    /// list, where it registers "ZDOData") listens too, for peers that never went through OnNewConnection (DevBridge's
    /// fake players). ZDOMan.RemovePeer (every disconnect, accepted or not, goes through ZNet.Disconnect to it) and
    /// ZDOMan.ShutDown (the world closing) let go; the game also calls RemovePeer when it handles a connection's
    /// "ServerHandshake" (ZNet.RPC_ServerHandshake, ClearPlayerData), which is why HighSeatPlus sends its hello after the
    /// game's handshake. Server only (a dedicated server or the player hosting); none of these is hot, and each catches
    /// its own failures so the game's connection code always runs on.
    /// </summary>
    internal static class DeltaHooks
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class Opened
        {
            private static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                Listen(__instance, peer);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
        private static class Added
        {
            private static void Postfix(ZNetPeer netPeer)
            {
                Listen(ZNet.instance, netPeer);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RemovePeer))]
        private static class Removed
        {
            private static void Postfix(ZNetPeer netPeer)
            {
                Safely(() => DeltaPeers.Left(netPeer));
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ShutDown))]
        private static class Closed
        {
            private static void Postfix()
            {
                Safely(Forget);
            }
        }

        private static void Listen(ZNet net, ZNetPeer peer)
        {
            if (net != null && net.IsServer())
            {
                Safely(() => DeltaPeers.Listen(peer));
            }
        }

        private static void Forget()
        {
            DeltaPeers.Clear();
            DeltaCache.Clear();
            History.Clear();
        }

        private static void Safely(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"delta bookkeeping skipped: {e.Message}");
            }
        }
    }
}
