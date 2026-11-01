using System.IO;

namespace HighSeatPlus
{
    /// <summary>
    /// The parts of the delta record being read (<see cref="DeltaFormat"/>), found in place, and the game record made
    /// from it: the id and the new revisions as the delta carries them (the same 18 bytes the game's record starts
    /// with), the owner and position from the delta or, where they did not change, from this game's copy (set from
    /// the server's record the delta starts from), then the changed data behind the game's int length. One record at a
    /// time, so its parts are static fields.
    /// </summary>
    internal static class DeltaHeader
    {
        // ZDO.Deserialize always reads the flags and the prefab: data shorter than that is broken.
        private const int MinData = 6;

        public static ZDOID Id;
        public static uint Data;
        public static ushort Owner;
        public static uint FromData;
        public static ushort FromOwner;

        private static int _at;
        private static int _ownerAt;
        private static int _positionAt;
        private static int _dataAt;
        private static int _dataLength;

        /// <summary>Reads the record at p; where the next record starts, or -1 when it is broken or runs past end.</summary>
        public static int Read(byte[] b, int p, int end)
        {
            if (p + DeltaFormat.RevisionsSize + 1 > end)
            {
                return -1;
            }
            _at = p;
            Id = PackageBytes.Id(b, p);
            Owner = PackageBytes.U16(b, p + 12);
            Data = PackageBytes.U32(b, p + 14);
            byte has = b[p + DeltaFormat.RevisionsSize];
            int q = p + DeltaFormat.RevisionsSize + 1;
            if (!Steps(b, ref q, end))
            {
                return -1;
            }
            _ownerAt = (has & DeltaFormat.HasOwner) != 0 ? PackageBytes.Take(ref q, 8, end) : -1;
            _positionAt = (has & DeltaFormat.HasPosition) != 0 ? PackageBytes.Take(ref q, 12, end) : -1;
            _dataLength = PackageBytes.Varint(b, ref q, end);
            _dataAt = _dataLength < MinData ? -1 : PackageBytes.Take(ref q, _dataLength, end);
            return _dataAt < 0 ? -1 : q;
        }

        // The steps back to the revisions the delta starts from; false when broken or larger than the revisions.
        private static bool Steps(byte[] b, ref int q, int end)
        {
            int dataStep = PackageBytes.Varint(b, ref q, end);
            int ownerStep = PackageBytes.Varint(b, ref q, end);
            if (dataStep < 0 || ownerStep < 0 || (uint)dataStep > Data || ownerStep > Owner)
            {
                return false;
            }
            FromData = Data - (uint)dataStep;
            FromOwner = (ushort)(Owner - ownerStep);
            return true;
        }

        /// <summary>The game's own record for the delta just read, over this game's copy of the object.</summary>
        public static void WriteGameRecord(ZPackage game, byte[] b, ZDO zdo)
        {
            BinaryWriter writer = game.m_writer;
            writer.Write(b, _at, DeltaFormat.RevisionsSize);
            if (_ownerAt >= 0)
            {
                writer.Write(b, _ownerAt, 8);
            }
            else
            {
                game.Write(zdo.GetOwner());
            }
            if (_positionAt >= 0)
            {
                writer.Write(b, _positionAt, 12);
            }
            else
            {
                game.Write(zdo.GetPosition());
            }
            writer.Write(_dataLength);
            writer.Write(b, _dataAt, _dataLength);
        }
    }
}
