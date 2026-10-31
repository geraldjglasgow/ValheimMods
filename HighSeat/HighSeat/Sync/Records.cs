using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// One object's record in a "ZDOData" package, byte for byte as ZDOMan.SendZDOs writes it (id, owner revision, data
    /// revision, owner, position, then the object's data as a nested package: its length, then ZDO.Serialize's bytes),
    /// made once a frame however many players get the object that frame. Nothing the send loop does changes an object,
    /// and nothing else runs between two players' sends in one frame, so the bytes are the same for every player; in a
    /// crowd of 100 one record is written once instead of 100 times. The records live in one buffer that is emptied at
    /// the start of every frame's sending (<see cref="NewFrame"/>) and copied from into each player's package; nothing is
    /// allocated per object.
    /// </summary>
    internal static class Records
    {
        private struct Span
        {
            public int Start;
            public int Length;
        }

        private static readonly ZPackage Made = new ZPackage();
        private static readonly ZPackage Data = new ZPackage();
        private static readonly Dictionary<ZDOID, Span> Where = new Dictionary<ZDOID, Span>();

        /// <summary>Forgets the last frame's records: objects may have changed since.</summary>
        public static void NewFrame()
        {
            if (Where.Count > 0)
            {
                Where.Clear();
                Made.Clear();
            }
        }

        /// <summary>Appends the object's record to the package.</summary>
        public static void Write(ZPackage pkg, ZDO zdo)
        {
            if (!Where.TryGetValue(zdo.m_uid, out Span span))
            {
                span = Make(zdo);
            }
            pkg.m_writer.Write(Made.m_stream.GetBuffer(), span.Start, span.Length);
        }

        private static Span Make(ZDO zdo)
        {
            int start = Made.Size();
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
            Span span = new Span { Start = start, Length = Made.Size() - start };
            Where[zdo.m_uid] = span;
            return span;
        }
    }
}
