using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// One object's record in a "ZDOData" package, byte for byte as ZDOMan.SendZDOs writes it (id, owner revision, data
    /// revision, owner, position, then the object's data as a nested package: its length, then ZDO.Serialize's bytes),
    /// made once and copied into every player's package for up to <see cref="KeepSeconds"/> s: in a crowd a round runs
    /// over many frames with a few players a frame, and making the record again for each player (ZDO.Serialize walks every
    /// value the object has) was most of a send's cost with 100 players. A kept record is used only while the object's
    /// owner is the one it names and only for a player it is news to (newer than what they have, by the game's own rule,
    /// ZDOPeer.ShouldSend); otherwise it is made again from the object as it is now and kept in its place. The player is
    /// then booked as having the revisions the record carries (<see cref="Package"/>), not the object's latest, so a
    /// change made after the record goes to them on a later turn. Every record goes after <see cref="KeepSeconds"/>, all
    /// at once (the buffer is emptied), so none is older than that; nothing is allocated per object. A record to make is
    /// taken, in this order: the one a send thread made ahead for this batch (<see cref="Premade"/>, while it still
    /// carries the object's revisions and owner), else one written here by <see cref="RecordWriter"/> (both only while
    /// <see cref="RecordChecks"/> lets the writer stand in for the game's code, and checked against it now and then), else
    /// the game's own ZDO.Serialize, as before; the bytes are the same either way. While any player reads deltas
    /// (HighSeatPlus), every record made is also kept for longer in <see cref="History"/>, the bytes a later delta starts
    /// from, and the deltas worked out from these records (<see cref="DeltaCache"/>) go with them. The kept records are
    /// written only on the main thread, outside a batch's preparation, so the send threads may read them then
    /// (<see cref="Kept"/>).
    /// </summary>
    internal static class Records
    {
        private const float KeepSeconds = 0.25f;

        public struct Span
        {
            public int Start;
            public int Length;
            public uint DataRevision;
            public ushort OwnerRevision;
            public long Owner;
        }

        private static readonly ZPackage Made = new ZPackage();
        private static readonly ZPackage Data = new ZPackage();
        private static readonly Dictionary<ZDOID, Span> Where = new Dictionary<ZDOID, Span>();
        private static readonly RecordWriter Writer = new RecordWriter();
        private static readonly RecordBuffer Own = new RecordBuffer();
        private static float _emptied;

        /// <summary>The buffer the records are in, at the Start of their spans (read it after the record is got).</summary>
        public static byte[] Bytes => Made.m_stream.GetBuffer();

        /// <summary>At the start of a frame's sending: every record goes once they are <see cref="KeepSeconds"/> old.</summary>
        public static void NewFrame()
        {
            #if DEBUG
            DeltaStats.Frame();
            #endif
            float now = Time.unscaledTime;
            RecordChecks.Frame(now);
            if (now - _emptied < KeepSeconds)
            {
                return;
            }
            _emptied = now;
            if (Where.Count > 0)
            {
                Where.Clear();
                Made.Clear();
                DeltaCache.Clear();
            }
        }

        /// <summary>Appends a record of the object that is news to the player, and returns the revisions it carries.</summary>
        public static Span Write(ZPackage pkg, ZDO zdo, ZDOMan.ZDOPeer peer)
        {
            Span span = Get(zdo, peer);
            pkg.m_writer.Write(Made.m_stream.GetBuffer(), span.Start, span.Length);
            return span;
        }

        /// <summary>The record of the object that is news to the player: the kept one when it serves, else made now.</summary>
        public static Span Get(ZDO zdo, ZDOMan.ZDOPeer peer)
        {
            bool has = peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo held);
            return Get(zdo, has, held);
        }

        /// <summary><see cref="Get"/> for a player whose books for the object (<paramref name="held"/>) were looked up already.</summary>
        public static Span Get(ZDO zdo, bool has, ZDOMan.ZDOPeer.PeerZDOInfo held)
        {
            if (!Where.TryGetValue(zdo.m_uid, out Span span) || !Usable(span, zdo, Owner(zdo), has, held))
            {
                span = Make(zdo);
            }
            return span;
        }

        /// <summary>
        /// True when a kept record serves the player as <see cref="Get"/> would find it; <paramref name="span"/> is the kept
        /// record whether it serves or not (a Length of 0 when none). For the send threads while the main thread waits for
        /// a batch: they only read the kept records, the object (its owner from the game's table, as RecordWriter reads
        /// it) and the player's books then.
        /// </summary>
        public static bool Kept(ZDO zdo, ZDOMan.ZDOPeer peer, out Span span)
        {
            if (!Where.TryGetValue(zdo.m_uid, out span))
            {
                return false;
            }
            bool has = peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo held);
            return Usable(span, zdo, RecordWriter.OwnerOf(zdo), has, held);
        }

        // The owner it names is still the owner, and the player lacks it or has an older revision of it.
        private static bool Usable(Span span, ZDO zdo, long owner, bool has, ZDOMan.ZDOPeer.PeerZDOInfo held)
        {
            if (span.OwnerRevision != zdo.OwnerRevision || span.Owner != owner)
            {
                return false;
            }
            return !has || span.OwnerRevision > held.m_ownerRevision || span.DataRevision > held.m_dataRevision;
        }

        // ZDO.GetOwner; while the record writer is on (no patch on it) the same table read in one lookup instead of two.
        private static long Owner(ZDO zdo) => RecordChecks.On ? RecordWriter.OwnerOf(zdo) : zdo.GetOwner();

        private static Span Make(ZDO zdo)
        {
            int start = Made.Size();
            Append(zdo);
            Span span = new Span
            {
                Start = start, Length = Made.Size() - start, DataRevision = zdo.DataRevision,
                OwnerRevision = zdo.OwnerRevision, Owner = Owner(zdo),
            };
            Where[zdo.m_uid] = span;
            if (DeltaPeers.Any)
            {
                History.Keep(zdo.m_uid, Made.m_stream.GetBuffer(), start, span.Length, span.DataRevision, span.OwnerRevision);
            }
            return span;
        }

        // The record's bytes onto the buffer: made ahead, else written here, else by the game's own code.
        private static void Append(ZDO zdo)
        {
            bool ahead = RecordChecks.On && Ahead(zdo);
            bool game = !ahead && !(RecordChecks.On && Written(zdo));
            if (game)
            {
                Serialize(zdo);
            }
            #if DEBUG
            SyncStats.Record(ahead, game);
            #endif
        }

        // This batch's record made ahead on a send thread, while it carries the object's revisions and owner.
        private static bool Ahead(ZDO zdo)
        {
            int at = Premade.Find(zdo, 0L, 0);
            if (at < 0)
            {
                return false;
            }
            byte[] bytes = Premade.BytesOf(at);
            int start = Premade.StartOf(at);
            int length = Premade.LengthOf(at);
            if (!Current(zdo, bytes, start) || !RecordChecks.Agrees(zdo, bytes, start, length))
            {
                return false;
            }
            Premade.Note(at, Made.Size());
            Made.m_writer.Write(bytes, start, length);
            return true;
        }

        private static bool Current(ZDO zdo, byte[] bytes, int start)
        {
            return RecordBytes.U16(bytes, start + DeltaFormat.OwnerRevisionAt) == zdo.OwnerRevision
                && RecordBytes.U32(bytes, start + DeltaFormat.DataRevisionAt) == zdo.DataRevision
                && RecordBytes.I64(bytes, start + DeltaFormat.OwnerAt) == RecordWriter.OwnerOf(zdo);
        }

        // Written here; a writer that throws (a game update that changed what it reads) is off for the session.
        private static bool Written(ZDO zdo)
        {
            Own.Clear();
            try
            {
                if (!Writer.Write(Own, zdo) || !RecordChecks.Agrees(zdo, Own.Bytes, 0, Own.Size))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                RecordChecks.Broke(e);
                return false;
            }
            Made.m_writer.Write(Own.Bytes, 0, Own.Size);
            return true;
        }

        // The game's way: the header, then ZDO.Serialize's bytes behind their length.
        private static void Serialize(ZDO zdo)
        {
            Made.Write(zdo.m_uid);
            Made.Write(zdo.OwnerRevision);
            Made.Write(zdo.DataRevision);
            Made.Write(zdo.GetOwner());
            Made.Write(zdo.GetPosition());
            Data.Clear();
            zdo.Serialize(Data);
            int length = Data.Size();
            Made.m_writer.Write(length);
            Made.m_writer.Write(Data.m_stream.GetBuffer(), 0, length);
        }
    }
}
