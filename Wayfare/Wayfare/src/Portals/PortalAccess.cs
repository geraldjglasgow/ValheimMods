namespace Wayfare.Portals
{
    /// <summary>The one access rule, shared by the client's map display filter and the server's teleport grant, so
    /// the two can never disagree about who may target a portal.</summary>
    public static class PortalAccess
    {
        public static bool MayTarget(ZDO portalZdo, long playerId, bool isAdmin, bool unownedIsPublic)
        {
            return MayTarget(PortalFields.GetMode(portalZdo, unownedIsPublic), PortalFields.GetOwner(portalZdo), playerId, isAdmin);
        }

        /// <summary>Same rule, from an already-taken snapshot (<see cref="PortalInfo"/>) rather than a live ZDO -
        /// used by the map display, which redraws from the registry's 5-second snapshot rather than re-reading
        /// every portal's ZDO every frame.</summary>
        public static bool MayTarget(PortalMode mode, long owner, long playerId, bool isAdmin)
        {
            if (isAdmin)
                return true;
            switch (mode)
            {
                case PortalMode.Public:
                    return true;
                case PortalMode.Private:
                    return owner == playerId;
                default: // Admin: only the isAdmin check above may pass
                    return false;
            }
        }

        /// <summary>Whether the acting player may cycle this portal's mode. An unowned portal may be claimed by
        /// anyone; an owned one only by its owner or an admin - otherwise ownership would mean nothing.</summary>
        public static bool MayCycle(ZDO portalZdo, long playerId, bool isAdmin)
        {
            if (!PortalFields.HasOwner(portalZdo))
                return true;
            return isAdmin || PortalFields.GetOwner(portalZdo) == playerId;
        }
    }
}
