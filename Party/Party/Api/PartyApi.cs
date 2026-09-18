using System;
using System.Collections.Generic;
using Party.Client;
using Party.Server;

namespace Party.Api
{
    /// <summary>
    /// The public surface other mods build on. Accurate for any player on the side that has full knowledge - a
    /// dedicated server or the host, via <see cref="PartyManager"/> - and, on a bare client, accurate only for the
    /// local player's own party, since the server never tells a client about anyone else's (see PLAN.md). A mod
    /// without a compile-time reference to Party never sees this type at all and should treat every player as
    /// party-less; see the README for the soft-dependency pattern.
    /// </summary>
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

        public static bool IsInParty(long playerId) => FindMembers(playerId, out _) != null;

        public static bool AreInSameParty(long playerIdA, long playerIdB)
        {
            List<PartyMemberInfo> members = FindMembers(playerIdA, out _);
            return members != null && members.Exists(m => m.Id == playerIdB);
        }

        public static long? GetLeader(long playerId)
        {
            FindMembers(playerId, out long leaderId);
            return leaderId == 0 ? (long?)null : leaderId;
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
