using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Who is sent the player history, and when. The game (ZNet.SendHistoricalPlayerList: when the world starts, at each
    /// join, and at the next 2 s beat after any change to the history, ZNet.SendPeriodicData) brings the history up to
    /// date (UpdatePlayerHistory), sends the whole list to every ready player, and raises ZNet.HistoricalPlayerListSent
    /// with the whole history (on the server, the host's own PlayStation block check). A join is two or three sends to
    /// everyone (the joining player's entry changes twice: at the join, with no display name yet, and once their game
    /// sends it), each the whole list. Here the history is brought up to date the same way, the list is the one
    /// <see cref="HistoryOut"/> makes, and the event is raised with the whole history as the game raises it; but the list
    /// goes at once only to a player who holds none yet (one who just joined; every player when this starts). A player
    /// holding an older list gets the newest from <see cref="Spread"/>, run after every player list send (every 2 s): at
    /// most once every <see cref="SpreadSeconds"/>, and not while the game is about to make a newer one (its change flag
    /// is set) unless a list has waited <see cref="WaitSeconds"/>. So a lone join costs everyone else one list, a few
    /// seconds after it (once the newcomer's name is in), and a burst of joins (a restart: dozens in a minute) one list
    /// per <see cref="SpreadSeconds"/>, instead of two or three whole lists per join. A player already holding the newest
    /// list is never sent it again. A player's game reads whatever list arrives in full (RPC_HistoricalPlayerList clears
    /// its copy and reads the new one), so a later copy only means the newest names reach them a few seconds later (the
    /// author box and the PlayStation check use them; the place their own pieces record is in their first list). Server
    /// only, while the game's code is what it was checked against and no other mod patches it (<see cref="ListChecks"/>);
    /// on any error, the game's own method (the whole list to everyone) for the rest of the session.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SendHistoricalPlayerList))]
    internal static class HistorySend
    {
        /// <summary>The least time between two lists to the players who hold one.</summary>
        private const float SpreadSeconds = 10f;

        /// <summary>The longest a newer list waits for the next one the game is about to make.</summary>
        private const float WaitSeconds = 30f;

        private const string Rpc = "HistoricalPlayerList";
        private static readonly FieldInfo SentEvent = AccessTools.Field(typeof(ZNet), "HistoricalPlayerListSent");
        private static readonly Dictionary<ZNetPeer, int> Held = new Dictionary<ZNetPeer, int>();
        private static readonly Dictionary<ZNetPeer, int> Kept = new Dictionary<ZNetPeer, int>();
        private static float _lastSpread = float.NegativeInfinity;
        private static float _waitingSince = float.PositiveInfinity;
        private static int _spreadVersion;

        /// <summary>Whether the game's event field was found (the event is raised through it).</summary>
        public static bool EventFound => SentEvent != null && SentEvent.IsStatic;

        public static void Reset()
        {
            Held.Clear();
            _lastSpread = float.NegativeInfinity;
            _waitingSince = float.PositiveInfinity;
            _spreadVersion = 0;
            HistoryOut.Reset();
        }

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZNet __instance, bool __runOriginal)
        {
            if (!__runOriginal || !ListGate.Active(ListGate.Part.Send, __instance))
            {
                return true;
            }
            try
            {
                Send(__instance);
                return false;
            }
            catch (Exception e)
            {
                ListGate.Fail(ListGate.Part.Send, e);
                return true;
            }
        }

        private static void Send(ZNet net)
        {
            net.UpdatePlayerHistory();
            if (net.m_peers.Count > 0)
            {
                HistoryOut.Build(net);
                if (HistoryOut.Version != _spreadVersion && float.IsPositiveInfinity(_waitingSince))
                {
                    _waitingSince = Time.unscaledTime;
                }
                foreach (ZNetPeer peer in net.m_peers)
                {
                    if (peer.IsReady() && !Held.ContainsKey(peer) && HistoryOut.Reaches(net, peer))
                    {
                        Give(peer);
                    }
                }
            }
            Action<IReadOnlyList<ZNet.CrossNetworkUserInfo>> sent =
                SentEvent.GetValue(null) as Action<IReadOnlyList<ZNet.CrossNetworkUserInfo>>;
            sent?.Invoke(ZNet.m_world.m_playerHistory);
        }

        /// <summary>Players holding an older list get the newest one, when it is due (see the class summary).</summary>
        public static void Spread(ZNet net, float now)
        {
            if (!HistoryOut.Built || HistoryOut.Version == _spreadVersion || now - _lastSpread < SpreadSeconds ||
                net.m_historicalPlayerListUpdated && now - _waitingSince < WaitSeconds)
            {
                return;
            }
            foreach (ZNetPeer peer in net.m_peers)
            {
                if (peer.IsReady() && Held.TryGetValue(peer, out int version) && version != HistoryOut.Version &&
                    HistoryOut.Reaches(net, peer))
                {
                    Give(peer);
                    _lastSpread = now;
                }
            }
            _spreadVersion = HistoryOut.Version;
            _waitingSince = float.PositiveInfinity;
            Prune(net);
        }

        private static void Give(ZNetPeer peer)
        {
            peer.m_rpc.Invoke(Rpc, HistoryOut.Package);
            Held[peer] = HistoryOut.Version;
        }

        // Players who left are forgotten once there are more held lists than players.
        private static void Prune(ZNet net)
        {
            if (Held.Count <= net.m_peers.Count)
            {
                return;
            }
            Kept.Clear();
            foreach (ZNetPeer peer in net.m_peers)
            {
                if (Held.TryGetValue(peer, out int version))
                {
                    Kept[peer] = version;
                }
            }
            Held.Clear();
            foreach (KeyValuePair<ZNetPeer, int> held in Kept)
            {
                Held[held.Key] = held.Value;
            }
        }
    }
}
