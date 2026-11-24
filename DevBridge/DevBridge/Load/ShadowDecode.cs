using System.IO;

namespace DevBridge.Load
{
    /// <summary>
    /// Reads the two world-data formats a fake can be sent into its copy (<see cref="FakeShadow"/>), written from the
    /// game's own reader (ZDOMan.RPC_ZDOData) and HighSeat's format description, independently of HighSeat's and
    /// HighSeatPlus's code, so the check does not share their mistakes. The game's "ZDOData": the drops (a count and
    /// each ZDOID), then records (id, owner revision, data revision, owner, position, the data as a nested package)
    /// until ZDOID.None. HighSeat's delta format: a version byte, the drops, then records behind a kind byte (0 end, 1
    /// whole: the game's record; 2 delta: id, owner revision, data revision, a byte saying whether the owner (1) and the
    /// position (2) follow, the 7-bit steps back to the revisions it starts from, those that follow, then the changed
    /// data behind its 7-bit length).
    /// </summary>
    internal static class ShadowDecode
    {
        internal static void Game(FakeShadow shadow, ZPackage pkg)
        {
            shadow.GamePackages++;
            Drops(pkg);
            while (true)
            {
                ZDOID id = pkg.ReadZDOID();
                if (id.IsNone()) return;
                Whole(shadow, pkg, id);
            }
        }

        internal static void Deltas(FakeShadow shadow, ZPackage pkg)
        {
            shadow.DeltaPackages++;
            byte version = pkg.ReadByte();
            if (version != FakeDeltas.Version) throw new InvalidDataException($"delta package version {version}, expected {FakeDeltas.Version}");
            Drops(pkg);
            while (true)
            {
                byte kind = pkg.ReadByte();
                if (kind == 0) return;
                if (kind == 1) Whole(shadow, pkg, pkg.ReadZDOID());
                else if (kind == 2) Delta(shadow, pkg);
                else throw new InvalidDataException($"record kind {kind}");
            }
        }

        // The drops only move the game's object out of its sectors (ZDO.InvalidateSector): nothing kept changes.
        private static void Drops(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++) pkg.ReadZDOID();
        }

        private static void Whole(FakeShadow shadow, ZPackage pkg, ZDOID id)
        {
            ushort owner = pkg.ReadUShort();
            uint data = pkg.ReadUInt();
            long ownerId = pkg.ReadLong();
            byte[] position = pkg.ReadByteArray(12);
            byte[] objectData = pkg.ReadByteArray();
            shadow.WholeRecords++;
            shadow.Take(id, owner, data, ownerId, position, objectData);
        }

        private static void Delta(FakeShadow shadow, ZPackage pkg)
        {
            ZDOID id = pkg.ReadZDOID();
            ushort owner = pkg.ReadUShort();
            uint data = pkg.ReadUInt();
            byte has = pkg.ReadByte();
            uint fromData = data - (uint)ShadowObject.Varint(pkg);
            ushort fromOwner = (ushort)(owner - ShadowObject.Varint(pkg));
            long? ownerId = (has & 1) != 0 ? pkg.ReadLong() : (long?)null;
            byte[] position = (has & 2) != 0 ? pkg.ReadByteArray(12) : null;
            byte[] objectData = pkg.ReadByteArray(ShadowObject.Varint(pkg));
            shadow.DeltaRecords++;
            if (!shadow.Objects.TryGetValue(id, out ShadowObject copy) || copy.Data != fromData || copy.Owner != fromOwner)
            {
                shadow.Ask(id);
                return;
            }
            shadow.Take(id, owner, data, ownerId ?? copy.OwnerId, position ?? copy.Position, objectData);
        }
    }
}
