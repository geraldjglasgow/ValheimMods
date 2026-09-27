namespace EarthWright.Core
{
    /// <summary>Which machine this is and who may do what. Admin checks of other peers only work on the server.</summary>
    public static class Side
    {
        public static bool HasNetwork => ZNet.instance != null;

        /// <summary>The dedicated server, the host of a listen server, or single player.</summary>
        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        public static bool IsDedicated => ZNet.instance != null && ZNet.instance.IsDedicated();

        /// <summary>This player is on the server's admin list or is the host / single player.</summary>
        public static bool LocalIsAdmin
        {
            get
            {
                if (ZNet.instance == null)
                    return true;
                if (ZNet.instance.IsServer())
                    return true;
                return Plugin.Synced != null && Plugin.Synced.IsAdmin;
            }
        }

        /// <summary>Server only: the peer with this uid is on the admin list (or is the server itself).</summary>
        public static bool PeerIsAdmin(long peerUid)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return false;
            if (peerUid == ZNet.GetUID())
                return true;
            ZNetPeer peer = ZNet.instance.GetPeer(peerUid);
            string host = peer?.m_socket?.GetHostName();
            return !string.IsNullOrEmpty(host) && ZNet.instance.IsAdmin(host);
        }

        /// <summary>The routed-RPC sender is the server (a relayed edit), or this machine is the server and sent it itself.</summary>
        public static bool IsServerPeer(long sender)
        {
            if (ZNet.instance == null)
                return true;
            if (ZNet.instance.IsServer())
                return sender == ZNet.GetUID();
            ZNetPeer server = ZNet.instance.GetServerPeer();
            return server != null && server.m_uid == sender;
        }
    }
}
