using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A routed RPC the server sends to everybody (ZRoutedRpc.RouteRPC with no target peer: chat, effects, damage
    /// numbers, ZDOMan.SendDestroyed's list of destroyed objects, a player's broadcast passed on, many mods' RPCs) written
    /// once for every player. The game writes the routed message into a new package, then for each ready player but the
    /// sender calls ZRpc.Invoke("RoutedRPC", package), which copies the whole message into a new array (ZRpc.Serialize,
    /// ZPackage.Write) and then into the player's own RPC package, and the socket copies it once more: with 100 players
    /// every broadcast is 200 copies. Here the "RoutedRPC" RPC is written once, laid out as ZRpc.Invoke writes it (the
    /// method's hash, its name too while the game debugs RPCs, then the routed message as its length and its bytes, the
    /// message written by the game's own RoutedRPCData.Serialize), and handed to each player's socket in the game's order,
    /// with ZRpc.Invoke's connection check and ZRpc.SendPackage's counters (m_sentPackages, m_sentData). A player whose
    /// socket is not a Steam one (crossplay: ZPlayFabSocket's Send writes into the package it is given) gets the game's own
    /// ZRpc.Invoke with the game's own routed package, made the game's way when first needed. A message to one player, a
    /// client's sends, and a broadcast started while one is going out stay the game's. Before each player the package's
    /// length is checked: if a send changed it, that player and the rest get the game's way and this stays off for the
    /// session. If writing the package throws, the game's own RouteRPC runs (nothing was sent yet); in the loop every call
    /// that can throw is one the game's own loop makes at the same point (IsReady, IsConnected, the socket's Send).
    /// </summary>
    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RouteRPC))]
    internal static class RouteBroadcast
    {
        private const string Method = "RoutedRPC";
        private static readonly int Hash = Method.GetStableHashCode();
        private static readonly ZPackage Pkg = new ZPackage();
        private static ZPackage _routed;
        private static bool _busy;
        private static int _size;

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZRoutedRpc __instance, ZRoutedRpc.RoutedRPCData rpcData, bool __runOriginal)
        {
            if (!__runOriginal || _busy || !ServerBroadcast(__instance, rpcData) || !Write(rpcData))
            {
                return true;
            }
            _busy = true;
            try
            {
                SendAll(__instance, rpcData);
            }
            finally
            {
                _busy = false;
                _routed = null;
            }
            return false;
        }

        // RouteRPC's broadcast branch (the server, no target peer: the game tests m_targetPeerID against 0), while on.
        private static bool ServerBroadcast(ZRoutedRpc router, ZRoutedRpc.RoutedRPCData data) =>
            data != null && router.m_server && data.m_targetPeerID == 0L && NetGate.Broadcast;

        // The RPC written once. False (logged, off for the session) leaves the whole call to the game: nothing was sent.
        private static bool Write(ZRoutedRpc.RoutedRPCData data)
        {
            try
            {
                int lengthAt = Head();
                data.Serialize(Pkg);
                _size = Pkg.Size();
                Pkg.m_stream.Position = lengthAt;
                Pkg.m_writer.Write(_size - lengthAt - sizeof(int));
                Pkg.m_writer.Flush();
                Pkg.m_stream.Position = _size;
                return true;
            }
            catch (Exception e)
            {
                NetGate.Fail(NetGate.Feature.Broadcast, e);
                return false;
            }
        }

        // ZRpc.Invoke's head (the method's hash, its name while the game debugs RPCs), then room for the message's
        // length, as ZPackage.Write(ZPackage) puts it first. Returns where the length goes.
        private static int Head()
        {
            Pkg.Clear();
            Pkg.Write(Hash);
            if (ZRpc.m_DEBUG)
            {
                Pkg.Write(Method);
            }
            int lengthAt = Pkg.Size();
            Pkg.Write(0);
            return lengthAt;
        }

        // The game's loop: every ready player but the sender, in the list's order.
        private static void SendAll(ZRoutedRpc router, ZRoutedRpc.RoutedRPCData data)
        {
            foreach (ZNetPeer peer in router.m_peers)
            {
                if (data.m_senderPeerID != peer.m_uid && peer.IsReady())
                {
                    SendTo(peer.m_rpc, data);
                }
            }
        }

        // ZRpc.Invoke and SendPackage with the package written once; the game's own Invoke when that cannot be used.
        private static void SendTo(ZRpc rpc, ZRoutedRpc.RoutedRPCData data)
        {
            if (!NetGate.Broadcast || !SteamDirect.Is(rpc.m_socket) || !Intact())
            {
                rpc.Invoke(Method, Routed(data));
                return;
            }
            if (rpc.IsConnected())
            {
                rpc.m_sentPackages++;
                rpc.m_sentData += _size;
                rpc.m_socket.Send(Pkg);
            }
        }

        // The game's own routed package, made as RouteRPC makes it, once a call.
        private static ZPackage Routed(ZRoutedRpc.RoutedRPCData data)
        {
            if (_routed == null)
            {
                _routed = new ZPackage();
                data.Serialize(_routed);
            }
            return _routed;
        }

        // Nothing was written into the package by a send: still exactly what was written for this call.
        private static bool Intact()
        {
            if (Pkg.m_stream.Length == _size)
            {
                return true;
            }
            string what = "a socket's Send wrote into the broadcast package";
            NetGate.Fail(NetGate.Feature.Broadcast, new InvalidOperationException(what));
            return false;
        }
    }
}
