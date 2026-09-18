using System;
using System.Linq;
using Party.Api;

namespace Party.Server
{
    /// <summary>
    /// Server side roster operations: create, join, leave, remove, promote, dissolve. Every method is a no-op
    /// unless this instance is the server (dedicated, or the hosting player) - see <see cref="Identity.IsServer"/>.
    /// Publishing the result to clients is <see cref="PartyPublisher"/>.
    /// </summary>
    public static class PartyManager
    {
        private static PartyStore store;

        public static PartyStore EnsureStore()
        {
            if (store != null)
                return store;
            store = new PartyStore(ZNet.instance.GetWorldName());
            if (!store.Load())
                PartyPlugin.Log.LogInfo($"Created party file {store.FilePath}");
            store.Changed += () => PartyPublisher.PublishAll(store.Data);
            store.Watch();
            return store;
        }

        public static void Shutdown()
        {
            store?.Dispose();
            store = null;
        }

        public static PartyRecord FindPartyOf(long playerId) =>
            EnsureStore().Data.Parties.FirstOrDefault(p => p.Contains(playerId));

        public static bool IsFull(PartyRecord party) => party.Members.Count >= PartyConfig.MaxPartySize.Value;

        public static PartyRecord CreateParty(long leaderId, string leaderName)
        {
            PartyRecord party = new PartyRecord
            {
                PartyId = Guid.NewGuid().ToString("N"),
                LeaderId = leaderId,
            };
            party.Members.Add(new PartyMember { Id = leaderId, Name = leaderName, LastSeen = PartyStore.Now });
            EnsureStore().Data.Parties.Add(party);
            return party;
        }

        public static void AddMember(PartyRecord party, long id, string name)
        {
            party.Members.Add(new PartyMember { Id = id, Name = name, LastSeen = PartyStore.Now });
            PartyApi.RaiseJoined(party.LeaderId, id);
        }

        /// <summary>Removes a member. Dissolves an emptied party, or hands leadership on per PLAN.md if the leader left.</summary>
        public static void RemoveMember(PartyRecord party, long id)
        {
            bool wasLeader = party.LeaderId == id;
            party.Members.RemoveAll(m => m.Id == id);
            if (party.Members.Count == 0)
            {
                EnsureStore().Data.Parties.Remove(party);
                PartyApi.RaiseLeft(id, id);
                return;
            }
            PartyApi.RaiseLeft(party.Members[0].Id, id);
            if (wasLeader)
            {
                party.LeaderId = NextLeader(party);
                PartyApi.RaiseLeaderChanged(party.Members[0].Id, party.LeaderId);
            }
        }

        /// <summary>Longest-tenured online member (join order is preserve order in <see cref="PartyRecord.Members"/>);
        /// if nobody is online, the oldest member keeps the party from being leaderless.</summary>
        private static long NextLeader(PartyRecord party)
        {
            foreach (PartyMember member in party.Members)
            {
                if (Identity.TryFind(member.Id, out _))
                    return member.Id;
            }
            return party.Members[0].Id;
        }

        public static void PromoteLeader(PartyRecord party, long newLeaderId)
        {
            party.LeaderId = newLeaderId;
            PartyApi.RaiseLeaderChanged(newLeaderId, newLeaderId);
        }

        public static void Touch(PartyRecord party, long id, string name)
        {
            PartyMember member = party.Find(id);
            if (member == null)
                return;
            if (!string.IsNullOrEmpty(name))
                member.Name = name;
            member.LastSeen = PartyStore.Now;
        }

        public static void SaveAndPublish(PartyRecord party)
        {
            EnsureStore().Save();
            PartyPublisher.Publish(party);
        }
    }
}
