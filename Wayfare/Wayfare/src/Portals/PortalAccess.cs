namespace Wayfare.Portals
{
    /// <summary>The one access rule, shared by the client's map display filter and the server's teleport grant, so
    /// the two can never disagree about who may target a portal.</summary>
    public static class PortalAccess
    {
        public static bool MayTarget(ZDO portalZdo, long playerId, bool isAdmin)
        {
            return MayTarget(PortalFields.GetMode(portalZdo), PortalFields.GetOwner(portalZdo), playerId, isAdmin);
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

        /// <summary>The mode Shift+E moves to: Public, Private, Admin for an admin; Public and Private for anyone else,
        /// whose press on an Admin portal they own makes it Public.</summary>
        public static PortalMode NextMode(PortalMode mode, bool isAdmin)
        {
            if (isAdmin)
                return (PortalMode)(((int)mode + 1) % 3);
            return mode == PortalMode.Public ? PortalMode.Private : PortalMode.Public;
        }

        /// <summary>Whether the acting player may set this mode: only an admin may set Admin.</summary>
        public static bool MaySet(long modeRaw, bool isAdmin)
        {
            if (modeRaw < (long)PortalMode.Public || modeRaw > (long)PortalMode.Admin)
                return false;
            return isAdmin || modeRaw != (long)PortalMode.Admin;
        }

        /// <summary>Whether the acting player may cycle this portal's mode. An unowned portal may be claimed by
        /// anyone; an owned one only by its owner or an admin - otherwise ownership would mean nothing. A Private
        /// portal only by its owner, the player who made it private (the user's rule, 2026-10-05): not even an admin.</summary>
        public static bool MayCycle(ZDO portalZdo, long playerId, bool isAdmin)
        {
            if (!PortalFields.HasOwner(portalZdo) || PortalFields.GetOwner(portalZdo) == playerId)
                return true;
            return isAdmin && PortalFields.GetMode(portalZdo) != PortalMode.Private;
        }
    }
}
