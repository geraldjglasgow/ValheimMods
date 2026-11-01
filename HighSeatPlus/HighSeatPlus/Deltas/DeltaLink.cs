using System;
using HarmonyLib;

namespace HighSeatPlus
{
    /// <summary>
    /// Where this game starts and ends the delta protocol (<see cref="DeltaReceiver"/>). A connection to a server
    /// opening (ZNet.OnNewConnection on a player's game, where the game registers its first calls and sends its own
    /// handshake): the delta packages are heard and the hello sent, before the server has even accepted this player, so
    /// a HighSeat server sends the very first world data as deltas and every object comes through the book of held
    /// copies (<see cref="HeldCopies"/>). The hello must go after the game's own "ServerHandshake" (which the game sends
    /// at the end of OnNewConnection, before this postfix): the server's answer to the handshake clears the player's
    /// state (ZNet.RPC_ServerHandshake, ClearPlayerData, ZDOMan.RemovePeer) and would forget a hello heard before it.
    /// The game hosting a world, or a dedicated server, is never a client: nothing.
    /// An object destroyed (ZDOMan.HandleDestroyedZDO, the end of every destroy) leaves the book; the world closing
    /// (ZDOMan.ShutDown) empties it. None of these is hot (a destroy costs one lookup in the book), and each catches its
    /// own failures so the game's own code always runs on.
    /// </summary>
    internal static class DeltaLink
    {
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class Opened
        {
            private static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                try
                {
                    if (!__instance.IsServer() && peer != null && peer.m_server && peer.m_rpc != null)
                    {
                        HeldCopies.Clear();
                        DeltaClientStats.Reset();
                        DeltaReceiver.Listen(peer);
                    }
                }
                catch (Exception e)
                {
                    HighSeatPlus.Log.LogWarning($"smaller world updates are off for this connection: {e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO))]
        private static class Destroyed
        {
            private static void Postfix(ZDOID uid)
            {
                HeldCopies.Forget(uid);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ShutDown))]
        private static class Closed
        {
            private static void Postfix()
            {
                HeldCopies.Clear();
            }
        }
    }
}
