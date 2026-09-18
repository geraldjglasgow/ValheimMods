using System.Collections.Generic;

namespace Party.Server
{
    /// <summary>One member's record inside a party. Kept even while offline, per spec.</summary>
    public sealed class PartyMember
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        /// <summary>UTC, <see cref="PartyStore.TimeFormat"/>. Updated whenever the player is seen online.</summary>
        public string LastSeen { get; set; } = "";
    }

    /// <summary>
    /// One party: a leader and its roster. <see cref="Members"/> is kept in join order, oldest first, so leadership
    /// succession ("whoever has been in the party longest and is currently online") only needs to walk it in order.
    /// </summary>
    public sealed class PartyRecord
    {
        public string PartyId { get; set; } = "";
        public long LeaderId { get; set; }
        public List<PartyMember> Members { get; set; } = new List<PartyMember>();

        public PartyMember Find(long id) => Members.Find(m => m.Id == id);

        public bool Contains(long id) => Find(id) != null;
    }

    public sealed class PartyFile
    {
        public List<PartyRecord> Parties { get; set; } = new List<PartyRecord>();
    }
}
