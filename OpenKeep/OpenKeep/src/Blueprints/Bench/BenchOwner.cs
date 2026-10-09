using System.Collections.Generic;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// One player's part of the shared pool: their character's id and name, the blueprints they shared and their folders
    /// (every folder, empty ones too), as paths from the top of their part, sorted.
    /// </summary>
    public sealed class BenchOwner
    {
        public long Id;
        public string Name = "";
        public readonly List<string> Paths = new List<string>();
        public readonly List<string> Folders = new List<string>();
    }

    /// <summary>
    /// The pool as the server sends it (<see cref="BenchRpc.Listing"/>): every owner with their blueprints and folders,
    /// packed, so a few hundred paths stay a few kilobytes.
    /// </summary>
    public static class BenchListing
    {
        public static ZPackage Write(List<BenchOwner> owners)
        {
            ZPackage inner = new ZPackage();
            inner.Write(owners.Count);
            foreach (BenchOwner owner in owners)
            {
                inner.Write(owner.Id);
                inner.Write(owner.Name);
                WriteList(inner, owner.Paths);
                WriteList(inner, owner.Folders);
            }
            ZPackage outer = new ZPackage();
            outer.Write(BenchZip.Pack(inner.GetArray()));
            return outer;
        }

        /// <summary>The owners in the package; empty when it cannot be read.</summary>
        public static List<BenchOwner> Read(ZPackage package)
        {
            List<BenchOwner> owners = new List<BenchOwner>();
            byte[] data = BenchZip.Unpack(package.ReadByteArray(), BenchLimits.MaxFileBytes);
            if (data == null)
                return owners;
            ZPackage inner = new ZPackage(data);
            int count = inner.ReadInt();
            for (int i = 0; i < count; i++)
            {
                BenchOwner owner = new BenchOwner { Id = inner.ReadLong(), Name = inner.ReadString() };
                ReadList(inner, owner.Paths);
                ReadList(inner, owner.Folders);
                owners.Add(owner);
            }
            return owners;
        }

        private static void WriteList(ZPackage package, List<string> list)
        {
            package.Write(list.Count);
            foreach (string item in list)
                package.Write(item);
        }

        private static void ReadList(ZPackage package, List<string> list)
        {
            int count = package.ReadInt();
            for (int i = 0; i < count; i++)
                list.Add(package.ReadString());
        }
    }
}
