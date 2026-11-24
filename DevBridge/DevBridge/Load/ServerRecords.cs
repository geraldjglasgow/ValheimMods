using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace DevBridge.Load
{
    /// <summary>
    /// The server's record of an object at the revisions a fake is booked at, for the check (<see cref="ShadowVerify"/>):
    /// the object itself when it still stands at those revisions (its owner, position and ZDO.Serialize's data now),
    /// else the record HighSeat sent then, from its history (HighSeat.DeltaProbe.Record, found by reflection in the
    /// running HighSeat's assembly once per report, as <see cref="HighSeatStats"/> finds its numbers), else none. The
    /// record is returned as a copy with its owner and position bytes beside it.
    /// </summary>
    internal sealed class ServerRecords
    {
        private const string Guid = "com.HighSeat";
        private const string ProbeName = "HighSeat.DeltaProbe";
        private readonly Func<ZDOID, uint, ushort, byte[]> history;

        internal ServerRecords()
        {
            history = Find(out string missing, out int version);
            Missing = missing;
            Version = version;
        }

        /// <summary>Why HighSeat's history cannot be read (not loaded, too old), or null.</summary>
        internal string Missing { get; }

        /// <summary>HighSeat's delta protocol version, 0 when unknown.</summary>
        internal int Version { get; }

        /// <summary>The record at these revisions, or null: its data parsed, with the header's owner and position.</summary>
        internal ShadowObject At(ZDOID id, uint data, ushort owner, out bool current)
        {
            current = false;
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo != null && zdo.DataRevision == data && zdo.OwnerRevision == owner)
            {
                current = true;
                return Now(zdo);
            }
            byte[] record = history?.Invoke(id, data, owner);
            return record == null ? null : FromRecord(record);
        }

        private static ShadowObject Now(ZDO zdo)
        {
            var bytes = new ZPackage();
            zdo.Serialize(bytes);
            ShadowObject copy = ShadowObject.Read(bytes.GetArray());
            var position = new ZPackage();
            position.Write(zdo.GetPosition());
            copy.OwnerId = zdo.GetOwner();
            copy.Position = position.GetArray();
            return copy;
        }

        // A whole record as HighSeat makes it: id, owner revision, data revision, owner, position, length, data.
        private static ShadowObject FromRecord(byte[] record)
        {
            var pkg = new ZPackage(record);
            pkg.ReadZDOID();
            pkg.ReadUShort();
            pkg.ReadUInt();
            long ownerId = pkg.ReadLong();
            byte[] position = pkg.ReadByteArray(12);
            ShadowObject copy = ShadowObject.Read(pkg.ReadByteArray());
            copy.OwnerId = ownerId;
            copy.Position = position;
            return copy;
        }

        private static Func<ZDOID, uint, ushort, byte[]> Find(out string missing, out int version)
        {
            version = 0;
            missing = null;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
            {
                missing = "HighSeat is not loaded";
                return null;
            }
            Type probe = info.Instance.GetType().Assembly.GetType(ProbeName);
            MethodInfo record = probe?.GetMethod("Record", new[] { typeof(ZDOID), typeof(uint), typeof(ushort) });
            if (record == null)
            {
                missing = $"HighSeat {info.Metadata.Version} has no {ProbeName}.Record";
                return null;
            }
            object value = probe.GetField("Version")?.GetRawConstantValue();
            version = value is int number ? number : 0;
            return (Func<ZDOID, uint, ushort, byte[]>)Delegate.CreateDelegate(typeof(Func<ZDOID, uint, ushort, byte[]>), record);
        }
    }
}
