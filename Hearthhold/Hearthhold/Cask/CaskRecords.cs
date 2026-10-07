using System.Collections.Generic;

namespace Hearthhold
{
    /// <summary>One aging slot of a cask: what lies there, how many, with how many stars, aging since when (game seconds).</summary>
    public struct CaskRecord
    {
        public int X;
        public int Y;
        public string Item;
        public int Stars;
        public int Stack;
        public double Start;

        public bool SameAs(CaskRecord other) =>
            X == other.X && Y == other.Y && Item == other.Item && Stars == other.Stars && Stack == other.Stack && Start == other.Start;
    }

    /// <summary>
    /// A cask's aging records as stored in its ZDO (<see cref="CaskKeys.Aging"/>): a version, a count, then one record per
    /// slot. Written only by the cask's owner, read by every peer (the hover's time left).
    /// </summary>
    public static class CaskRecords
    {
        private const int Version = 1;

        public static List<CaskRecord> Read(ZDO zdo)
        {
            List<CaskRecord> records = new List<CaskRecord>();
            byte[] bytes = zdo?.GetByteArray(CaskKeys.Aging);
            if (bytes == null || bytes.Length == 0)
                return records;
            ZPackage package = new ZPackage(bytes);
            if (package.ReadInt() != Version)
                return records;
            int count = package.ReadInt();
            for (int i = 0; i < count; i++)
                records.Add(ReadOne(package));
            return records;
        }

        public static void Write(ZDO zdo, List<CaskRecord> records)
        {
            ZPackage package = new ZPackage();
            package.Write(Version);
            package.Write(records.Count);
            foreach (CaskRecord record in records)
                WriteOne(package, record);
            zdo.Set(CaskKeys.Aging, package.GetArray());
        }

        public static bool Equal(List<CaskRecord> a, List<CaskRecord> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].SameAs(b[i]))
                    return false;
            }
            return true;
        }

        private static CaskRecord ReadOne(ZPackage package) => new CaskRecord
        {
            X = package.ReadInt(),
            Y = package.ReadInt(),
            Item = package.ReadString(),
            Stars = package.ReadInt(),
            Stack = package.ReadInt(),
            Start = package.ReadDouble(),
        };

        private static void WriteOne(ZPackage package, CaskRecord record)
        {
            package.Write(record.X);
            package.Write(record.Y);
            package.Write(record.Item ?? "");
            package.Write(record.Stars);
            package.Write(record.Stack);
            package.Write(record.Start);
        }
    }
}
