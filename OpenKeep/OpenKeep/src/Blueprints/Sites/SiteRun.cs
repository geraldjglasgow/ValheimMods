using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// This machine's working state for one loaded construction site, kept on its <see cref="SiteMarker"/> and never
    /// networked (everything that must last is in the ZDO): when the builder looks at the site next and since when this
    /// machine owns it, the ground plan it worked out last, and the cached list of what the site is still missing.
    /// </summary>
    internal sealed class SiteRun
    {
        /// <summary>The builder's next step, and since when this machine owns the site (-1: it does not), Time.time.</summary>
        public float NextStep;
        public float OwnedSince = -1f;

        /// <summary>
        /// While steps change nothing (a site waiting for materials): the wait before the next look, doubling up to
        /// <see cref="SiteBuilder"/>'s limit (0: not idle), and the ZDO revision the last idle step saw; a new revision
        /// (a delivery) wakes the site at once.
        /// </summary>
        public float IdleDelay;
        public uint IdleRevision;

        /// <summary>The ground work planned last on this machine (the owner's), and when.</summary>
        public GroundWork Ground;
        public float GroundAt;

        /// <summary>The cached <see cref="SiteNeeds.Missing(SiteMarker)"/>, for the ZDO revision and settings it was worked out for.</summary>
        public Dictionary<string, int> Missing;
        public uint MissingRevision;
        public bool MissingFree;
        public float MissingAt;
    }
}
