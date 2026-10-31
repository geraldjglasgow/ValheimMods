using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A player leaving (ZDOMan.RemovePeer, from ZNet.Disconnect) drops their send loop state at once: their place in
    /// every sector of <see cref="Interest"/> and their pending set, and a round under way passes over them. The game has
    /// already taken them off its own list. Never throws into the game's disconnect: a failure here hands sending back to
    /// the game's own loop (<see cref="SyncLoop.Fail"/>).
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RemovePeer))]
    internal static class PeerLeft
    {
        private static void Postfix(ZNetPeer netPeer)
        {
            try
            {
                SyncPeers.Left(netPeer);
            }
            catch (Exception e)
            {
                SyncLoop.Fail(e);
            }
        }
    }
}
