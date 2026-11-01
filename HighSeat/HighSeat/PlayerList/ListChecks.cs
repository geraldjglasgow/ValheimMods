using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// Whether each player list part may stand in for the game's code (<see cref="ListGate.Part"/>): every game method it
    /// replaces or relies on must be the code it was checked against (its <see cref="ListCode"/> fingerprint equals the
    /// value taken from Valheim 1.0.21, see the constants), and no other mod may patch a method whose code it
    /// skips, so a mod that changes the player list or the history keeps the game's own path (and its own change). One
    /// exception: another mod's postfix on ZNet.UpdatePlayerList still runs after <see cref="PlayerRows"/>, on the same
    /// list the game's code would have made (ServerDevcommands fills in private positions for admins there), so it does
    /// not stop it. The history send also checks the code a player's game runs on the list it gets: how it reads it
    /// (RPC_HistoricalPlayerList, then UpdatePlayerHistory), how a piece records its builder's place (Piece.SetCreator),
    /// and that the place holder's id reads back (PlatformUserID.TryParse); a game update that changes any of them leaves
    /// the whole list to the game. Fingerprints are taken once per process (the game's code does not change while it
    /// runs), patches at every check (a mod can patch later). The values come from the client's game: should the
    /// dedicated server's build differ in one, that part stays off there and the log gives the value to check. Each answer
    /// is null to go ahead, or why not, for the log.
    /// </summary>
    internal static class ListChecks
    {
        private const uint RowsCode = 0x635d18e8;
        private const uint SteamHostCode = 0xb0dba56f;
        private const uint PlayFabHostCode = 0x07f8780b;
        private const uint NamesCode = 0x5b781401;
        private const uint MatchesCode = 0x44f7edcc;
        private const uint HistoryCode = 0x97f65130;
        private const uint SendCode = 0x4a29ae83;
        private const uint ReceiveCode = 0xa0c38f11;
        private const uint CreatorCode = 0x6cf9cda6;
        private const uint ParseCode = 0xbb91d99e;
        private const string Unchecked = "its game code was not checked";

        private static readonly Type[] Mine = { typeof(PlayerRows), typeof(FastNames), typeof(FastHistory), typeof(HistorySend) };
        private static readonly string[] Code = { Unchecked, Unchecked, Unchecked, Unchecked, Unchecked };
        private static MethodBase _rows, _steamHost, _playFabHost, _names, _matches, _history, _send, _receive, _creator, _parse;
        private static bool _fingerprinted;

        public static string Verdict(ListGate.Part part)
        {
            Fingerprints();
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
        private static void Fingerprints()
        {
            if (_fingerprinted)
            {
                return;
            }
            _fingerprinted = true;
            Find();
            string index = ListVersion<ZNet.CrossNetworkUserInfo>.Ready ? null : "the runtime's lists keep no change count";
            string met = ListVersion<PlatformUserID>.Ready ? null : "the runtime's lists keep no change count";
            Code[(int)ListGate.Part.Rows] = Expect(_rows, RowsCode);
            Code[(int)ListGate.Part.HostNames] = Expect(_steamHost, SteamHostCode) ?? Expect(_playFabHost, PlayFabHostCode);
            Code[(int)ListGate.Part.Names] = Expect(_names, NamesCode) ?? Expect(_matches, MatchesCode) ?? index;
            Code[(int)ListGate.Part.History] = Expect(_history, HistoryCode) ?? index ?? met;
            Code[(int)ListGate.Part.Send] = Expect(_send, SendCode) ?? Expect(_receive, ReceiveCode) ??
                Expect(_history, HistoryCode) ?? Expect(_creator, CreatorCode) ?? Expect(_parse, ParseCode) ?? index ??
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

        private static string Expect(MethodBase method, uint expected)
        {
            if (method == null)
            {
                return "a game method it relies on is missing";
            }
            uint found = ListCode.Fingerprint(method);
            return found == expected
                ? null
                : $"{Name(method)} is not the code it was checked against ({found:x8}, checked {expected:x8})";
        }

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
