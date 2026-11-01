using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// Whether each player list part may stand in for the game's code (<see cref="ListGate.Part"/>): the game must be the
    /// version it was checked against (<see cref="CheckedGame"/>), and no other mod may patch a method whose code it
    /// skips, so a mod that changes the player list or the history keeps the game's own path (and its own change). One
    /// exception: another mod's postfix on ZNet.UpdatePlayerList still runs after <see cref="PlayerRows"/>, on the same
    /// list the game's code would have made (ServerDevcommands fills in private positions for admins there), so it does
    /// not stop it. The history send also relies on the code a player's game runs on the list it gets: how it reads it
    /// (RPC_HistoricalPlayerList, then UpdatePlayerHistory), how a piece records its builder's place (Piece.SetCreator),
    /// and that the place holder's id reads back (PlatformUserID.TryParse); those must be there too. The version is
    /// checked once per process (the game does not change while it runs), patches at every check (a mod can patch
    /// later). Each answer is null to go ahead, or why not, for the log.
    /// </summary>
    internal static class ListChecks
    {
        private const string Unchecked = "its game code was not checked";

        private static readonly Type[] Mine = { typeof(PlayerRows), typeof(FastNames), typeof(FastHistory), typeof(HistorySend) };
        private static readonly string[] Code = { Unchecked, Unchecked, Unchecked, Unchecked, Unchecked };
        private static MethodBase _rows, _steamHost, _playFabHost, _names, _matches, _history, _send, _receive, _creator, _parse;
        private static bool _checked;

        public static string Verdict(ListGate.Part part)
        {
            Check();
            return Code[(int)part] ?? Patched(part);
        }

        private static string Patched(ListGate.Part part)
        {
            switch (part)
            {
                case ListGate.Part.Rows:
                    return Other(_rows, true);
                case ListGate.Part.HostNames:
                    return Other(_steamHost, false) ?? Other(_playFabHost, false);
                case ListGate.Part.Names:
                    return Other(_names, false) ?? Other(_matches, false);
                case ListGate.Part.History:
                    return Other(_history, false);
                default:
                    return Other(_send, false);
            }
        }

        // Once per process; the verdicts start as "not checked", so a check that throws leaves every part off.
        private static void Check()
        {
            if (_checked)
            {
                return;
            }
            _checked = true;
            Find();
            string version = CheckedGame.Verdict();
            string index = ListVersion<ZNet.CrossNetworkUserInfo>.Ready ? null : "the runtime's lists keep no change count";
            string met = ListVersion<PlatformUserID>.Ready ? null : "the runtime's lists keep no change count";
            Code[(int)ListGate.Part.Rows] = Missing(_rows) ?? version;
            Code[(int)ListGate.Part.HostNames] = Missing(_steamHost, _playFabHost) ?? version;
            Code[(int)ListGate.Part.Names] = Missing(_names, _matches) ?? version ?? index;
            Code[(int)ListGate.Part.History] = Missing(_history) ?? version ?? index ?? met;
            Code[(int)ListGate.Part.Send] = Missing(_send, _receive, _history, _creator, _parse) ?? version ?? index ??
                (HistorySend.EventFound ? null : "the game's HistoricalPlayerListSent event was not found");
        }

        private static void Find()
        {
            Type net = typeof(ZNet);
            _rows = AccessTools.Method(net, nameof(ZNet.UpdatePlayerList), Type.EmptyTypes);
            _steamHost = AccessTools.Method(typeof(ZSteamSocket), nameof(ZSteamSocket.GetHostName), Type.EmptyTypes);
            _playFabHost = AccessTools.Method(typeof(ZPlayFabSocket), nameof(ZPlayFabSocket.GetHostName), Type.EmptyTypes);
            _names = AccessTools.Method(net, nameof(ZNet.GetUniqueDisplayName), new[] { typeof(ZNet.CrossNetworkUserInfo) });
            _matches = AccessTools.Method(net, nameof(ZNet.CheckForMatches), new[]
            {
                typeof(ZNet.CrossNetworkUserInfo), typeof(IEnumerable<(PlatformUserID, string)>), typeof(bool).MakeByRefType(),
                typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
            });
            _history = AccessTools.Method(net, nameof(ZNet.UpdatePlayerHistory), Type.EmptyTypes);
            _send = AccessTools.Method(net, nameof(ZNet.SendHistoricalPlayerList), Type.EmptyTypes);
            _receive = AccessTools.Method(net, nameof(ZNet.RPC_HistoricalPlayerList), new[] { typeof(ZRpc), typeof(ZPackage) });
            _creator = AccessTools.Method(typeof(Piece), nameof(Piece.SetCreator), new[] { typeof(long), typeof(PlatformUserID) });
            _parse = AccessTools.Method(typeof(PlatformUserID), nameof(PlatformUserID.TryParse),
                new[] { typeof(string), typeof(PlatformUserID).MakeByRefType() });
        }

        private static string Missing(params MethodBase[] methods) =>
            Array.IndexOf(methods, null) >= 0 ? "a game method it relies on is missing" : null;

        // The first patch on the method not declared here (a postfix is let through when allowed), or null.
        private static string Other(MethodBase method, bool allowPostfixes)
        {
            Patches info = Harmony.GetPatchInfo(method);
            if (info == null)
            {
                return null;
            }
            string other = Other(info.Prefixes) ?? (allowPostfixes ? null : Other(info.Postfixes)) ??
                Other(info.Transpilers) ?? Other(info.Finalizers) ?? Other(info.ILManipulators);
            return other == null ? null : $"{other} also patches {Name(method)}";
        }

        private static string Other(IEnumerable<Patch> patches)
        {
            foreach (Patch patch in patches)
            {
                Type owner = patch.PatchMethod?.DeclaringType;
                if (Array.IndexOf(Mine, owner) < 0)
                {
                    return $"{patch.owner} ({owner?.FullName})";
                }
            }
            return null;
        }

        private static string Name(MethodBase method) => method.DeclaringType?.Name + "." + method.Name;
    }
}
