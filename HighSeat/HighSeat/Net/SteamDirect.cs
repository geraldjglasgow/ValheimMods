using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;

namespace HighSeat
{
    /// <summary>
    /// The server's packages handed to Steam without the socket's managed copy. The game's ZSteamSocket.Send copies every
    /// package into a new array (ZPackage.GetArray), queues it and sends the queue (SendQueuedPackages): each array is
    /// copied into native memory, handed to Steam as one reliable message (flags 8) and the memory freed; on k_EResultOK
    /// its length goes into m_totalSent (the socket's stats) and it leaves the queue, otherwise "Failed to send data" is
    /// logged and it stays queued, sent again by ZSteamSocket.Update every frame. Here, while that queue is empty, the
    /// package's own bytes (its stream's buffer, what GetArray would copy) are copied into native memory and handed to
    /// Steam the same way, through the interface the game's own send loop calls (<see cref="SendChecks.GameServerApi"/>:
    /// the game-server one on a dedicated server, the client one on a hosting player's game), and on k_EResultOK counted
    /// in m_totalSent: the queue ends as it was, empty, and nothing else in the socket changes. Whenever something waits
    /// in the queue (this one must go after it), Steam does not take the message (its buffer is full), the package is
    /// empty or the connection is gone, the game's own Send runs for it, so the order and the retries stay the game's
    /// (after a refusal it asks Steam once more at once, a try the game would not have made: the message is refused
    /// again and queued, or taken, still in order). Every package the server sends through Steam passes here:
    /// HighSeat's own ZDO packages, the game's RPCs, chat and broadcasts. Server only (<see cref="NetGate.Direct"/>);
    /// crossplay players have a ZPlayFabSocket, untouched. If anything here throws, the game's own Send runs for that
    /// package (Steam had not taken it: a send throws only before the message reaches Steam) and this stays off for the
    /// session.
    /// </summary>
    [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Send))]
    internal static class SteamDirect
    {
        private const int Reliable = 8;

        /// <summary>A Steam socket as the game makes it, the only kind sent through here.</summary>
        public static bool Is(ISocket socket) => socket != null && socket.GetType() == typeof(ZSteamSocket);

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZSteamSocket __instance, ZPackage pkg, bool __runOriginal)
        {
            if (!__runOriginal || pkg == null || !NetGate.Direct)
            {
                return true;
            }
            try
            {
                return !Sent(__instance, pkg);
            }
            catch (Exception e)
            {
                NetGate.Fail(NetGate.Feature.Direct, e);
                return true;
            }
        }

        // True when Steam took the package; false leaves it to the game's own Send.
        private static bool Sent(ZSteamSocket socket, ZPackage pkg)
        {
            if (pkg.Size() == 0 || !socket.IsConnected() || socket.m_sendQueue.Count > 0)
            {
                return false;
            }
            if (!pkg.m_stream.TryGetBuffer(out ArraySegment<byte> bytes) || Hand(socket.m_con, bytes) != EResult.k_EResultOK)
            {
                return false;
            }
            socket.m_totalSent += bytes.Count;
            return true;
        }

        // SendQueuedPackages' own steps for one message: a native copy, Steam, the copy freed.
        private static EResult Hand(HSteamNetConnection connection, ArraySegment<byte> bytes)
        {
            IntPtr data = Marshal.AllocHGlobal(bytes.Count);
            try
            {
                Marshal.Copy(bytes.Array, bytes.Offset, data, bytes.Count);
                uint size = (uint)bytes.Count;
                return SendChecks.GameServerApi
                    ? SteamGameServerNetworkingSockets.SendMessageToConnection(connection, data, size, Reliable, out _)
                    : SteamNetworkingSockets.SendMessageToConnection(connection, data, size, Reliable, out _);
            }
            finally
            {
                Marshal.FreeHGlobal(data);
            }
        }
    }
}
