using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// A player's server-assigned name (ZNet.GetUniqueDisplayName: "Name", or "Name#2" when the name is not unique), the
    /// game's answer worked out from the history's index instead of the whole history. The game calls it for every
    /// player online at every player list update (every 2 s, and at each join and leave) and reads the whole player
    /// history each time (a LINQ Select over it, then over the players online): players times history, and garbage.
    /// The game counts the history entries with the player's display name (ordinal ==), in order; the place of the first
    /// one that is also the player's id gives the number (one plus the same-named entries before it); when no entry is
    /// both, it counts the players online the same way after them (the player is always one of those). "Name#N" when more
    /// than one entry had the name, else the name. Here the same count reads only the history entries with that name
    /// (<see cref="HistoryIndex"/>, in order) and then, only when needed, the players online: the same answer, in steps
    /// of the few entries sharing the name, and the "Name#N" text is made once per name and number and kept. Server only,
    /// while the game's code is what it was checked against and no other mod patches it or CheckForMatches
    /// (<see cref="ListChecks"/>); on any error, the game's own method for the rest of the session.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.GetUniqueDisplayName))]
    internal static class FastNames
    {
        private static readonly Dictionary<string, List<string>> Numbered =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private static readonly List<string> NumberedNull = new List<string>();

        // The game's three counters: whether the id was met, its place among the same-named, how many share the name.
        private struct Tally
        {
            public bool Found;
            public int Position;
            public int Matching;

            public void Count(PlatformUserID candidate, PlatformUserID id)
            {
                Matching++;
                if (!Found)
                {
                    if (candidate == id)
                    {
                        Found = true;
                    }
                    else
                    {
                        Position++;
                    }
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZNet __instance, ZNet.CrossNetworkUserInfo userInfo, ref string __result, bool __runOriginal)
        {
            if (!__runOriginal || !ListGate.Active(ListGate.Part.Names, __instance))
            {
                return true;
            }
            try
            {
                __result = Unique(__instance, userInfo);
                return false;
            }
            catch (Exception e)
            {
                ListGate.Fail(ListGate.Part.Names, e);
                return true;
            }
        }

        private static string Unique(ZNet net, ZNet.CrossNetworkUserInfo user)
        {
            List<ZNet.CrossNetworkUserInfo> history = ZNet.m_world.m_playerHistory;
            HistoryIndex.Sync(history);
            Tally tally = new Tally { Position = 1 };
            for (int at = HistoryIndex.FirstNamed(user.m_displayName); at >= 0; at = HistoryIndex.NextNamed(at))
            {
                tally.Count(history[at].m_id, user.m_id);
            }
            if (!tally.Found)
            {
                Online(net, user, ref tally);
            }
            if (!tally.Found)
            {
                ZLog.LogError($"Couldn't find matching ID to user {user} in player history!");
            }
            return tally.Matching > 1 ? Number(user.m_displayName, tally.Position) : user.m_displayName;
        }

        private static void Online(ZNet net, ZNet.CrossNetworkUserInfo user, ref Tally tally)
        {
            foreach (ZNet.PlayerInfo player in net.m_players)
            {
                if (player.m_userInfo.m_displayName == user.m_displayName)
                {
                    tally.Count(player.m_userInfo.m_id, user.m_id);
                }
            }
        }

        // The game's $"{name}#{position}": the position is never below 1, so its digits do not depend on the culture.
        private static string Number(string name, int position)
        {
            List<string> numbers = NumberedNull;
            if (name != null && !Numbered.TryGetValue(name, out numbers))
            {
                numbers = new List<string>();
                Numbered.Add(name, numbers);
            }
            while (numbers.Count <= position)
            {
                numbers.Add(null);
            }
            return numbers[position] ?? (numbers[position] = name + "#" + position.ToString(CultureInfo.InvariantCulture));
        }
    }
}
