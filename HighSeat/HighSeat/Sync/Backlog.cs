using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A player who joins or arrives far away has their whole area pending: tens of thousands of objects in a large
    /// base. Looking through all of them on every round, as the send would, costs milliseconds a round for the whole
    /// drain. So once a pending set holds more than <see cref="BigSet"/>, it is checked and put in the game's send order
    /// once (<see cref="SendOrder.Key"/>) and set aside as ids, best first, the distant ring's after every near one (the
    /// game sends those only while fewer than 10 near ones wait, so they would only clog the feed); each round then moves
    /// only enough of it back into the pending set to keep <see cref="Feed"/> there (two rounds' worth at most), and the
    /// usual send works from those, so a round's cost no longer grows with the backlog. Objects gone or no longer wanted
    /// by the time they come back leave as usual, and changes arriving meanwhile go straight to the pending set. The order
    /// is worked out from where the player was then; a later jump that makes the set big again sorts what is left
    /// together with the new objects, from the new place. Setting the pending set aside lets go of its holds
    /// (<see cref="Holds"/>), so what comes back is looked at in full. While a backlog drains, the player's safety-net
    /// check waits (<see cref="FullCheck"/>): it would only put the backlog back into the pending set. One per thread that
    /// prepares sends (<see cref="SendPrep"/>, the sort buffers are its own); game state is only read, the objects by id
    /// straight from ZDOMan.m_objectsByID.
    /// </summary>
    internal sealed class Backlog
    {
        private const int BigSet = 4096;
        private const int Feed = 2 * SendOrder.Limit;
        private const double DistantAfter = 1e8;

        private double[] _keys = new double[BigSet];
        private ZDOID[] _ids = new ZDOID[BigSet];
        private int _count;

        /// <summary>Before a send: sets a big pending set aside in order, then feeds the pending set from what is aside.</summary>
        public void Prepare(ZDOMan man, PeerSync ps, Vector3 refPos, float time)
        {
            if (ps.Pending.Count > BigSet)
            {
                SetAside(man, ps, refPos, time);
            }
            FeedPending(ps);
        }

        /// <summary>Objects still set aside for this player.</summary>
        public static int Left(PeerSync ps)
        {
            return ps.Backlog.Count - ps.BacklogAt;
        }

        // The pending set and what is left aside, each object once, sorted into a new backlog; the pending set emptied.
        private void SetAside(ZDOMan man, PeerSync ps, Vector3 refPos, float time)
        {
            _count = 0;
            foreach (ZDOID id in ps.Pending)
            {
                AddKeyed(man, ps, id, refPos, time);
            }
            for (int i = ps.BacklogAt; i < ps.Backlog.Count; i++)
            {
                if (!ps.Pending.Contains(ps.Backlog[i]))
                {
                    AddKeyed(man, ps, ps.Backlog[i], refPos, time);
                }
            }
            Array.Sort(_keys, _ids, 0, _count);
            Store(ps);
        }

        private void Store(PeerSync ps)
        {
            ps.Backlog.Clear();
            for (int i = 0; i < _count; i++)
            {
                ps.Backlog.Add(_ids[i]);
            }
            ps.BacklogAt = 0;
            ps.Pending.Clear();
            ps.Pending.TrimExcess();
            ps.Holds.Clear();
        }

        // Keeps the object, with its place in the send order, if the player still wants it (as the send itself checks).
        private void AddKeyed(ZDOMan man, PeerSync ps, ZDOID id, Vector3 refPos, float time)
        {
            byte level = man.m_objectsByID.TryGetValue(id, out ZDO zdo) ? ps.LevelOf(zdo) : AreaShape.Out;
            if (level == AreaShape.Out)
            {
                return;
            }
            bool has = ps.Peer.m_zdos.TryGetValue(id, out ZDOMan.ZDOPeer.PeerZDOInfo info);
            if (has && !Newer(zdo, info))
            {
                return;
            }
            Grow();
            double key = SendOrder.Key(ps.Peer.m_peer.m_uid, zdo, refPos, time, has, info.m_syncTime);
            _keys[_count] = level == AreaShape.Distant ? key + DistantAfter : key;
            _ids[_count] = id;
            _count++;
        }

        // ZDOPeer.ShouldSend's rule for an object the player has: a newer owner revision or newer data.
        private static bool Newer(ZDO zdo, ZDOMan.ZDOPeer.PeerZDOInfo info)
        {
            return zdo.OwnerRevision > info.m_ownerRevision || zdo.DataRevision > info.m_dataRevision;
        }

        private void Grow()
        {
            if (_count == _keys.Length)
            {
                Array.Resize(ref _keys, _count * 2);
                Array.Resize(ref _ids, _count * 2);
            }
        }

        // Tops the pending set up from the front of what is aside; an emptied backlog lets go of its memory.
        private static void FeedPending(PeerSync ps)
        {
            List<ZDOID> backlog = ps.Backlog;
            while (ps.Pending.Count < Feed && ps.BacklogAt < backlog.Count)
            {
                ps.Pending.Add(backlog[ps.BacklogAt++]);
            }
            if (backlog.Count > 0 && ps.BacklogAt >= backlog.Count)
            {
                backlog.Clear();
                backlog.TrimExcess();
                ps.BacklogAt = 0;
            }
        }
    }
}
