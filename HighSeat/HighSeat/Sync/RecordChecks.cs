using System;
using System.Collections.ObjectModel;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Whether <see cref="RecordWriter"/> may stand in for ZDO.Serialize in the records the server sends
    /// (<see cref="Records"/>). Two checks. No patch, of any mod, may be on a game method whose bytes or lookups it copies
    /// (ZDO.Serialize, ZDOExtraData.GetData, GetConnection and GetOwner, ZDO.GetOwner and GetPosition, ZDOID's user
    /// lookup, and the ZPackage writes a record is made of): looked at when a session starts and every
    /// <see cref="RecheckSeconds"/> s after, as a mod can patch later. And its records must match the game's own: the
    /// first <see cref="FirstChecks"/> records of a session, and one in <see cref="SampleEvery"/> after, are made again
    /// with ZDO.Serialize when they are first used and compared byte for byte; one that differs goes as the game made
    /// it, and the writer is off for the rest of the session (logged once, with the object), as after a game update that
    /// changed the layout; so is a writer that throws. While it is off every record is made with ZDO.Serialize on the
    /// main thread, as before, and none is made ahead on the send threads (<see cref="Premade"/>). Each change is logged
    /// once. Main thread only.
    /// </summary>
    internal static class RecordChecks
    {
        private const float RecheckSeconds = 10f;
        private const int FirstChecks = 512;
        private const int SampleEvery = 256;

        private static readonly ZPackage Game = new ZPackage();
        private static readonly ZPackage GameData = new ZPackage();
        private static MethodBase[] _methods;
        private static string _patched = "not checked yet";
        private static string _said;
        private static bool _differs;
        private static float _nextCheck;
        private static int _first;
        private static int _sampled;

        /// <summary>True while records may be written by <see cref="RecordWriter"/>, here and ahead on the send threads.</summary>
        public static bool On => _patched == null && !_differs;

        /// <summary>A new session: checked afresh, and a difference found in the last one forgotten.</summary>
        public static void Begin()
        {
            RecordWriter.Begin();
            _differs = false;
            _first = 0;
            _sampled = 0;
            _nextCheck = 0f;
            Frame(0f);
        }

        /// <summary>Each frame of sending: the patches looked at when due.</summary>
        public static void Frame(float now)
        {
            if (now < _nextCheck || _differs)
            {
                return;
            }
            _nextCheck = now + RecheckSeconds;
            _patched = Patched();
            Say(_patched == null ? "on" : $"left to the game's ZDO.Serialize, {_patched}");
        }

        /// <summary>
        /// A record the writer made (here or ahead), before it is first used: false when a comparison with the game's
        /// own found it differs (the writer is then off for the session).
        /// </summary>
        public static bool Agrees(ZDO zdo, byte[] bytes, int at, int length)
        {
            if (!Sampled())
            {
                return true;
            }
            Serialize(zdo);
            if (Game.Size() == length && RecordBytes.Same(Game.m_stream.GetBuffer(), 0, bytes, at, length))
            {
                return true;
            }
            _differs = true;
            HighSeat.Log.LogWarning($"a record made without ZDO.Serialize differs from the game's ({zdo.m_uid}, prefab {zdo.GetPrefab()}, " +
                $"{length} bytes against {Game.Size()}): records are made with ZDO.Serialize for the rest of this session");
            return false;
        }

        /// <summary>The writer threw: off for the rest of the session, logged once; records go as the game makes them.</summary>
        public static void Broke(Exception e)
        {
            if (_differs)
            {
                return;
            }
            _differs = true;
            HighSeat.Log.LogWarning($"writing a record without ZDO.Serialize failed, so records are made with ZDO.Serialize for the rest of this session: {e}");
        }

        // The session's first records, then one in SampleEvery (counted so it never overflows).
        private static bool Sampled()
        {
            if (_first < FirstChecks)
            {
                _first++;
                return true;
            }
            if (++_sampled < SampleEvery)
            {
                return false;
            }
            _sampled = 0;
            return true;
        }

        // Records' own make, the game's way: the header, then ZDO.Serialize's bytes behind their length.
        private static void Serialize(ZDO zdo)
        {
            Game.Clear();
            Game.Write(zdo.m_uid);
            Game.Write(zdo.OwnerRevision);
            Game.Write(zdo.DataRevision);
            Game.Write(zdo.GetOwner());
            Game.Write(zdo.GetPosition());
            GameData.Clear();
            zdo.Serialize(GameData);
            Game.m_writer.Write(GameData.Size());
            Game.m_writer.Write(GameData.m_stream.GetBuffer(), 0, GameData.Size());
        }

        // Null when nothing is patched, else which patch is on which method.
        private static string Patched()
        {
            _methods ??= Methods();
            foreach (MethodBase method in _methods)
            {
                if (method == null)
                {
                    return "a game method it copies is missing";
                }
                string owner = Owner(Harmony.GetPatchInfo(method));
                if (owner != null)
                {
                    return $"{owner} patches {method.DeclaringType?.Name}.{method.Name}";
                }
            }
            return null;
        }

        // The owner of the first patch on the method, or null.
        private static string Owner(Patches info)
        {
            if (info == null)
            {
                return null;
            }
            return First(info.Prefixes) ?? First(info.Postfixes) ?? First(info.Transpilers) ?? First(info.Finalizers)
                ?? First(info.ILManipulators);
        }

        private static string First(ReadOnlyCollection<Patch> patches) => patches != null && patches.Count > 0 ? patches[0].owner : null;

        private static MethodBase[] Methods()
        {
            Type pkg = typeof(ZPackage);
            Type extra = typeof(ZDOExtraData);
            return new MethodBase[]
            {
                AccessTools.Method(typeof(ZDO), nameof(ZDO.Serialize), new[] { pkg }),
                AccessTools.Method(typeof(ZDO), nameof(ZDO.GetOwner), Type.EmptyTypes),
                AccessTools.Method(typeof(ZDO), nameof(ZDO.GetPosition), Type.EmptyTypes),
                AccessTools.Method(extra, nameof(ZDOExtraData.GetData)),
                AccessTools.Method(extra, nameof(ZDOExtraData.GetConnection), new[] { typeof(ZDOID) }),
                AccessTools.Method(extra, nameof(ZDOExtraData.GetOwner), new[] { typeof(ZDOID) }),
                AccessTools.Method(typeof(ZDOID), nameof(ZDOID.GetUserID), new[] { typeof(ushort) }),
                AccessTools.PropertyGetter(typeof(ZDOID), nameof(ZDOID.UserID)),
                Write(pkg, typeof(ZDOID)), Write(pkg, typeof(ushort)), Write(pkg, typeof(uint)), Write(pkg, typeof(long)),
                Write(pkg, typeof(int)), Write(pkg, typeof(float)), Write(pkg, typeof(byte)), Write(pkg, typeof(Vector3)),
                Write(pkg, typeof(Quaternion)), Write(pkg, typeof(string)), Write(pkg, typeof(byte[])),
                AccessTools.Method(pkg, nameof(ZPackage.WriteNumItems), new[] { typeof(int) }),
            };
        }

        private static MethodBase Write(Type pkg, Type value) => AccessTools.Method(pkg, nameof(ZPackage.Write), new[] { value });

        private static void Say(string state)
        {
            if (state != _said)
            {
                _said = state;
                HighSeat.Log.LogInfo($"records written without the game's copies: {state}");
            }
        }
    }
}
