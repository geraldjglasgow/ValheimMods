using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The players the send loop serves: one <see cref="PeerSync"/> for each peer in the game's own list
    /// (ZDOMan.m_peers, which ZDOMan.AddPeer fills once a player is accepted), in the order they joined. At the start of
    /// each round that list is read again: a new peer gets its state, and a peer no longer on the list loses its state
    /// (ZDOMan.RemovePeer normally dropped it already, <see cref="PeerLeft"/>). Every peer's area is then brought up to
    /// date over the round's first frames (<see cref="RoundStart"/>): a new peer's whole area is checked as it is first
    /// registered in <see cref="Interest"/> (it has nothing yet, so everything there is pending).
    /// </summary>
    internal static class SyncPeers
    {
        public static readonly List<PeerSync> All = new List<PeerSync>();

        private static readonly Dictionary<ZDOMan.ZDOPeer, PeerSync> ByPeer = new Dictionary<ZDOMan.ZDOPeer, PeerSync>();
        private static int _stamp;

        public static void Refresh(ZDOMan man)
        {
            _stamp++;
            foreach (ZDOMan.ZDOPeer peer in man.m_peers)
            {
                if (!ByPeer.TryGetValue(peer, out PeerSync ps))
                {
                    ps = Add(peer);
                }
                ps.Seen = _stamp;
            }
            DropUnseen();
        }

        /// <summary>The game removed this player (they left or were disconnected).</summary>
        public static void Left(ZNetPeer net)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                if (All[i].Peer.m_peer == net)
                {
                    Drop(All[i]);
                }
            }
        }

        /// <summary>Lets go of every player (a new session, or the loop stopped); a round under way passes over them.</summary>
        public static void Reset()
        {
            foreach (PeerSync ps in All)
            {
                ps.Gone = true;
                ps.HasArea = false;
                ps.Forget();
            }
            All.Clear();
            ByPeer.Clear();
        }

        private static PeerSync Add(ZDOMan.ZDOPeer peer)
        {
            PeerSync ps = new PeerSync(peer) { LastFullCheck = Time.time };
            ByPeer.Add(peer, ps);
            All.Add(ps);
            return ps;
        }

        private static void DropUnseen()
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                if (All[i].Seen != _stamp)
                {
                    Drop(All[i]);
                }
            }
        }

        private static void Drop(PeerSync ps)
        {
            ps.Gone = true;
            Interest.Remove(ps);
            ps.Forget();
            ByPeer.Remove(ps.Peer);
            All.Remove(ps);
        }
    }
}
