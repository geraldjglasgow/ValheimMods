using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// One player's turn in a round, the game's ZDOMan.SendZDOs and CreateSyncList with the area scan replaced by the
    /// player's pending set. A player with nothing waiting, force-sent or to drop costs one test. Otherwise their byte
    /// budget is this round's share of HighSeat's send rate (<see cref="SendRate"/>: 500 KB/s is
    /// <see cref="RoundBytes"/> bytes a round; crossplay players keep the game's own <see cref="CrossplayBytes"/>, as
    /// HighSeat leaves their PlayFab rate alone) less what still waits in their socket, and as in the game a player with
    /// less than <see cref="MinBytes"/> of it left is passed over this round. A big pending set is first put in order and
    /// set aside, then fed back a round's worth at a time (<see cref="Backlog"/>). Each pending object is looked up again
    /// by id: one that is gone, that the player no longer needs (ZDOPeer.ShouldSend) or that is no longer in their area
    /// (<see cref="PeerSync.LevelOf"/>) leaves the set; one the update rate holds back (<see cref="Throttle"/>) stays for
    /// a later round. The rest go in the game's order (<see cref="SendOrder"/>), the distant ring's only while fewer than
    /// <see cref="NearBeforeDistant"/> near ones are waiting, as the game does, and the force-sent in front (the game's
    /// own ZDOMan.AddForceSendZdos: Wayfare sends far areas through it). What does not fit stays pending.
    /// </summary>
    internal static class PeerSend
    {
        private const int RoundBytes = 500 * 1024 / 20;
        private const int CrossplayBytes = 10240;
        private const int MinBytes = 2048;
        private const int NearBeforeDistant = 10;

        private static readonly List<ZDOID> Ids = new List<ZDOID>();
        private static readonly List<ZDO> ToSend = new List<ZDO>();
        private static readonly SendOrder Near = new SendOrder();
        private static readonly SendOrder Far = new SendOrder();

        public static void Serve(ZDOMan man, PeerSync ps)
        {
            if (!ps.HasWork)
            {
                return;
            }
            ZDOMan.ZDOPeer peer = ps.Peer;
            int budget = Budget(peer.m_peer.m_socket);
            if (budget < MinBytes)
            {
                SyncStats.Skipped();
                return;
            }
            try
            {
                Backlog.Prepare(man, ps);
                Collect(man, ps);
                Order(man, peer);
                Package.Send(man, ps, ToSend, budget);
            }
            finally
            {
                Clear();
            }
        }

        /// <summary>Lets go of every ZDO reference held for a send.</summary>
        public static void Clear()
        {
            Ids.Clear();
            ToSend.Clear();
            Near.Clear();
            Far.Clear();
        }

        // Below MinBytes (or under 0) when the socket's queue leaves too little, as ZDOMan.SendZDOs checks it.
        private static int Budget(ISocket socket)
        {
            int full = socket is ZPlayFabSocket ? CrossplayBytes : RoundBytes;
            return full - socket.GetSendQueueSize();
        }

        private static void Collect(ZDOMan man, PeerSync ps)
        {
            foreach (ZDOID id in ps.Pending)
            {
                Ids.Add(id);
            }
            Vector3 refPos = ps.Peer.m_peer.GetRefPos();
            float time = Time.time;
            foreach (ZDOID id in Ids)
            {
                ZDO zdo = man.GetZDO(id);
                byte level = zdo == null ? AreaShape.Out : ps.LevelOf(zdo);
                if (level == AreaShape.Out || !ps.Peer.ShouldSend(zdo))
                {
                    ps.Pending.Remove(id);
                    continue;
                }
                Consider(ps.Peer, zdo, level == AreaShape.Near ? Near : Far, refPos, time);
            }
        }

        // A wanted object joins this round's candidates unless the update rate holds it back (it then stays pending).
        private static void Consider(ZDOMan.ZDOPeer peer, ZDO zdo, SendOrder into, Vector3 refPos, float time)
        {
            bool has = peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo info);
            if (has && Throttle.Waits(info, zdo, refPos))
            {
                return;
            }
            into.Add(zdo, SendOrder.Key(peer.m_peer.m_uid, zdo, refPos, time, has, info.m_syncTime));
        }

        private static void Order(ZDOMan man, ZDOMan.ZDOPeer peer)
        {
            int near = Near.Count;
            Near.TakeBest(ToSend);
            if (near < NearBeforeDistant)
            {
                Far.TakeBest(ToSend);
            }
            man.AddForceSendZdos(peer, ToSend);
        }
    }
}
