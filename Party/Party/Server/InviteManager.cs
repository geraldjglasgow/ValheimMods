using System;
using System.Collections.Generic;

namespace Party.Server
{
    public sealed class PendingInvite
    {
        public long InviterId;
        public string InviterName;
        public long TargetId;
        public string TargetName;
        public DateTime ExpiresAtUtc;
    }

    /// <summary>
    /// Pending invites, keyed by target player so a target has at most one at a time (a second invite to an
    /// already-pending target is refused to the new inviter rather than silently replacing the first - see
    /// <see cref="InviteFlow"/> - so nobody's invite is ever cancelled out from under them without being told).
    /// </summary>
    public static class InviteManager
    {
        private static readonly Dictionary<long, PendingInvite> pending = new Dictionary<long, PendingInvite>();

        public static bool HasPending(long targetId) => pending.ContainsKey(targetId);

        public static void Add(PendingInvite invite) => pending[invite.TargetId] = invite;

        public static bool TryTake(long targetId, out PendingInvite invite) => pending.Remove(targetId, out invite);

        public static bool TryGet(long targetId, out PendingInvite invite) => pending.TryGetValue(targetId, out invite);

        /// <summary>Called once a second from <see cref="PartyTicker"/>. Expires invites nobody answered in time.</summary>
        public static void Tick()
        {
            if (!Identity.IsServer || pending.Count == 0)
                return;
            List<long> expired = FindExpired();
            foreach (long targetId in expired)
                InviteFlow.Expire(targetId);
        }

        private static List<long> FindExpired()
        {
            DateTime now = DateTime.UtcNow;
            List<long> expired = new List<long>();
            foreach (KeyValuePair<long, PendingInvite> entry in pending)
            {
                if (entry.Value.ExpiresAtUtc <= now)
                    expired.Add(entry.Key);
            }
            return expired;
        }
    }

    internal static class DictionaryExtensions
    {
        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
        {
            if (dict.TryGetValue(key, out value))
            {
                dict.Remove(key);
                return true;
            }
            value = default;
            return false;
        }
    }
}
