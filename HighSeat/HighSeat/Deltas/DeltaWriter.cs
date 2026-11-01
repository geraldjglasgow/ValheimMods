using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// One object in the package of a player who reads deltas (<see cref="Package"/>): the record that goes is the one
    /// <see cref="Records"/> would send them (kept or made now), and the player is booked for its revisions exactly as
    /// for the game's format. It goes as a delta when the player holds an earlier record of it that the server sent and
    /// still keeps (<see cref="History"/>, found by the revisions ZDOPeer.m_zdos books them at) and the delta is smaller;
    /// otherwise as the whole record behind its kind byte. A player who holds nothing of it, holds revisions newer than
    /// the record in either count, or whose revisions the server never sent (their own update, received by
    /// ZDOMan.RPC_ZDOData) gets the whole record; in the last case the history may still hold other bytes at those
    /// revisions, and the player's game sees that its copy did not come from the server and asks for the whole object
    /// (HighSeatPlus). Each delta is worked out once per record and held revisions (<see cref="DeltaCache"/>): taken from
    /// a send thread that made it ahead for this batch (<see cref="Ahead"/>, <see cref="Premade"/>) when it was made for
    /// the same record and from the same held record in the history (so its bytes are the ones worked out here), else
    /// worked out here.
    /// </summary>
    internal static class DeltaWriter
    {
        private const int NotAhead = -2;

        private static readonly Stopwatch Clock = new Stopwatch();
        private static readonly DeltaRecord Maker = new DeltaRecord();

        /// <summary>Appends the object as a delta or whole, and returns the revisions the record carries.</summary>
        public static Records.Span Write(ZPackage pkg, ZDO zdo, ZDOMan.ZDOPeer peer)
        {
            bool has = peer.m_zdos.TryGetValue(zdo.m_uid, out ZDOMan.ZDOPeer.PeerZDOInfo held);
            Records.Span span = Records.Get(zdo, has, held);
            if (!has || !Delta(pkg, zdo, span, held))
            {
                pkg.m_writer.Write(DeltaFormat.Full);
                pkg.m_writer.Write(Records.Bytes, span.Start, span.Length);
                DeltaStats.Whole(span.Length + 1);
            }
            return span;
        }

        /// <summary>
        /// Whether a delta can lead from the held revisions to the record's: neither count is behind, and the data step
        /// fits the delta's 7-bit number. Any thread.
        /// </summary>
        public static bool Steps(Records.Span span, uint data, ushort owner)
        {
            return span.DataRevision >= data && span.OwnerRevision >= owner && span.DataRevision - data <= int.MaxValue;
        }

        // The delta for a player holding these revisions, from the cache or worked out now; false when the whole record goes.
        private static bool Delta(ZPackage pkg, ZDO zdo, Records.Span span, ZDOMan.ZDOPeer.PeerZDOInfo held)
        {
            uint data = held.m_dataRevision;
            ushort owner = held.m_ownerRevision;
            if (!DeltaCache.Find(zdo.m_uid, data, owner, span.Start, out int at, out int length))
            {
                at = DeltaCache.Buffer.Size;
                length = Make(zdo, span, data, owner);
                DeltaCache.Store(zdo.m_uid, data, owner, span.Start, at, length);
            }
            if (length < 0)
            {
                return false;
            }
            pkg.m_writer.Write(DeltaCache.Bytes, at, length);
            DeltaStats.Delta(length, span.Length + 1);
            return true;
        }

        // Appends the delta to the cache's buffer; its length, or -1 when the whole record goes.
        private static int Make(ZDO zdo, Records.Span span, uint data, ushort owner)
        {
            if (!Steps(span, data, owner))
            {
                return -1;
            }
            if (!History.Find(zdo.m_uid, data, owner, out int oldAt, out int _))
            {
                DeltaStats.NoBase();
                return -1;
            }
            int ahead = Ahead(zdo, span, data, owner, oldAt);
            if (ahead != NotAhead)
            {
                return ahead;
            }
            Clock.Restart();
            int length = Maker.Append(DeltaCache.Buffer, History.Bytes, oldAt, Records.Bytes, span, data, owner);
            DeltaStats.Diffed(Clock.ElapsedTicks);
            return length;
        }

        // The delta a send thread made ahead from the same held record to the same record, copied into the cache's
        // buffer: its length, -1 when it found the whole record no larger, NotAhead when there is none to take.
        private static int Ahead(ZDO zdo, Records.Span span, uint data, ushort owner, int oldAt)
        {
            int at = Premade.Find(zdo, Premade.DeltaTag(data, owner), Premade.RecordOf(zdo, span.Start));
            if (at < 0 || Premade.ExtraOf(at) != oldAt)
            {
                return NotAhead;
            }
            int length = Premade.LengthOf(at);
            if (length > 0)
            {
                DeltaCache.Buffer.Run(Premade.BytesOf(at), Premade.StartOf(at), length);
            }
            DeltaStats.Ahead();
            return length > 0 ? length : -1;
        }
    }
}
