using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Whether each send patch may stand in for the game's code: the game must be the version it was checked against
    /// (<see cref="CheckedGame"/>), and no other patch may be on it (a mod that hooks or changes those methods, a
    /// compression mod or another network mod, keeps the game's own path and so keeps working; the patch here stays out
    /// of its way). The Steam send loop (SendQueuedPackages) comes in two builds: a player's game, hosting or not, calls
    /// Steam's client interface (SteamNetworkingSockets), the dedicated server's game, built apart (its ZNet.IsDedicated
    /// is true), the game-server one (SteamGameServerNetworkingSockets). The build running decides which one
    /// <see cref="SteamDirect"/> calls, so it is always the one the game itself calls. The version is checked once per
    /// process (the game does not change while it runs), the patches at every check (a mod can patch later). Each answer
    /// is null to go ahead, or why not, for the log. One patch of another mod is known to be safe beside them: Jotunn's
    /// prefix on ZSteamSocket.Send (<see cref="AllowedPrefixes"/>) only holds back a joining player's first world
    /// packages while its config sync with them runs (it returns false, so the patches here, last and checking
    /// __runOriginal, stand aside) and lets every other send through unchanged; Jotunn is on most modded servers.
    /// </summary>
    internal static class SendChecks
    {
        private const string Unchecked = "its game code was not checked";
        private static readonly string[] AllowedPrefixes = { "Jotunn.Managers.SynchronizationManager+Patches.Socket_Send_Prefix" };

        private static readonly Type[] Mine = { typeof(SteamDirect), typeof(RouteBroadcast), typeof(NestedWrite) };
        private static MethodBase _send, _queue, _getArray, _write, _route, _invoke, _serialize, _sendPackage;
        private static string _direct = Unchecked, _nested = Unchecked, _broadcast = Unchecked;
        private static bool _checked;

        /// <summary>The Steam interface the game's own send loop calls: true for the game-server one.</summary>
        public static bool GameServerApi { get; private set; }

        public static string Direct()
        {
            Check();
            return _direct ?? Patched(_send, _queue, _getArray);
        }

        public static string Nested()
        {
            Check();
            return _nested ?? Patched(_write, _getArray);
        }

        // The broadcast writes what ZRpc.Invoke, Serialize, SendPackage and ZPackage.Write would, and hands it to the
        // socket's own Send, so all of them must be the game's.
        public static string Broadcast()
        {
            Check();
            return _broadcast ?? Patched(_route, _invoke, _serialize, _sendPackage, _write, _getArray, _send);
        }

        // Once per process, from ZNet's update; the verdicts start as "not checked", so a check that throws leaves every
        // feature off.
        private static void Check()
        {
            if (_checked)
            {
                return;
            }
            _checked = true;
            Find();
            GameServerApi = ZNet.instance != null && ZNet.instance.IsDedicated();
            string version = CheckedGame.Verdict();
            _direct = Missing(_send, _queue, _getArray) ?? version;
            _nested = Missing(_write, _getArray) ?? version;
            _broadcast = Missing(_route, _invoke, _serialize, _sendPackage, _write, _getArray, _send) ?? version;
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

        private static string Missing(params MethodBase[] methods) =>
            Array.IndexOf(methods, null) >= 0 ? "a game method it stands in for is missing" : null;

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
