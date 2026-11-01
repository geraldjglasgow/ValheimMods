using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Who played here lately, for the player history sent in part (<see cref="HistoryOut"/>): when each player was last
    /// online this session (since the world started), noted at every player list send (<see cref="HistoryTick"/>, every
    /// 2 s). <see cref="Candidates"/> gives history places to send in full after the players online: the players seen
    /// this session, the latest first, then the newest arrivals in the history (the end of the list, where first-time
    /// players are added), for a server that has just started. A player who left a minute ago comes before one who left
    /// an hour ago. Nothing is saved: a restart starts from the newest arrivals again. Main thread, server only.
    /// </summary>
    internal static class SeenPlayers
    {
        private struct Seen
        {
            public int Place;
            public float At;
        }

        // The latest first; the later place (the newer arrival) first on a tie.
        private sealed class Latest : IComparer<Seen>
        {
            public int Compare(Seen a, Seen b) => a.At != b.At ? b.At.CompareTo(a.At) : b.Place.CompareTo(a.Place);
        }

        private static readonly Dictionary<PlatformUserID, float> LastSeen = new Dictionary<PlatformUserID, float>(IdKeys.Comparer);
        private static readonly Latest ByLatest = new Latest();
        private static Seen[] _order = new Seen[64];

        public static void Reset() => LastSeen.Clear();

        public static void Touch(List<ZNet.PlayerInfo> players)
        {
            float now = Time.unscaledTime;
            foreach (ZNet.PlayerInfo player in players)
            {
                LastSeen[player.m_userInfo.m_id] = now;
            }
        }

        /// <summary>
        /// History places in the order to send them in full: everyone seen this session, latest first, then the
        /// <paramref name="newest"/> newest arrivals (some may repeat; the caller skips places it has).
        /// </summary>
        public static void Candidates(List<ZNet.CrossNetworkUserInfo> history, List<int> into, int newest)
        {
            into.Clear();
            int seen = Order();
            for (int i = 0; i < seen; i++)
            {
                into.Add(_order[i].Place);
            }
            for (int place = history.Count - 1; place >= 0 && into.Count < seen + newest; place--)
            {
                into.Add(place);
            }
        }

        // The players seen this session with a place in the history, sorted latest first into _order; their count.
        private static int Order()
        {
            if (_order.Length < LastSeen.Count)
            {
                _order = new Seen[Math.Max(LastSeen.Count, _order.Length * 2)];
            }
            int count = 0;
            foreach (KeyValuePair<PlatformUserID, float> seen in LastSeen)
            {
                int place = HistoryIndex.IndexOf(seen.Key);
                if (place >= 0)
                {
                    _order[count++] = new Seen { Place = place, At = seen.Value };
                }
            }
            Array.Sort(_order, 0, count, ByLatest);
            return count;
        }
    }
}
