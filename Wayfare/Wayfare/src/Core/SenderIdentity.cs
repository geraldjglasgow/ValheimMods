namespace Wayfare.Core
{
    /// <summary>Resolves the <c>sender</c> peer uid a routed RPC handler receives to a persistent player id and an
    /// admin flag. <c>ZNet.GetPeer</c> has no entry for the local machine itself (only for other connected peers), so
    /// a sender equal to this machine's own routed id is the local player. Any other unknown sender resolves to no
    /// player (0): on a client, every machine but the server is unknown, so an owner-side check there fails closed
    /// instead of crediting the local player with someone else's request.</summary>
    public static class SenderIdentity
    {
        public static long PlayerId(long sender)
        {
            ZNetPeer peer = ZNet.instance != null ? ZNet.instance.GetPeer(sender) : null;
            if (peer != null)
                return peer.m_playerID;
            bool self = ZRoutedRpc.instance != null && sender == ZRoutedRpc.instance.m_id;
            return self && Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        }

        /// <summary>True when a routed RPC came from the server: the server peer's uid on a client, this
        /// machine's own routed id where it is the server itself (a locally routed call). Handlers for
        /// server-only replies check this so another client cannot forge a grant or a portal list.</summary>
        public static bool IsFromServer(long sender)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
                return false;
            if (ZNet.instance.IsServer())
                return sender == ZRoutedRpc.instance.m_id;
            ZNetPeer server = ZNet.instance.GetServerPeer();
            return server != null && sender == server.m_uid;
        }

        public static bool IsAdmin(long sender)
        {
            if (ZNet.instance == null)
                return false;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer != null)
                return ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            bool self = ZRoutedRpc.instance != null && sender == ZRoutedRpc.instance.m_id;
            return self && ZNet.instance.LocalPlayerIsAdminOrHost();
        }
    }
}
