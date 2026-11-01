using System.Collections.Generic;

namespace HighSeatPlus
{
    /// <summary>
    /// Turns one delta package from the server (<see cref="DeltaFormat"/>) into the game's own "ZDOData" package, so the
    /// game's ZDOMan.RPC_ZDOData does every bit of the work a full update does (revisions, owner, position, the data
    /// through ZDO.Deserialize, portals, the book of what the server holds, its counters) and every mod's patch on it
    /// sees the same. The drops are copied as they are. A whole record is copied byte for byte. A delta is used only when
    /// this game's copy stands at the revisions it starts from as the server sent them (<see cref="HeldCopies"/>): it
    /// becomes a game record with the new revisions, the owner and position (the delta's, or the copy's own where they
    /// did not change, which are the server's from the record it starts from) and the changed data, which Deserialize
    /// lays over the copy exactly as the whole data would (it only ever sets what it reads). Otherwise the object is
    /// left out and asked for again whole (<see cref="Asks"/>). What RPC_ZDOData will do with each record goes into
    /// <see cref="Expected"/>, worked out before it runs. False, with nothing to apply, for a package that is not
    /// this version or whose parts run past its end.
    /// </summary>
    internal static class PackageConvert
    {
        public static readonly ZPackage Game = new ZPackage();
        public static readonly List<HeldCopies.Expect> Expected = new List<HeldCopies.Expect>();
        public static readonly List<ZDOID> Asks = new List<ZDOID>();

        /// <summary>Deltas used in this package (the rest of the records went whole or were asked for again).</summary>
        public static int Deltas;

        /// <summary>Fills <see cref="Game"/>, <see cref="Expected"/> and <see cref="Asks"/>; false when it is not readable.</summary>
        public static bool Convert(ZDOMan man, ZPackage pkg)
        {
            Clear();
            byte[] b = pkg.m_stream.GetBuffer();
            int end = pkg.Size();
            int p = pkg.GetPos();
            if (p >= end || b[p++] != DeltaFormat.Version)
            {
                return false;
            }
            p = Drops(b, p, end);
            while (p >= 0 && p < end)
            {
                byte kind = b[p++];
                if (kind == DeltaFormat.End)
                {
                    Game.Write(ZDOID.None);
                    return p == end;
                }
                p = kind == DeltaFormat.Full ? Whole(man, b, p, end) : kind == DeltaFormat.Delta ? Delta(man, b, p, end) : -1;
            }
            return false;
        }

        public static void Clear()
        {
            Game.Clear();
            Expected.Clear();
            Asks.Clear();
            Deltas = 0;
        }

        private static int Drops(byte[] b, int p, int end)
        {
            int count = p + 4 <= end ? PackageBytes.I32(b, p) : -1;
            p += 4;
            if (count < 0 || p + 12L * count > end)
            {
                return -1;
            }
            Game.Write(count);
            Game.m_writer.Write(b, p, 12 * count);
            return p + 12 * count;
        }

        private static int Whole(ZDOMan man, byte[] b, int p, int end)
        {
            int length = p + DeltaFormat.HeaderSize <= end ? PackageBytes.I32(b, p + DeltaFormat.DataLengthAt) : -1;
            if (length < 0 || p + DeltaFormat.HeaderSize + (long)length > end)
            {
                return -1;
            }
            ZDOID id = PackageBytes.Id(b, p);
            Expected.Add(HeldCopies.Predict(man, id, PackageBytes.U32(b, p + 14), PackageBytes.U16(b, p + 12)));
            Game.m_writer.Write(b, p, DeltaFormat.HeaderSize + length);
            return p + DeltaFormat.HeaderSize + length;
        }

        private static int Delta(ZDOMan man, byte[] b, int p, int end)
        {
            int next = DeltaHeader.Read(b, p, end);
            if (next < 0)
            {
                return -1;
            }
            ZDO zdo = man.GetZDO(DeltaHeader.Id);
            if (zdo == null || !HeldCopies.Holds(zdo, DeltaHeader.FromData, DeltaHeader.FromOwner))
            {
                Asks.Add(DeltaHeader.Id);
                return next;
            }
            Expected.Add(HeldCopies.Predict(man, DeltaHeader.Id, DeltaHeader.Data, DeltaHeader.Owner));
            DeltaHeader.WriteGameRecord(Game, b, zdo);
            Deltas++;
            return next;
        }
    }
}
