namespace HighSeatPlus
{
    /// <summary>
    /// The delta protocol with HighSeat on the server, version <see cref="Version"/>: this game's copy of HighSeat's
    /// HighSeat.DeltaFormat (the two must agree; a change there is a new version here). Hello once on connecting
    /// (<see cref="HelloRpc"/>, an int: the version read here, 0 to give up). A HighSeat server then sends
    /// <see cref="DataRpc"/> in place of the game's "ZDOData": a ZPackage holding a version byte, the ids to drop (a
    /// count and each ZDOID, as the game writes them), then records each behind a kind byte until <see cref="End"/>.
    /// <see cref="Full"/> is the game's own record byte for byte (id, owner revision, data revision, owner, position,
    /// the data's int length and ZDO.Serialize's bytes); <see cref="Delta"/> is id, owner revision, data revision, a byte
    /// of <see cref="HasOwner"/> and <see cref="HasPosition"/>, the steps back to the revisions the delta starts from
    /// (two 7-bit numbers), the owner and the position when they changed, then data in ZDO.Serialize's layout holding
    /// only what changed, behind its 7-bit length. <see cref="FullCopyRpc"/> (a ZPackage: a count, then each ZDOID)
    /// asks for objects whose delta did not fit this game's copy.
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

        /// <summary>The game's record header: id 12, owner revision 2, data revision 4, owner 8, position 12, length 4.</summary>
        public const int HeaderSize = 42;

        public const int RevisionsSize = 18;
        public const int DataLengthAt = 38;
    }
}
