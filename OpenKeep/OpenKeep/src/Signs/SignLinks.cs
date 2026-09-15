namespace OpenKeep.Signs
{
    /// <summary>
    /// The ZDO keys that tie a sign to its container and the small reads and writes on them: the container carries
    /// <c>OpenKeep.sign</c> (its sign's id) and <c>OpenKeep.noSign</c> (the hammer opt-out), the sign carries
    /// <c>OpenKeep.signOf</c> (its container's id) and <c>OpenKeep.autoText</c> (what the mod last wrote).
    /// </summary>
    public static class SignLinks
    {
        public const string SignKey = "OpenKeep.sign";
        public const string SignOfKey = "OpenKeep.signOf";
        public const string AutoTextKey = "OpenKeep.autoText";
        public const string NoSignKey = "OpenKeep.noSign";

        public static ZDOID SignId(ZDO container) => container.GetZDOID(SignKey);

        /// <summary>The container's sign ZDO, or null when it has none or the linked ZDO no longer exists.</summary>
        public static ZDO LinkedSign(ZDO container)
        {
            ZDOID id = SignId(container);
            if (id == ZDOID.None || ZDOMan.instance == null)
                return null;
            return ZDOMan.instance.GetZDO(id);
        }

        public static ZDOID ContainerId(ZDO sign) => sign.GetZDOID(SignOfKey);

        /// <summary>A sign this mod placed: it names its container.</summary>
        public static bool IsAutomatic(ZDO sign) => sign != null && sign.IsValid() && ContainerId(sign) != ZDOID.None;

        public static void Link(ZDO container, ZDO sign)
        {
            sign.Set(SignOfKey, container.m_uid);
            container.Set(SignKey, sign.m_uid);
        }

        public static void Unlink(ZDO container)
        {
            if (SignId(container) != ZDOID.None)
                container.Set(SignKey, ZDOID.None);
        }

        public static bool NoSign(ZDO container) => container.GetBool(NoSignKey);

        public static void SetNoSign(ZDO container, bool value)
        {
            if (NoSign(container) != value)
                container.Set(NoSignKey, value);
        }

        /// <summary>The game's in-use flag of a container ZDO another client owns (an open chest is never claimed).</summary>
        public static bool InUseByAnother(ZDO container) => !container.IsOwner() && container.GetInt(ZDOVars.s_inUse) == 1;

        /// <summary>
        /// Claims a ZDO for the local client the way <c>ContainerScan.Claim</c> does through the net view: the owner is
        /// set to this session and the ZDO is force sent to the previous owner. Works for loaded and unloaded ZDOs.
        /// </summary>
        public static bool Claim(ZDO zdo)
        {
            if (zdo == null || !zdo.IsValid() || ZDOMan.instance == null)
                return false;
            if (zdo.IsOwner())
                return true;
            long previous = zdo.GetOwner();
            zdo.SetOwner(ZDOMan.GetSessionID());
            if (previous != 0L)
                ZDOMan.instance.ForceSendZDO(previous, zdo.m_uid);
            return zdo.IsOwner();
        }
    }
}
