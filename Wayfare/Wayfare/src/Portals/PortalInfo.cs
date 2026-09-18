using UnityEngine;

namespace Wayfare.Portals
{
    /// <summary>A snapshot of one portal's ZDO, taken at a <see cref="PortalRegistry"/> refresh. Display only -
    /// never used to decide whether a teleport is granted, which always re-reads the live ZDO.</summary>
    public readonly struct PortalInfo
    {
        public readonly ZDOID Id;
        public readonly Vector3 Position;
        public readonly string Tag;
        public readonly PortalMode Mode;
        public readonly long Owner;

        public PortalInfo(ZDOID id, Vector3 position, string tag, PortalMode mode, long owner)
        {
            Id = id;
            Position = position;
            Tag = tag;
            Mode = mode;
            Owner = owner;
        }
    }
}
