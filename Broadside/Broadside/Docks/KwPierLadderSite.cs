using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the ladder tool would hang a dock ladder (<see cref="KwPierLadderAim"/>): its top on the edge at the deck's
    /// top, the level way out over the water, its length down to its foot, the material it is made of (the dock's), the
    /// dock section it hangs from (null on a game floor), and why it may not be built there (null when it may). Plain data.
    /// </summary>
    public sealed class KwPierLadderSite
    {
        public Vector3 Top { get; set; }
        public Vector3 Outward { get; set; }
        public float Length { get; set; }
        public KwPierKind Kind { get; set; } = KwPierKinds.Wood;
        public KwPier? Pier { get; set; }
        public bool OnDock { get; set; }

        /// <summary>Down to the sea floor (shallower than the water's depth under it) rather than into the water.</summary>
        public bool Grounded { get; set; }

        public string? Why { get; set; }

        public Quaternion Turn => Quaternion.LookRotation(Outward);

        /// <summary>Its saved shape with a seed.</summary>
        public KwPierLadderSpec Spec(int seed) => new KwPierLadderSpec(Length, seed, OnDock);

        public static KwPierLadderSite Refused(string why) => new KwPierLadderSite { Why = why };
    }
}
