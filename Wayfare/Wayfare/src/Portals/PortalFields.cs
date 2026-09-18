namespace Wayfare.Portals
{
    public enum PortalMode
    {
        Public = 0,
        Private = 1,
        Admin = 2
    }

    /// <summary>The two Wayfare-owned fields on a portal's own ZDO: access mode and owner. Absent (a portal no
    /// Wayfare player has ever touched) reads as unowned, mode decided by <see cref="Core.WayfareConfig.UnownedPortalsArePublic"/>.</summary>
    public static class PortalFields
    {
        public const string ModeKey = "wf_mode";
        public const string OwnerKey = "wf_owner";
        private const int NoMode = -1;

        public static bool HasOwner(ZDO zdo) => GetOwner(zdo) != 0L;

        public static PortalMode GetMode(ZDO zdo, bool unownedIsPublic)
        {
            int raw = zdo.GetInt(ModeKey, NoMode);
            if (raw < 0)
                return unownedIsPublic ? PortalMode.Public : PortalMode.Private;
            return (PortalMode)raw;
        }

        public static long GetOwner(ZDO zdo) => zdo.GetLong(OwnerKey, 0L);

        public static void SetModeAndOwner(ZDO zdo, PortalMode mode, long ownerPlayerId)
        {
            zdo.Set(ModeKey, (int)mode);
            zdo.Set(OwnerKey, ownerPlayerId);
        }
    }
}
