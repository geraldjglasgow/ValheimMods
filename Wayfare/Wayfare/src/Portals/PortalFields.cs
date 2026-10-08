namespace Wayfare.Portals
{
    public enum PortalMode
    {
        Public = 0,
        Private = 1,
        Admin = 2
    }

    /// <summary>The two Wayfare-owned fields on a portal's own ZDO: access mode and owner. Absent (a portal no
    /// Wayfare player has ever touched) reads as unowned and Public: every portal is built Public (the user's rule, 2026-10-05).</summary>
    public static class PortalFields
    {
        // As the hashes the game stores them under: read for every portal on every list the server builds and on every
        // hover frame.
        public static readonly int ModeKey = "wf_mode".GetStableHashCode();
        public static readonly int OwnerKey = "wf_owner".GetStableHashCode();
        private const int NoMode = -1;

        public static bool HasOwner(ZDO zdo) => GetOwner(zdo) != 0L;

        public static PortalMode GetMode(ZDO zdo)
        {
            int raw = zdo.GetInt(ModeKey, NoMode);
            return raw < 0 ? PortalMode.Public : (PortalMode)raw;
        }

        public static long GetOwner(ZDO zdo) => zdo.GetLong(OwnerKey, 0L);

        /// <summary>The portal has a tag, its name; in the TargetTeleport mode only named portals are connected. Read
        /// straight from the ZDO: the game's own <c>GetText</c> runs the word filter, too dear for every frame.</summary>
        public static bool HasName(ZDO zdo) => HasName(zdo.GetString(ZDOVars.s_tag));

        public static bool HasName(string tag) => !string.IsNullOrWhiteSpace(tag);

        public static void SetModeAndOwner(ZDO zdo, PortalMode mode, long ownerPlayerId)
        {
            zdo.Set(ModeKey, (int)mode);
            zdo.Set(OwnerKey, ownerPlayerId);
        }
    }
}
