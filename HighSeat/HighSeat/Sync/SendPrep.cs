using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The per-player part of a send, the game's ZDOMan.CreateSyncList with the area scan replaced by the player's pending
    /// set; one per thread that prepares sends (<see cref="PrepPool"/>), each with its own buffers, so several players are
    /// prepared at once. A big pending set is first put in order and set aside, then fed back a round's worth at a time
    /// (<see cref="Backlog"/>). Then each pending object: one the update rate holds (<see cref="Holds"/>) costs one lookup
    /// and stays, unless the crowd ranks now give it a faster rate that lets it go; any other is looked up again by id,
    /// and one that is gone, that the player no longer needs (ZDOPeer.ShouldSend's rule) or that is no longer in their area
    /// (<see cref="PeerSync.LevelOf"/>) leaves the set; one the update rate holds back now (<see cref="Throttle"/>, with
    /// the crowd ranks of this send, <see cref="CrowdRank"/>) stays and is held. The rest go into the slot's list in the
    /// game's order (<see cref="SendOrder"/>), the distant ring's only while fewer than <see cref="NearBeforeDistant"/>
    /// near ones are waiting, as the game does; the force-sent are put in front later, on the main thread
    /// (<see cref="PeerSend"/>). Those of them new to the player are counted (<see cref="SendSlot.Fresh"/>: whether the
    /// records their package needs are worth making ahead, <see cref="Ahead"/>). Game state is only read here (what, is listed on <see cref="PrepPool"/>), through fields
    /// and the game's own tables rather than methods another mod might patch, and no Unity API is called: the time is read
    /// on the main thread and handed in.
    /// </summary>
    internal sealed class SendPrep
    {
        private const int NearBeforeDistant = 10;

        private struct Candidate
        {
            public ZDO Zdo;
            public byte Level;
            public float Distance2;
            public bool Has;
            public ZDOMan.ZDOPeer.PeerZDOInfo Info;
        }

        private struct Held
        {
            public ZDOID Id;
            public Holds.Hold Hold;
        }

        private readonly List<ZDOID> _drops = new List<ZDOID>();
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private readonly List<Held> _held = new List<Held>();
        private readonly CrowdRank _crowd = new CrowdRank();
        private readonly SendOrder _near = new SendOrder();
        private readonly SendOrder _far = new SendOrder();
        private readonly Backlog _backlog = new Backlog();
        private int _live;

        #if DEBUG
        /// <summary>Pending objects passed over by a hold; the main thread reads and zeroes it between batches.</summary>
        public int HoldHits;
        #endif

        /// <summary>Fills the slot's list for its player.</summary>
        public void Run(ZDOMan man, SendSlot slot, float time)
        {
            PeerSync ps = slot.Peer;
            try
            {
                Vector3 refPos = ps.Peer.m_peer.m_refPos;
                _backlog.Prepare(man, ps, refPos, time);
                Collect(man, ps, refPos);
                slot.Fresh = Consider(ps, refPos, time);
                Order(slot.ToSend);
            }
            finally
            {
                Clear();
            }
        }

        /// <summary>Lets go of every ZDO reference held for a send.</summary>
        public void Clear()
        {
            _drops.Clear();
            _candidates.Clear();
            _held.Clear();
            _near.Clear();
            _far.Clear();
        }

        // Every pending object still wanted becomes a candidate (the rest leave the set) and the held ones stay as they
        // are; the crowd ranks among both, then the held ones the ranks now let go are looked at too. The set is walked as
        // it is; only the ids to drop are listed.
        private void Collect(ZDOMan man, PeerSync ps, Vector3 refPos)
        {
            _crowd.Clear();
            int round = Throttle.Round;
            foreach (ZDOID id in ps.Pending)
            {
                if (ps.Holds.Waiting(id, round, out Holds.Hold hold))
                {
                    _crowd.Add(hold.Distance2);
                    _held.Add(new Held { Id = id, Hold = hold });
                }
                else
                {
                    Look(man, ps, id, refPos, true);
                }
            }
            _crowd.Measure();
            Promote(man, ps, refPos, round);
            foreach (ZDOID id in _drops)
            {
                ps.Pending.Remove(id);
            }
        }

        // A held object whose rate the crowd ranks have made faster (another went further off or left) goes as soon as the
        // faster rate lets it, as without the hold: it is looked at in full now. The rest stay held.
        private void Promote(ZDOMan man, PeerSync ps, Vector3 refPos, int round)
        {
            _live = 0;
            foreach (Held held in _held)
            {
                int every = Throttle.Every(held.Hold.Distance2, _crowd);
                if (every < held.Hold.Every && held.Hold.Sent + every <= round)
                {
                    ps.Holds.Release(held.Id);
                    Look(man, ps, held.Id, refPos, false);
                }
                else
                {
                    _live++;
                }
            }
            #if DEBUG
            HoldHits += _live;
            #endif
        }

        // Looked up again: a candidate if still wanted, else to drop. Counted for the crowd ranks unless they are measured.
        private void Look(ZDOMan man, PeerSync ps, ZDOID id, Vector3 refPos, bool rank)
        {
            if (!man.m_objectsByID.TryGetValue(id, out ZDO zdo) || !Wanted(ps, zdo, refPos, rank))
            {
                _drops.Add(id);
            }
        }

        // In the player's area and news to them: ZDOPeer.ShouldSend's own rule, on the one lookup of what they have that
        // the rest needs too. One they have with newer data (not an owner change) is one the update rate may hold back.
        private bool Wanted(PeerSync ps, ZDO zdo, Vector3 refPos, bool rank)
        {
            byte level = ps.LevelOf(zdo);
            if (level == AreaShape.Out)
            {
                return false;
            }
            Candidate c = new Candidate { Zdo = zdo, Level = level };
            c.Has = ps.Peer.m_zdos.TryGetValue(zdo.m_uid, out c.Info);
            bool sameOwner = c.Has && zdo.OwnerRevision <= c.Info.m_ownerRevision;
            if (sameOwner && zdo.DataRevision <= c.Info.m_dataRevision)
            {
                return false;
            }
            c.Distance2 = (zdo.GetPosition() - refPos).sqrMagnitude;
            if (sameOwner && rank)
            {
                _crowd.Add(c.Distance2);
            }
            _candidates.Add(c);
            return true;
        }

        // A wanted object joins this round's send order unless the update rate holds it back (it then stays pending).
        // Returns how many of those that join it are new to the player (they hold no record of them).
        private int Consider(PeerSync ps, Vector3 refPos, float time)
        {
            long receiver = ps.Peer.m_peer.m_uid;
            int fresh = 0;
            foreach (Candidate c in _candidates)
            {
                if (c.Has && Waits(ps, c))
                {
                    continue;
                }
                fresh += c.Has ? 0 : 1;
                SendOrder into = c.Level == AreaShape.Near ? _near : _far;
                into.Add(c.Zdo, SendOrder.Key(receiver, c.Zdo, refPos, time, c.Has, c.Info.m_syncTime));
            }
            ps.Holds.Prune(_live);
            return fresh;
        }

        // Held back this round: it stays pending and is held until its round, unless it is force-sent too (it then goes
        // in front anyway, as with the game's own list, and is never held).
        private bool Waits(PeerSync ps, Candidate c)
        {
            if (!Throttle.Waits(c.Info, c.Zdo, c.Distance2, _crowd, out Holds.Hold hold))
            {
                return false;
            }
            HashSet<ZDOID> forced = ps.Peer.m_forceSend;
            if (forced.Count == 0 || !forced.Contains(c.Zdo.m_uid))
            {
                ps.Holds.Add(c.Zdo.m_uid, hold);
                _live++;
            }
            return true;
        }

        private void Order(List<ZDO> into)
        {
            int near = _near.Count;
            _near.TakeBest(into);
            if (near < NearBeforeDistant)
            {
                _far.TakeBest(into);
            }
        }
    }
}
