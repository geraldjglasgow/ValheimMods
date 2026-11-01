using System.Text;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// One object's record (<see cref="Records"/>: id, owner revision, data revision, owner, position, then the data's
    /// length and ZDO.Serialize's bytes), written byte for byte as Records writes it with ZDO.Serialize, but read straight
    /// from the game's own tables into a plain buffer (<see cref="RecordBuffer"/>) with nothing allocated. ZDO.Serialize
    /// (through ZDOExtraData.GetData) copies each of the object's seven value lists into a new list and makes a closure and
    /// a delegate for each: about fifteen allocations a record, and Mono's garbage collector takes one lock for every
    /// allocation, so records made with it on several threads at once came no faster than on one (offline under Unity's
    /// own Mono, 2026-10-31: 149,000 player records a second on one thread, 183,000 at best on three, fewer on five) and
    /// a player's record cost 7 µs. Here: the flags as ZDO.Serialize sets them (connection, the seven lists that have
    /// entries, persistent, distant, the type, the rotation when it is not Quaternion.identity's euler angles by
    /// Vector3's own comparison), the prefab, the rotation, then the connection and each list in key order (the order
    /// BinarySearchDictionary keeps them in and GetData copies them in). What it reads, only reads: the ZDO's fields
    /// (id, revisions, flags, prefab, position, rotation), ZDOExtraData's seven value tables, its connections and its
    /// owners, and ZDOID's user list; nothing else is called. Used only while none of the game's methods it stands in for
    /// is patched and its records keep matching the game's own (<see cref="RecordChecks"/>). A string or byte array that
    /// is null (ZDO.Serialize throws on it) makes it give up: false, nothing kept, and the caller uses ZDO.Serialize.
    /// One per thread: it keeps the object's lists in its fields while it writes.
    /// </summary>
    internal sealed class RecordWriter
    {
        // ZDO.ExtraDataFlags
        private const int Connections = 0x1;
        private const int LowBits = 0xFF;
        private const int Persistent = 0x100;
        private const int Distant = 0x200;
        private const int TypeShift = 10;
        private const int Rotation = 0x1000;

        private static Vector3 _identity;

        private BinarySearchDictionary<int, float> _floats;
        private BinarySearchDictionary<int, Vector3> _vec3s;
        private BinarySearchDictionary<int, Quaternion> _quats;
        private BinarySearchDictionary<int, int> _ints;
        private BinarySearchDictionary<int, long> _longs;
        private BinarySearchDictionary<int, string> _strings;
        private BinarySearchDictionary<int, byte[]> _arrays;
        private ZDOConnection _connection;

        /// <summary>On the main thread before records are written: Quaternion.identity's euler angles, as ZDO.Serialize reads them.</summary>
        public static void Begin() => _identity = Quaternion.identity.eulerAngles;

        /// <summary>Appends the object's record; false when it gave up (the buffer is as it was).</summary>
        public bool Write(RecordBuffer into, ZDO zdo)
        {
            int start = into.Size;
            try
            {
                Header(into, zdo);
                if (Data(into, zdo))
                {
                    into.Put(start + DeltaFormat.DataLengthAt, into.Size - start - DeltaFormat.DataAt);
                    return true;
                }
            }
            catch (EncoderFallbackException)
            {
                // A string BinaryWriter cannot encode either: ZDO.Serialize throws on it in the caller's fallback.
            }
            finally
            {
                Forget();
            }
            into.Size = start;
            return false;
        }

        // Records' header (ZDOMan.SendZDOs' order), with room for the data's length.
        private static void Header(RecordBuffer into, ZDO zdo)
        {
            into.Id(zdo.m_uid);
            into.U16(zdo.OwnerRevision);
            into.I32((int)zdo.DataRevision);
            into.I64(OwnerOf(zdo));
            into.Vec3(zdo.m_position);
            into.I32(0);
        }

        /// <summary>ZDO.GetOwner, from the same tables: no owner unless flagged as owned, then the owners table through ZDOID's user list.</summary>
        public static long OwnerOf(ZDO zdo)
        {
            if ((zdo.m_dataFlags & ZDO.DataFlags.Owned) == 0)
            {
                return 0L;
            }
            return ZDOExtraData.s_owner.TryGetValue(zdo.m_uid, out ushort key) ? ZDOID.GetUserID(key) : 0L;
        }

        private bool Data(RecordBuffer into, ZDO zdo)
        {
            Find(zdo.m_uid);
            bool rotation = zdo.m_rotation != _identity;
            int flags = Flags(zdo, rotation);
            into.U16((ushort)flags);
            into.I32(zdo.m_prefab);
            if (rotation)
            {
                into.Vec3(zdo.m_rotation);
            }
            if ((flags & LowBits) == 0)
            {
                return true;
            }
            if ((flags & Connections) != 0)
            {
                into.U8((byte)_connection.m_type);
                into.Id(_connection.m_target);
            }
            return Lists(into);
        }

        // ZDOExtraData.GetData's lookups, without the copies.
        private void Find(ZDOID id)
        {
            ZDOExtraData.s_floats.TryGetValue(id, out _floats);
            ZDOExtraData.s_vec3.TryGetValue(id, out _vec3s);
            ZDOExtraData.s_quats.TryGetValue(id, out _quats);
            ZDOExtraData.s_ints.TryGetValue(id, out _ints);
            ZDOExtraData.s_longs.TryGetValue(id, out _longs);
            ZDOExtraData.s_strings.TryGetValue(id, out _strings);
            ZDOExtraData.s_byteArrays.TryGetValue(id, out _arrays);
            ZDOExtraData.s_connections.TryGetValue(id, out _connection);
        }

        private int Flags(ZDO zdo, bool rotation)
        {
            int flags = _connection != null && _connection.m_type != ZDOExtraData.ConnectionType.None ? Connections : 0;
            flags |= Has(_floats, 0x2) | Has(_vec3s, 0x4) | Has(_quats, 0x8) | Has(_ints, 0x10);
            flags |= Has(_longs, 0x20) | Has(_strings, 0x40) | Has(_arrays, 0x80);
            ZDO.DataFlags data = zdo.m_dataFlags;
            flags |= (data & ZDO.DataFlags.Persistent) != 0 ? Persistent : 0;
            flags |= (data & ZDO.DataFlags.Distant) != 0 ? Distant : 0;
            flags |= (int)(data & ZDO.DataFlags.Type) << TypeShift;
            return rotation ? flags | Rotation : flags;
        }

        private static int Has<T>(BinarySearchDictionary<int, T> list, int flag) => list != null && list.m_length > 0 ? flag : 0;

        private bool Lists(RecordBuffer into)
        {
            Floats(into);
            Vec3s(into);
            Quats(into);
            Ints(into);
            Longs(into);
            return Strings(into) && Arrays(into);
        }

        private void Floats(RecordBuffer into)
        {
            int n = _floats == null ? 0 : _floats.m_length;
            if (n == 0)
            {
                return;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                into.I32(_floats.m_keys[i]);
                into.F32(_floats.m_values[i]);
            }
        }

        private void Vec3s(RecordBuffer into)
        {
            int n = _vec3s == null ? 0 : _vec3s.m_length;
            if (n == 0)
            {
                return;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                into.I32(_vec3s.m_keys[i]);
                into.Vec3(_vec3s.m_values[i]);
            }
        }

        private void Quats(RecordBuffer into)
        {
            int n = _quats == null ? 0 : _quats.m_length;
            if (n == 0)
            {
                return;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                into.I32(_quats.m_keys[i]);
                into.Quat(_quats.m_values[i]);
            }
        }

        private void Ints(RecordBuffer into)
        {
            int n = _ints == null ? 0 : _ints.m_length;
            if (n == 0)
            {
                return;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                into.I32(_ints.m_keys[i]);
                into.I32(_ints.m_values[i]);
            }
        }

        private void Longs(RecordBuffer into)
        {
            int n = _longs == null ? 0 : _longs.m_length;
            if (n == 0)
            {
                return;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                into.I32(_longs.m_keys[i]);
                into.I64(_longs.m_values[i]);
            }
        }

        // False on a null string, which ZDO.Serialize throws on.
        private bool Strings(RecordBuffer into)
        {
            int n = _strings == null ? 0 : _strings.m_length;
            if (n == 0)
            {
                return true;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                string value = _strings.m_values[i];
                if (value == null)
                {
                    return false;
                }
                into.I32(_strings.m_keys[i]);
                into.Text(value);
            }
            return true;
        }

        // False on a null byte array, which ZDO.Serialize throws on.
        private bool Arrays(RecordBuffer into)
        {
            int n = _arrays == null ? 0 : _arrays.m_length;
            if (n == 0)
            {
                return true;
            }
            into.Count(n);
            for (int i = 0; i < n; i++)
            {
                byte[] value = _arrays.m_values[i];
                if (value == null)
                {
                    return false;
                }
                into.I32(_arrays.m_keys[i]);
                into.ByteArray(value);
            }
            return true;
        }

        // No ZDO's lists are kept past its record.
        private void Forget()
        {
            _floats = null;
            _vec3s = null;
            _quats = null;
            _ints = null;
            _longs = null;
            _strings = null;
            _arrays = null;
            _connection = null;
        }
    }
}
