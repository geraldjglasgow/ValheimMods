namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Server side: who sent a bench request, and whether they may change a player's shared blueprints - the player who
    /// shared them (the same character), an admin on the server's admin list, or the hosting player. The character is
    /// read from the server's own copy of the sender's player object, never from what the request says.
    /// </summary>
    public static class BenchRights
    {
        /// <summary>The sender's character id and name; 0 when the server does not know their character (yet).</summary>
        public static long Who(long sender, out string name)
        {
            if (sender == ZDOMan.GetSessionID())
            {
                Player local = Player.m_localPlayer;
                name = local != null ? local.GetPlayerName() : "";
                return local != null ? local.GetPlayerID() : 0L;
            }
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            name = peer?.m_playerName ?? "";
            ZDO character = peer == null || peer.m_characterID.IsNone() ? null : ZDOMan.instance.GetZDO(peer.m_characterID);
            return character != null ? character.GetLong(ZDOVars.s_playerID) : 0L;
        }

        public static bool IsAdmin(long sender)
        {
            if (sender == ZDOMan.GetSessionID())
                return true;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            return peer?.m_socket != null && ZNet.instance.ListContainsId(ZNet.instance.m_adminList, peer.m_socket.GetHostName());
        }

        public static bool MayChange(long sender, long owner) => owner != 0L && (Who(sender, out _) == owner || IsAdmin(sender));
    }
}
