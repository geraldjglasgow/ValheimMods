namespace HighSeat
{
    /// <summary>
    /// For DevBridge's load tests (its fake players' /load?verify=1), read by reflection: the protocol version and the
    /// record the server sent of an object at given revisions, while the history keeps it, so a fake player's own copy
    /// built from what it received can be checked against what the server meant it to hold. Costs nothing unless called.
    /// </summary>
    public static class DeltaProbe
    {
        public const int Version = DeltaFormat.Version;

        /// <summary>
        /// A copy of the whole record (header and data, as <see cref="Records"/> made it) of the object at these
        /// revisions, or null when no such record is kept.
        /// </summary>
        public static byte[] Record(ZDOID id, uint dataRevision, ushort ownerRevision)
        {
            if (!History.Find(id, dataRevision, ownerRevision, out int at, out int length))
            {
                return null;
            }
            byte[] copy = new byte[length];
            System.Buffer.BlockCopy(History.Bytes, at, copy, 0, length);
            return copy;
        }
    }
}
