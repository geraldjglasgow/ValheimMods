namespace Wayfare.Core
{
    /// <summary>Resolves the <c>sender</c> peer uid a routed RPC handler receives to a persistent player id and an
    /// admin flag. <c>ZNet.GetPeer</c> has no entry for the local machine itself (only for other connected peers),
    /// so a sender that resolves to no peer is the local player - the only way a routed RPC target-self call
    /// reaches a handler without going through the peer list at all.</summary>
    public static class SenderIdentity
    {
        public static long PlayerId(long sender)
        {
            ZNetPeer peer = ZNet.instance != null ? ZNet.instance.GetPeer(sender) : null;
            if (peer != null)
                return peer.m_playerID;
            return Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        }

        public static bool IsAdmin(long sender)
        {
            if (ZNet.instance == null)
                return false;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer != null)
                return ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            return ZNet.instance.LocalPlayerIsAdminOrHost();
        }
    }
}
