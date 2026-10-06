using System;
using System.Collections.Generic;
using Party.Client;
using Party.Server;

namespace Party.Api
{
    /// <summary>Public API. Full accuracy on the server; a bare client only knows its own party.</summary>
    public static class PartyApi
    {
        /// <summary>A party this player belongs to changed in some way.</summary>
        public static event Action<long> PartyChanged;
        /// <summary>(anyExistingMemberId, joinedPlayerId).</summary>
        public static event Action<long, long> MemberJoined;
        /// <summary>(anyRemainingMemberId, leftPlayerId).</summary>
        public static event Action<long, long> MemberLeft;
        /// <summary>(anyMemberId, newLeaderId).</summary>
        public static event Action<long, long> LeaderChanged;

        // The bool and leader questions are answered without building a member list: other mods may ask per hit.
        public static bool IsInParty(long playerId) => Identity.IsServer
            ? PartyManager.FindPartyOf(playerId) != null
            : PartyClientState.InParty && PartyClientState.Find(playerId) != null;

        public static bool AreInSameParty(long playerIdA, long playerIdB)
        {
            if (Identity.IsServer)
            {
                PartyRecord party = PartyManager.FindPartyOf(playerIdA);
                return party != null && party.Contains(playerIdB);
            }
            return PartyClientState.InParty && PartyClientState.Find(playerIdA) != null && PartyClientState.Find(playerIdB) != null;
        }

        public static long? GetLeader(long playerId)
        {
            long leaderId = LeaderOf(playerId);
            return leaderId == 0 ? (long?)null : leaderId;
        }

        /// <summary>The leader of the player's party, or 0 when they have none (as far as this peer knows).</summary>
        private static long LeaderOf(long playerId)
        {
            if (Identity.IsServer)
                return PartyManager.FindPartyOf(playerId)?.LeaderId ?? 0;
            return PartyClientState.InParty && PartyClientState.Find(playerId) != null ? PartyClientState.LeaderId : 0;
        }

        public static IReadOnlyList<PartyMemberInfo> GetMembers(long playerId)
        {
            List<PartyMemberInfo> members = FindMembers(playerId, out _);
            return members != null ? (IReadOnlyList<PartyMemberInfo>)members : Array.Empty<PartyMemberInfo>();
        }

        private static List<PartyMemberInfo> FindMembers(long playerId, out long leaderId)
        {
            leaderId = 0;
            if (Identity.IsServer)
                return FromServer(playerId, out leaderId);
            return FromClient(playerId, out leaderId);
        }

        private static List<PartyMemberInfo> FromServer(long playerId, out long leaderId)
        {
            leaderId = 0;
            PartyRecord party = PartyManager.FindPartyOf(playerId);
            if (party == null)
                return null;
            leaderId = party.LeaderId;
            List<PartyMemberInfo> result = new List<PartyMemberInfo>();
            foreach (PartyMember member in party.Members)
            {
                result.Add(new PartyMemberInfo
                {
                    Id = member.Id,
                    Name = member.Name,
                    Online = Identity.TryFind(member.Id, out _),
                    IsLeader = member.Id == party.LeaderId,
                });
            }
            return result;
        }

        private static List<PartyMemberInfo> FromClient(long playerId, out long leaderId)
        {
            leaderId = 0;
            if (!PartyClientState.InParty || PartyClientState.Find(playerId) == null)
                return null;
            leaderId = PartyClientState.LeaderId;
            List<PartyMemberInfo> result = new List<PartyMemberInfo>();
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                result.Add(new PartyMemberInfo
                {
                    Id = member.Id,
                    Name = member.Name,
                    Online = member.Online,
                    IsLeader = member.IsLeader,
                });
            }
            return result;
        }

        internal static void RaiseChanged(long playerId) => PartyChanged?.Invoke(playerId);
        internal static void RaiseJoined(long anchorId, long joinedId) => MemberJoined?.Invoke(anchorId, joinedId);
        internal static void RaiseLeft(long anchorId, long leftId) => MemberLeft?.Invoke(anchorId, leftId);
        internal static void RaiseLeaderChanged(long anchorId, long newLeaderId) => LeaderChanged?.Invoke(anchorId, newLeaderId);
    }
}
