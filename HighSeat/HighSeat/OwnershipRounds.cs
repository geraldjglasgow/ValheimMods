using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The game's ownership pass (ZDOMan.ReleaseZDOS) spread over frames, on the server while it runs creatures. Every
    /// two seconds the game calls ReleaseNearbyZDOS for the server's own position and then for every player's, all in one
    /// frame; with every player's area loaded each call scans that area (<see cref="Ownership"/>), and 50 players spread
    /// out made a 140-240 ms frame every 2 s (measured 2026-11-01). Here a round starts every RoundSeconds once the last
    /// has finished: the server first, then the players in the game's order, each read where they stand when their turn
    /// comes and passed over if they have left by then. A frame takes turns until BudgetMs is spent, at least one, and at
    /// least enough that a round still ends within RoundSeconds at 30 frames a second. Each round is one pass for
    /// <see cref="Ownership"/> (a zone done once) and <see cref="SharedRule"/> (its players listed once).
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseZDOS))]
    internal static class OwnershipRounds
    {
        private const float RoundSeconds = 2f;
        private const double BudgetMs = 2.0;
        private const int FramesPerRound = 60;

        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;
        private static readonly List<ZDOMan.ZDOPeer> Turns = new List<ZDOMan.ZDOPeer>();
        private static bool _serverTurn;
        private static int _next;
        private static float _timer;

        /// <summary>Counts the rounds: a new number is a new pass.</summary>
        public static int Round { get; private set; }

        private static bool Prefix(ZDOMan __instance, float dt)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            _timer += dt;
            if (!Running() && _timer > RoundSeconds)
            {
                Start(__instance);
            }
            Take(__instance);
            return false;
        }

        private static bool Running() => _serverTurn || _next < Turns.Count;

        private static void Start(ZDOMan man)
        {
            _timer = 0f;
            Round++;
            Turns.Clear();
            Turns.AddRange(man.m_peers);
            _next = 0;
            _serverTurn = true;
            Ownership.BeginRound();
        }

        private static void Take(ZDOMan man)
        {
            long start = Stopwatch.GetTimestamp();
            int least = (Turns.Count + FramesPerRound) / FramesPerRound;
            for (int taken = 0; Running() && (taken < least || (Stopwatch.GetTimestamp() - start) / TicksPerMs < BudgetMs); taken++)
            {
                One(man);
            }
            if (!Running())
            {
                Turns.Clear();
                _next = 0;
            }
        }

        private static void One(ZDOMan man)
        {
            if (_serverTurn)
            {
                _serverTurn = false;
                man.ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), ZDOMan.GetSessionID());
                return;
            }
            ZDOMan.ZDOPeer peer = Turns[_next++];
            if (man.m_peers.Contains(peer))
            {
                man.ReleaseNearbyZDOS(peer.m_peer.m_refPos, peer.m_peer.m_uid);
            }
        }
    }
}
