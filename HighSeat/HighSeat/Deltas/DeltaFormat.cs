namespace HighSeat
{
    /// <summary>
    /// The protocol between HighSeat (the server) and HighSeatPlus (a player's game) for smaller world updates, version
    /// <see cref="Version"/>. HighSeatPlus says hello once on connecting (<see cref="HelloRpc"/>, an int: the version it
    /// reads, 0 to stop); HighSeat then sends that player <see cref="DataRpc"/> instead of the game's "ZDOData", with the
    /// same ZPackage layout inside ZRpc (the method's hash, its length, the payload). The payload: a version byte, the
    /// ids to drop as the game writes them (a count, then each ZDOID), then records, each led by a kind byte, until
    /// <see cref="End"/>. <see cref="Full"/>: the game's own record, byte for byte (<see cref="Records"/>: id, owner
    /// revision, data revision, owner, position, the data's length and ZDO.Serialize's bytes). <see cref="Delta"/>: id,
    /// owner revision, data revision, a byte of <see cref="HasOwner"/> and <see cref="HasPosition"/>, the steps from the
    /// revisions the player holds (two 7-bit numbers), the owner and the position when they differ from what the player
    /// holds, then the data in ZDO.Serialize's own layout holding only what changed (<see cref="Diff"/>), its length a
    /// 7-bit number. A player who cannot use a delta (it holds other revisions) asks for the whole object again
    /// (<see cref="FullCopyRpc"/>, a ZPackage: a count, then each ZDOID). HighSeatPlus keeps a copy of these values; a
    /// change here is a new version there too.
    /// </summary>
    internal static class DeltaFormat
    {
        public const int Version = 1;
        public const string HelloRpc = "HighSeatPlus_Deltas";
        public const string DataRpc = "HighSeat_ZDODeltas";
        public const string FullCopyRpc = "HighSeatPlus_FullCopy";

        public const byte End = 0;
        public const byte Full = 1;
        public const byte Delta = 2;

        public const byte HasOwner = 1;
        public const byte HasPosition = 2;

        /// <summary>At most this many ids are taken from one full-copy request; the rest are left (they ask again).</summary>
        public const int MaxAskedIds = 4096;

        public static readonly int DataHash = DataRpc.GetStableHashCode();

        // A record's header as Records writes it (ZDOMan.SendZDOs' order), offsets from its start.
        public const int OwnerRevisionAt = 12;
        public const int DataRevisionAt = 14;
        public const int OwnerAt = 18;
        public const int PositionAt = 26;
        public const int DataLengthAt = 38;
        public const int DataAt = 42;
    }
}
