using System;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// When each player list part stands in for the game's code: <see cref="PlayerRows"/> (the player list built without
    /// garbage), <see cref="PeerIds"/> (players' host names kept), <see cref="FastNames"/> (server-assigned names from
    /// the history's index), <see cref="FastHistory"/> (the history brought up to date from the index) and
    /// <see cref="HistorySend"/> (the history sent in part, <see cref="HistoryOut"/>). Each is on only on the server
    /// (dedicated or hosting) and while its check passes (<see cref="ListChecks"/>); a player's game is never touched.
    /// These are big server fixes that give every player what the game would (the history: what their game needs of it),
    /// so they need no setting. Checked when a world starts (the first call with a new ZNet, which also clears what the
    /// parts kept for the last world) and every <see cref="CheckSeconds"/> s after, from the parts' own calls (a mod can
    /// patch later); each change is logged once. A part that throws is off for the rest of the session (the world), logged
    /// once, and a new world starts afresh; all are off for the session if the checks themselves throw.
    /// </summary>
    internal static class ListGate
    {
        public enum Part
        {
            Rows,
            HostNames,
            Names,
            History,
            Send,
        }

        private const float CheckSeconds = 10f;
        private static readonly string[] Names =
        {
            "player list built without garbage", "players' host names kept", "unique player names from an index",
            "player history updated from an index", "player history sent in part",
        };
        private static readonly bool[] On = new bool[5];
        private static readonly bool[] Failed = new bool[5];
        private static readonly string[] Said = new string[5];
        private static ZNet _session;
        private static ZNet _broken;
        private static float _nextCheck;

        /// <summary>Whether <paramref name="part"/> stands in for the game's code now. Never throws.</summary>
        public static bool Active(Part part, ZNet net)
        {
            if (!ReferenceEquals(net, _session))
            {
                Begin(net);
            }
            if (Time.unscaledTime >= _nextCheck)
            {
                Check(net);
            }
            return On[(int)part];
        }

        /// <summary>Turns <paramref name="part"/> off for the rest of the session, logged once. Never throws.</summary>
        public static void Fail(Part part, Exception e)
        {
            int i = (int)part;
            On[i] = false;
            if (Failed[i])
            {
                return;
            }
            Failed[i] = true;
            Log($"{Names[i]} failed, so the game's own code runs for the rest of this session: {e}");
        }

        // A new world: everything off until checked for it, failures forgotten, the last world's lists let go.
        private static void Begin(ZNet net)
        {
            _session = net;
            _nextCheck = 0f;
            Array.Clear(On, 0, On.Length);
            Array.Clear(Failed, 0, Failed.Length);
            Array.Clear(Said, 0, Said.Length);
            try
            {
                SeenPlayers.Reset();
                LocalIds.Reset();
                HistorySend.Reset();
            }
            catch (Exception e)
            {
                _broken = net;
                Log($"the player list parts could not start, so the game's own code runs for this session: {e}");
            }
        }

        private static void Check(ZNet net)
        {
            _nextCheck = Time.unscaledTime + CheckSeconds;
            if (ReferenceEquals(net, _broken) || !net.IsServer())
            {
                Array.Clear(On, 0, On.Length);
                return;
            }
            try
            {
                for (int i = 0; i < On.Length; i++)
                {
                    Decide(i, ListChecks.Verdict((Part)i));
                }
            }
            catch (Exception e)
            {
                _broken = net;
                Array.Clear(On, 0, On.Length);
                Log($"the player list checks failed, so the game's own code runs for the rest of this session: {e}");
            }
        }

        // On when nothing stands against it and it has not failed this session; each change of state is logged once.
        private static void Decide(int i, string against)
        {
            if (Failed[i])
            {
                On[i] = false;
                return;
            }
            string state = against ?? "on";
            if (state != Said[i])
            {
                Said[i] = state;
                HighSeat.Log.LogInfo(against == null ? $"{Names[i]}: on" : $"{Names[i]}: left to the game, {against}");
            }
            On[i] = against == null;
        }

        private static void Log(string text)
        {
            try
            {
                HighSeat.Log.LogError(text);
            }
            catch (Exception)
            {
                // Already off; only the log line is lost.
            }
        }
    }
}
