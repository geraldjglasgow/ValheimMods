using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Whether each send patch may stand in for the game's code: that code must be what it was checked against (each
    /// method's <see cref="GameCode"/> fingerprint equals the value taken from Valheim 1.0.21, see the constants), and no
    /// other patch may be on it (a mod that hooks or changes those methods, a compression mod or another network mod,
    /// keeps the game's own path and so keeps working; the patch here stays out of its way). The Steam send loop
    /// (SendQueuedPackages) comes in two builds: a player's game, hosting or not, calls Steam's client interface
    /// (SteamNetworkingSockets), the dedicated server's game, built apart (its ZNet.IsDedicated is true), the game-server
    /// one (SteamGameServerNetworkingSockets). The build found decides which one <see cref="SteamDirect"/> calls, so it is
    /// always the one the game itself calls. The dedicated server's value is the client build's code with the game-server
    /// interface in its place, as its game was not at hand to read: should its code differ in anything else, the direct
    /// send stays off on dedicated servers and the log gives the value to check. Fingerprints are taken once per
    /// process (the game's code does not change while it runs), the patches at every check (a mod can patch later). Each
    /// answer is null to go ahead, or why not, for the log. A game update that changes one of these methods leaves the
    /// features that copy it off and logs the method with its new value: read the new code, and when the patch still
    /// does exactly what it does, put the new value here. One patch of another mod is known to be safe beside them:
    /// Jotunn's prefix on ZSteamSocket.Send (<see cref="AllowedPrefixes"/>) only holds back a joining player's first
    /// world packages while its config sync with them runs (it returns false, so the patches here, last and checking
    /// __runOriginal, stand aside) and lets every other send through unchanged; Jotunn is on most modded servers.
    /// </summary>
    internal static class SendChecks
    {
        private const uint SendCode = 0x883d9c75;
        private const uint QueueClientCode = 0x8d1fc2a4;
        private const uint QueueServerCode = 0x49c0b107;
        private const uint GetArrayCode = 0x379cdc48;
        private const uint WriteCode = 0xa4e39731;
        private const uint RouteCode = 0x9c0ecae9;
        private const uint InvokeCode = 0xb69913a2;
        private const uint SerializeCode = 0x05c38527;
        private const uint SendPackageCode = 0xc5455035;
        private const string Unchecked = "its game code was not checked";
        private static readonly string[] AllowedPrefixes = { "Jotunn.Managers.SynchronizationManager+Patches.Socket_Send_Prefix" };

        private static readonly Type[] Mine = { typeof(SteamDirect), typeof(RouteBroadcast), typeof(NestedWrite) };
        private static MethodBase _send, _queue, _getArray, _write, _route, _invoke, _serialize, _sendPackage;
        private static string _directCode = Unchecked, _nestedCode = Unchecked, _broadcastCode = Unchecked;
        private static bool _fingerprinted;

        /// <summary>The Steam interface the game's own send loop calls: true for the game-server one.</summary>
        public static bool GameServerApi { get; private set; }

        public static string Direct()
        {
            Fingerprints();
            return _directCode ?? Patched(_send, _queue, _getArray);
        }

        public static string Nested()
        {
            Fingerprints();
            return _nestedCode ?? Patched(_write, _getArray);
        }

        // The broadcast writes what ZRpc.Invoke, Serialize, SendPackage and ZPackage.Write would, and hands it to the
        // socket's own Send, so all of them must be the game's.
        public static string Broadcast()
        {
            Fingerprints();
            return _broadcastCode ?? Patched(_route, _invoke, _serialize, _sendPackage, _write, _getArray, _send);
        }

        // Once per process; the verdicts start as "not checked", so a check that throws leaves every feature off.
        private static void Fingerprints()
        {
            if (_fingerprinted)
            {
                return;
            }
            _fingerprinted = true;
            Find();
            string arrays = Expect(_getArray, GetArrayCode);
            _directCode = Expect(_send, SendCode) ?? Queue() ?? arrays;
            _nestedCode = Expect(_write, WriteCode) ?? arrays;
            _broadcastCode = Expect(_route, RouteCode) ?? Expect(_invoke, InvokeCode) ?? Expect(_serialize, SerializeCode)
                ?? Expect(_sendPackage, SendPackageCode) ?? Expect(_send, SendCode) ?? _nestedCode;
        }

        private static void Find()
        {
            Type pkg = typeof(ZPackage);
            Type rpc = typeof(ZRpc);
            _send = AccessTools.Method(typeof(ZSteamSocket), nameof(ZSteamSocket.Send), new[] { pkg });
            _queue = AccessTools.Method(typeof(ZSteamSocket), nameof(ZSteamSocket.SendQueuedPackages), Type.EmptyTypes);
            _getArray = AccessTools.Method(pkg, nameof(ZPackage.GetArray), Type.EmptyTypes);
            _write = AccessTools.Method(pkg, nameof(ZPackage.Write), new[] { pkg });
            _route = AccessTools.Method(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RouteRPC), new[] { typeof(ZRoutedRpc.RoutedRPCData) });
            _invoke = AccessTools.Method(rpc, nameof(ZRpc.Invoke), new[] { typeof(string), typeof(object[]) });
            _serialize = AccessTools.Method(rpc, nameof(ZRpc.Serialize), new[] { typeof(object[]), pkg.MakeByRefType() });
            _sendPackage = AccessTools.Method(rpc, nameof(ZRpc.SendPackage), new[] { pkg });
        }

        private static string Expect(MethodBase method, uint expected)
        {
            if (method == null)
            {
                return "a game method it stands in for is missing";
            }
            uint found = GameCode.Fingerprint(method);
            return found == expected
                ? null
                : $"{Name(method)} is not the code it was checked against ({found:x8}, checked {expected:x8})";
        }

        // Either build of the send loop; anything else leaves the direct send off.
        private static string Queue()
        {
            if (_queue == null)
            {
                return "a game method it stands in for is missing";
            }
            uint found = GameCode.Fingerprint(_queue);
            if (found != QueueClientCode && found != QueueServerCode)
            {
                return $"{Name(_queue)} is not the code it was checked against ({found:x8}, checked {QueueClientCode:x8} " +
                    $"and {QueueServerCode:x8})";
            }
            GameServerApi = found == QueueServerCode;
            return null;
        }

        private static string Patched(params MethodBase[] methods)
        {
            foreach (MethodBase method in methods)
            {
                string other = Other(Harmony.GetPatchInfo(method));
                if (other != null)
                {
                    return $"{other} also patches {Name(method)}";
                }
            }
            return null;
        }

        // The first patch not declared by a send patch here, or null.
        private static string Other(Patches info)
        {
            if (info == null)
            {
                return null;
            }
            return Other(info.Prefixes, true) ?? Other(info.Postfixes, false) ?? Other(info.Transpilers, false)
                ?? Other(info.Finalizers, false) ?? Other(info.ILManipulators, false);
        }

        private static string Other(IEnumerable<Patch> patches, bool prefixes)
        {
            foreach (Patch patch in patches)
            {
                Type owner = patch.PatchMethod?.DeclaringType;
                bool allowed = prefixes && Array.IndexOf(AllowedPrefixes, owner?.FullName + "." + patch.PatchMethod?.Name) >= 0;
                if (Array.IndexOf(Mine, owner) < 0 && !allowed)
                {
                    return $"{patch.owner} ({owner?.FullName})";
                }
            }
            return null;
        }

        private static string Name(MethodBase method) => method.DeclaringType?.Name + "." + method.Name;
    }
}
