using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>One queued selection of a site: a name and the piece indices (into the site blueprint's pieces) to build first.</summary>
    public sealed class SiteSelection
    {
        public string Name = "";
        public List<int> Pieces = new List<int>();
    }

    /// <summary>
    /// A construction site's state in its marker's ZDO (the contract every site class reads and writes; writes only on
    /// the ZDO's owner): the blueprint (<see cref="SiteCodec"/>), name, frame, the built pieces (a bit set), the queue,
    /// the store of delivered materials (item prefab name to count), whether the ground is shaped, the ground's net stone
    /// (worked out where the site is placed, corrected by the builder), and the creator. Reads are cached per ZDO data
    /// revision, so drawing every frame costs nothing.
    /// </summary>
    public sealed class SiteState
    {
        public const string BlueprintKey = "OpenKeep.site_bp";
        public const string NameKey = "OpenKeep.site_name";
        public const string OriginKey = "OpenKeep.site_origin";
        public const string YawKey = "OpenKeep.site_yaw";
        public const string BuiltKey = "OpenKeep.site_built";
        public const string QueueKey = "OpenKeep.site_queue";
        public const string StoreKey = "OpenKeep.site_store";
        public const string GroundKey = "OpenKeep.site_ground";
        public const string CreatorKey = "OpenKeep.site_creator";
        public const string CreatorNameKey = "OpenKeep.site_creatorName";
        public const string GroundStoneKey = "OpenKeep.site_groundStone";

        private readonly ZDO zdo;
        private Blueprint blueprint;
        private bool decoded;
        private uint builtRevision = uint.MaxValue;
        private bool[] built = new bool[0];

        public SiteState(ZDO zdo) => this.zdo = zdo;

        public ZDO Zdo => zdo;

        /// <summary>
        /// The site's blueprint, decoded once (it never changes after the site is placed); null while the ZDO has no
        /// blueprint yet (the frame a placed marker wakes before <see cref="Init"/>) or when it cannot be read.
        /// </summary>
        public Blueprint Blueprint
        {
            get
            {
                if (blueprint != null || decoded)
                    return blueprint;
                byte[] data = zdo.GetByteArray(BlueprintKey);
                if (data == null || data.Length == 0)
                    return null;
                decoded = true;
                blueprint = SiteCodec.Decode(data);
                return blueprint;
            }
        }

        public string Name => zdo.GetString(NameKey);

        public BuildFrame Frame => new BuildFrame(zdo.GetVec3(OriginKey, Vector3.zero), zdo.GetFloat(YawKey));

        public bool GroundDone => zdo.GetBool(GroundKey);

        /// <summary>Stone the ground work still costs (raised less lowered ground; negative: it gives stone back).</summary>
        public int GroundStone => zdo.GetInt(GroundStoneKey);

        public long Creator => zdo.GetLong(CreatorKey);

        public string CreatorName => zdo.GetString(CreatorNameKey);

        /// <summary>Which pieces stand (index into the blueprint's pieces); re-read when the ZDO changed.</summary>
        public bool[] Built
        {
            get
            {
                if (builtRevision == zdo.DataRevision && built.Length > 0)
                    return built;
                builtRevision = zdo.DataRevision;
                built = Bits(zdo.GetByteArray(BuiltKey), Blueprint?.Pieces.Count ?? 0);
                return built;
            }
        }

        public int BuiltCount
        {
            get
            {
                int n = 0;
                foreach (bool b in Built)
                    n += b ? 1 : 0;
                return n;
            }
        }

        public List<SiteSelection> Queue => ReadQueue(zdo.GetByteArray(QueueKey));

        public Dictionary<string, int> Store => ReadStore(zdo.GetByteArray(StoreKey));

        // ---------------------------------------------------------------- writes (the ZDO's owner only)

        /// <summary>Everything a new site starts with, on the machine that places it (which owns the new ZDO).</summary>
        public void Init(Blueprint bp, BuildFrame frame, Player creator)
        {
            zdo.Set(BlueprintKey, SiteCodec.Encode(bp));
            zdo.Set(NameKey, bp.Name ?? "");
            zdo.Set(OriginKey, frame.Origin);
            zdo.Set(YawKey, frame.Yaw);
            zdo.Set(CreatorKey, creator.GetPlayerID());
            zdo.Set(CreatorNameKey, creator.GetPlayerName());
            zdo.Set(BuiltKey, new byte[(bp.Pieces.Count + 7) / 8]);
            blueprint = bp;
        }

        public void SetBuilt(int index)
        {
            bool[] now = (bool[])Built.Clone();
            if (index < 0 || index >= now.Length || now[index])
                return;
            now[index] = true;
            zdo.Set(BuiltKey, Bytes(now));
        }

        /// <summary>Marks several pieces built with one write (a frame of a fast build).</summary>
        public void SetBuilt(ICollection<int> indices)
        {
            if (indices.Count == 0)
                return;
            bool[] now = (bool[])Built.Clone();
            foreach (int index in indices)
            {
                if (index >= 0 && index < now.Length)
                    now[index] = true;
            }
            zdo.Set(BuiltKey, Bytes(now));
        }

        public void SetGroundDone() => zdo.Set(GroundKey, true);

        public void SetGroundStone(int stone) => zdo.Set(GroundStoneKey, stone);

        public void SetQueue(List<SiteSelection> queue) => zdo.Set(QueueKey, WriteQueue(queue));

        public void SetStore(Dictionary<string, int> store) => zdo.Set(StoreKey, WriteStore(store));

        // ---------------------------------------------------------------- encoding

        private static bool[] Bits(byte[] data, int count)
        {
            bool[] bits = new bool[count];
            for (int i = 0; data != null && i < count && i / 8 < data.Length; i++)
                bits[i] = (data[i / 8] & (1 << (i % 8))) != 0;
            return bits;
        }

        private static byte[] Bytes(bool[] bits)
        {
            byte[] data = new byte[(bits.Length + 7) / 8];
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    data[i / 8] |= (byte)(1 << (i % 8));
            }
            return data;
        }

        public static byte[] WriteQueue(List<SiteSelection> queue)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(queue.Count);
            foreach (SiteSelection s in queue)
            {
                pkg.Write(s.Name ?? "");
                pkg.Write(s.Pieces.Count);
                foreach (int i in s.Pieces)
                    pkg.Write(i);
            }
            return pkg.GetArray();
        }

        public static List<SiteSelection> ReadQueue(byte[] data)
        {
            List<SiteSelection> queue = new List<SiteSelection>();
            if (data == null || data.Length == 0)
                return queue;
            ZPackage pkg = new ZPackage(data);
            int count = pkg.ReadInt();
            for (int n = 0; n < count; n++)
            {
                SiteSelection s = new SiteSelection { Name = pkg.ReadString() };
                int pieces = pkg.ReadInt();
                for (int i = 0; i < pieces; i++)
                    s.Pieces.Add(pkg.ReadInt());
                queue.Add(s);
            }
            return queue;
        }

        private static byte[] WriteStore(Dictionary<string, int> store)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(store.Count);
            foreach (KeyValuePair<string, int> item in store)
            {
                pkg.Write(item.Key);
                pkg.Write(item.Value);
            }
            return pkg.GetArray();
        }

        private static Dictionary<string, int> ReadStore(byte[] data)
        {
            Dictionary<string, int> store = new Dictionary<string, int>();
            if (data == null || data.Length == 0)
                return store;
            ZPackage pkg = new ZPackage(data);
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
                store[pkg.ReadString()] = pkg.ReadInt();
            return store;
        }
    }
}
