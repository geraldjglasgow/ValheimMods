using System;
using System.Collections.Generic;
using HarmonyLib;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// The player history brought up to date with the players online (ZNet.UpdatePlayerHistory), the game's own steps
    /// with each lookup in one step. The game runs it on the server at every player list update (every 2 s) and before
    /// every history send: for each player online, in the list's order, it first adds them to the players met this
    /// session (ZNet.m_recentPlayers, once each, never the server's own platform user) and then finds their id in the
    /// history (FindIndex, a new closure per player, reading the history up to them): an entry that differs from the
    /// player's info (a new name, a new server-assigned name) is replaced in place, a player not in it is added at the
    /// end, and either marks the history changed (m_historicalPlayerListUpdated, which makes the game send it again).
    /// The players newly met go to the platform's recent players (Steam's "played with") in one call at the end. Here the
    /// players met are mirrored in a set kept in step with the game's list (its change count, <see cref="ListVersion{T}"/>),
    /// the history is found through <see cref="HistoryIndex"/>, and the list of newly met players is reused: the same
    /// changes in the same order, no garbage unless a player is newly met. Server only (a player's game runs the game's
    /// own code when a history arrives), while the game's code is what it was checked against and no other mod patches
    /// it (<see cref="ListChecks"/>); on any error, the game's own method for the rest of the session.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.UpdatePlayerHistory))]
    internal static class FastHistory
    {
        private static readonly HashSet<PlatformUserID> Met = new HashSet<PlatformUserID>(IdKeys.Comparer);
        private static readonly List<PlatformUserID> NewlyMet = new List<PlatformUserID>();
        private static List<PlatformUserID> _metList;
        private static int _metVersion;

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZNet __instance, bool __runOriginal)
        {
            if (!__runOriginal || !ListGate.Active(ListGate.Part.History, __instance))
            {
                return true;
            }
            try
            {
                Update(__instance);
                return false;
            }
            catch (Exception e)
            {
                ListGate.Fail(ListGate.Part.History, e);
                return true;
            }
        }

        // The platform reads come first: the loop after them only touches the two lists and the index.
        private static void Update(ZNet net)
        {
            List<ZNet.CrossNetworkUserInfo> history = ZNet.m_world.m_playerHistory;
            HistoryIndex.Sync(history);
            SyncMet(net.m_recentPlayers);
            PlatformUserID local = LocalIds.Id();
            IMatchmakingProvider matchmaking = PlatformManager.DistributionPlatform.MatchmakingProvider;
            NewlyMet.Clear();
            foreach (ZNet.PlayerInfo player in net.m_players)
            {
                Meet(net, player.m_userInfo.m_id, local);
                Record(net, history, player.m_userInfo);
            }
            if (matchmaking != null && NewlyMet.Count > 0)
            {
                matchmaking.AddRecentPlayers(NewlyMet.ToArray());
            }
        }

        private static void Meet(ZNet net, PlatformUserID id, PlatformUserID local)
        {
            if (!Met.Contains(id) && id != local)
            {
                NewlyMet.Add(id);
                net.m_recentPlayers.Add(id);
                Met.Add(id);
                _metVersion = ListVersion<PlatformUserID>.Of(net.m_recentPlayers);
            }
        }

        private static void Record(ZNet net, List<ZNet.CrossNetworkUserInfo> history, ZNet.CrossNetworkUserInfo info)
        {
            int at = HistoryIndex.IndexOf(info.m_id);
            if (at < 0)
            {
                history.Add(info);
                HistoryIndex.Appended();
                net.m_historicalPlayerListUpdated = true;
                return;
            }
            ZNet.CrossNetworkUserInfo before = history[at];
            if (before != info)
            {
                history[at] = info;
                HistoryIndex.Replaced(at, before.m_displayName);
                net.m_historicalPlayerListUpdated = true;
            }
        }

        private static void SyncMet(List<PlatformUserID> list)
        {
            int version = ListVersion<PlatformUserID>.Of(list);
            if (ReferenceEquals(list, _metList) && version == _metVersion)
            {
                return;
            }
            Met.Clear();
            foreach (PlatformUserID id in list)
            {
                Met.Add(id);
            }
            _metList = list;
            _metVersion = version;
        }
    }
}
