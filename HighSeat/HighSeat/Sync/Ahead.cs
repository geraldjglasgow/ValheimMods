using System;
using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// One send thread's making ahead (<see cref="Premade"/>). Right after the thread has prepared a player's send list
    /// (<see cref="SendPrep"/>), when that list holds at least <see cref="MinFresh"/> objects new to the player or
    /// force-sent ones (a player who just joined, or arrived somewhere far: what they get is mostly not shared with
    /// anyone, and records for it are most of their package's cost), it walks that player's package as
    /// <see cref="Package"/> will write it: the drops, then the force-sent objects the game will put in front
    /// (ZDOMan.AddForceSendZdos: those the player still lacks the latest of, the last of its set first), then the list,
    /// each object once, while the bytes stay within the player's budget (checked before each object, as the package
    /// does). For each object it finds the record that will go as <see cref="Records.Get"/> will: a kept one that serves
    /// the player (<see cref="Records.Kept"/>), else the batch's, made here with <see cref="RecordWriter"/> into this
    /// thread's own buffer, once a batch whoever needs it. For a player who reads deltas and holds an earlier record of
    /// the object, it also finds the delta to that record as <see cref="DeltaWriter"/> will: the main thread's cached
    /// one, else made here from the held record in the <see cref="History"/>, once a batch for each held revision and
    /// record. The main thread then copies what was made in place of making it (<see cref="Records"/>,
    /// <see cref="DeltaWriter"/>): the object, the kept records and the history are the same then, as no game code runs
    /// between the batch's preparation and its packages. Each object counts at what will go: the record, or the delta
    /// (smaller, so a player reading deltas gets far more objects into their budget); one another thread is still making
    /// counts at the kept record's length or <see cref="GuessBytes"/>, and a delta still being made at
    /// <see cref="DeltaGuessBytes"/>. It reads what the preparation reads (listed on <see cref="PrepPool"/>), what a
    /// record is made of (<see cref="RecordWriter"/>), the kept records, the delta cache and the history; it never writes
    /// game state, and never throws: a failure is handed to <see cref="Premade.Broke"/>. A player who only gets updates
    /// of what they hold (a crowd's steady state) is not walked: the records and deltas they need are the crowd's, a few
    /// hundred a round made once each, and finding out which those are cost the send threads about as much as making them
    /// on the main thread (offline, 100 players in a crowd: 3.92 ms a round with the record writer alone, 3.96 ms when
    /// every player was walked). One per thread, with its own buffers.
    /// </summary>
    internal sealed class Ahead
    {
        private const int MinFresh = 16;
        private const int GuessBytes = 100;
        private const int DeltaGuessBytes = 80;

        private readonly RecordWriter _writer = new RecordWriter();
        private readonly DeltaRecord _deltas = new DeltaRecord();
        private readonly HashSet<ZDOID> _forced = new HashSet<ZDOID>();
        private readonly List<ZDO> _front = new List<ZDO>();
        private readonly int _lane;

        /// <summary>This thread's records and deltas, read by the main thread through <see cref="Premade"/>.</summary>
        public readonly RecordBuffer Buffer = new RecordBuffer();

        public Ahead(int lane)
        {
            _lane = lane;
        }

        /// <summary>Makes what the slot's package will need and nothing made serves, when worth it; never throws.</summary>
        public void Walk(ZDOMan man, SendSlot slot)
        {
            try
            {
                ZDOMan.ZDOPeer peer = slot.Peer.Peer;
                if (slot.Fresh + peer.m_forceSend.Count < MinFresh)
                {
                    return;
                }
                int size = (slot.Deltas ? 1 : 0) + 4 + 12 * peer.m_invalidSector.Count;
                Listed(slot, peer, Front(man, slot, peer, size));
            }
            catch (Exception e)
            {
                Premade.Broke(e);
            }
            finally
            {
                _forced.Clear();
                _front.Clear();
            }
        }

        // The force-sent objects the player still lacks the latest of, in the order the game will put them in front.
        private int Front(ZDOMan man, SendSlot slot, ZDOMan.ZDOPeer peer, int size)
        {
            if (peer.m_forceSend.Count == 0)
            {
                return size;
            }
            foreach (ZDOID id in peer.m_forceSend)
            {
                if (id != ZDOID.None && man.m_objectsByID.TryGetValue(id, out ZDO zdo) && News(peer, zdo))
                {
                    _front.Add(zdo);
                }
            }
            for (int i = _front.Count - 1; i >= 0 && size <= slot.Budget; i--)
            {
                if (_forced.Add(_front[i].m_uid))
                {
                    size += Need(_front[i], peer, slot.Deltas);
                }
            }
            return size;
        }

        // The list after the force-sent, each object once, while the budget lasts.
        private void Listed(SendSlot slot, ZDOMan.ZDOPeer peer, int size)
        {
            List<ZDO> list = slot.ToSend;
            for (int i = 0; i < list.Count && size <= slot.Budget; i++)
            {
                if (_forced.Count == 0 || !_forced.Contains(list[i].m_uid))
                {
                    size += Need(list[i], peer, slot.Deltas);
                }
            }
        }

        // ZDOPeer.ShouldSend's rule, on the player's books.
        private static bool News(ZDOMan.ZDOPeer peer, ZDO zdo)
        {
            if (!peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo info))
            {
                return true;
            }
            return zdo.OwnerRevision > info.m_ownerRevision || zdo.DataRevision > info.m_dataRevision;
        }

        // The bytes the object will take in the package: the delta when the player reads deltas and one goes, else the
        // record with its kind byte in the delta format.
        private int Need(ZDO zdo, ZDOMan.ZDOPeer peer, bool deltas)
        {
            int kind = deltas ? 1 : 0;
            if (!Record(zdo, peer, out byte[] bytes, out Records.Span span, out int of))
            {
                return (span.Length > 0 ? span.Length : GuessBytes) + kind;
            }
            if (!deltas || !peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo held))
            {
                return span.Length + kind;
            }
            int delta = DeltaOf(zdo, bytes, span, of, held);
            if (delta < 0)
            {
                return Math.Min(DeltaGuessBytes, span.Length + 1);
            }
            return delta > 0 ? delta : span.Length + 1;
        }

        // The record that will go: a kept one that serves (named by its place), else the batch's own (named
        // BatchRecord), made here unless another thread has it. False while not known: being made elsewhere, or not made.
        private bool Record(ZDO zdo, ZDOMan.ZDOPeer peer, out byte[] bytes, out Records.Span span, out int of)
        {
            bytes = Records.Bytes;
            of = Premade.BatchRecord;
            if (Records.Kept(zdo, peer, out span))
            {
                of = span.Start;
                return true;
            }
            int at = Premade.Claim(zdo, 0L, 0, out bool mine);
            if (at >= 0 && mine)
            {
                Make(at, zdo);
            }
            if (at < 0 || Premade.LengthIfMade(at) < 0)
            {
                return false;
            }
            bytes = Premade.BytesOf(at);
            span = Header(bytes, Premade.StartOf(at), Premade.LengthOf(at));
            return true;
        }

        // The record into this thread's buffer, published; failed when the writer gave up (the main thread makes it).
        private void Make(int at, ZDO zdo)
        {
            int start = Buffer.Size;
            if (_writer.Write(Buffer, zdo))
            {
                Premade.Publish(at, _lane, start, Buffer.Size - start, -1);
                return;
            }
            Premade.Fail(at);
        }

        // A record's span read from its own header: the revisions and the owner it carries.
        private static Records.Span Header(byte[] bytes, int start, int length)
        {
            return new Records.Span
            {
                Start = start, Length = length,
                DataRevision = RecordBytes.U32(bytes, start + DeltaFormat.DataRevisionAt),
                OwnerRevision = RecordBytes.U16(bytes, start + DeltaFormat.OwnerRevisionAt),
                Owner = RecordBytes.I64(bytes, start + DeltaFormat.OwnerAt),
            };
        }

        // The delta from the held revisions to this record: the main thread's cached one for a kept record, else made
        // here unless another thread has it. Its length; 0 when the whole record goes; -1 while not known.
        private int DeltaOf(ZDO zdo, byte[] bytes, Records.Span span, int of, ZDOMan.ZDOPeer.PeerZDOInfo held)
        {
            uint data = held.m_dataRevision;
            ushort owner = held.m_ownerRevision;
            if (of >= 0 && DeltaCache.Find(zdo.m_uid, data, owner, of, out int _, out int cached))
            {
                return Math.Max(cached, 0);
            }
            int at = Premade.Claim(zdo, Premade.DeltaTag(data, owner), of, out bool mine);
            if (at >= 0 && mine)
            {
                MakeDelta(at, zdo, bytes, span, data, owner);
            }
            return at < 0 ? -1 : Premade.LengthIfMade(at);
        }

        // The delta into this thread's buffer, published with the place of the held record it starts from (the main
        // thread takes it only from that same place); a length of 0 when the whole record goes.
        private void MakeDelta(int at, ZDO zdo, byte[] bytes, Records.Span span, uint data, ushort owner)
        {
            if (!DeltaWriter.Steps(span, data, owner) || !History.Find(zdo.m_uid, data, owner, Premade.Now, out int oldAt, out int _))
            {
                Premade.Publish(at, _lane, 0, 0, -1);
                return;
            }
            int start = Buffer.Size;
            int length = _deltas.Append(Buffer, History.Bytes, oldAt, bytes, span, data, owner);
            Premade.Publish(at, _lane, start, Math.Max(length, 0), oldAt);
        }
    }
}
