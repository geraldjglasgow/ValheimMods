namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Server side: whether the sender of a request may take a site down - the hosting player (who is the server), an
    /// admin on the server's admin list, or the player whose id the site records as its creator.
    /// </summary>
    public static class SiteRights
    {
        public static bool MayTakeDown(long sender, ZDO site)
        {
            if (sender == ZDOMan.GetSessionID())
                return true;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer == null)
                return false;
            if (peer.m_socket != null && ZNet.instance.ListContainsId(ZNet.instance.m_adminList, peer.m_socket.GetHostName()))
                return true;
            ZDO character = peer.m_characterID.IsNone() ? null : ZDOMan.instance.GetZDO(peer.m_characterID);
            long creator = site.GetLong(SiteState.CreatorKey);
            return character != null && creator != 0L && character.GetLong(ZDOVars.s_playerID) == creator;
        }
    }
}
