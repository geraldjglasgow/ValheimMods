namespace HighSeat
{
    /// <summary>
    /// One delta record (<see cref="DeltaFormat"/>), from the record a player holds (in <see cref="History"/>) to the
    /// record about to go (in <see cref="Records"/>, or made ahead in a send thread's buffer, <see cref="Premade"/>): the
    /// id and the new revisions copied from the new record's header, which of the owner and position differ from the held
    /// record's, the steps of the two revisions, those that differ, then the changed data (<see cref="Diff"/>) behind its
    /// 7-bit length. It is appended only when it is smaller than the whole record with its kind byte; otherwise nothing
    /// is appended and the whole record goes. One per thread that works deltas out (the main thread's in
    /// <see cref="DeltaWriter"/>, one in each <see cref="Ahead"/>), each with its own diff and data buffer.
    /// </summary>
    internal sealed class DeltaRecord
    {
        private readonly RecordBuffer _data = new RecordBuffer();
        private readonly Diff _diff = new Diff();

        /// <summary>
        /// Appends the delta from the held record at <paramref name="oldAt"/> in <paramref name="old"/> to the record
        /// <paramref name="span"/> in <paramref name="now"/>; its length, or -1 when the whole record is no larger or the
        /// records cannot be compared.
        /// </summary>
        public int Append(RecordBuffer into, byte[] old, int oldAt, byte[] now, Records.Span span, uint heldData, ushort heldOwner)
        {
            int nowAt = span.Start;
            int oldLength = RecordBytes.I32(old, oldAt + DeltaFormat.DataLengthAt);
            int nowLength = RecordBytes.I32(now, nowAt + DeltaFormat.DataLengthAt);
            if (!_diff.Write(old, oldAt + DeltaFormat.DataAt, oldLength, now, nowAt + DeltaFormat.DataAt, nowLength, _data))
            {
                return -1;
            }
            int dataStep = (int)(span.DataRevision - heldData);
            int ownerStep = span.OwnerRevision - heldOwner;
            byte has = Has(old, oldAt, now, nowAt);
            int size = Size(has, dataStep, ownerStep, _data.Size);
            if (size >= span.Length + 1)
            {
                return -1;
            }
            Write(into, now, nowAt, has, dataStep, ownerStep);
            return size;
        }

        // Which of the owner and the position differ from the held record's.
        private static byte Has(byte[] old, int oldAt, byte[] now, int nowAt)
        {
            byte has = 0;
            if (!RecordBytes.Same(old, oldAt + DeltaFormat.OwnerAt, now, nowAt + DeltaFormat.OwnerAt, 8))
            {
                has |= DeltaFormat.HasOwner;
            }
            if (!RecordBytes.Same(old, oldAt + DeltaFormat.PositionAt, now, nowAt + DeltaFormat.PositionAt, 12))
            {
                has |= DeltaFormat.HasPosition;
            }
            return has;
        }

        private static int Size(byte has, int dataStep, int ownerStep, int dataSize)
        {
            int size = 1 + DeltaFormat.OwnerAt + 1 + RecordBytes.VarintSize(dataStep) + RecordBytes.VarintSize(ownerStep);
            size += (has & DeltaFormat.HasOwner) != 0 ? 8 : 0;
            size += (has & DeltaFormat.HasPosition) != 0 ? 12 : 0;
            return size + RecordBytes.VarintSize(dataSize) + dataSize;
        }

        private void Write(RecordBuffer into, byte[] now, int nowAt, byte has, int dataStep, int ownerStep)
        {
            into.U8(DeltaFormat.Delta);
            into.Run(now, nowAt, DeltaFormat.OwnerAt);
            into.U8(has);
            into.Varint(dataStep);
            into.Varint(ownerStep);
            if ((has & DeltaFormat.HasOwner) != 0)
            {
                into.Run(now, nowAt + DeltaFormat.OwnerAt, 8);
            }
            if ((has & DeltaFormat.HasPosition) != 0)
            {
                into.Run(now, nowAt + DeltaFormat.PositionAt, 12);
            }
            into.Varint(_data.Size);
            into.Run(_data.Bytes, 0, _data.Size);
        }
    }
}
