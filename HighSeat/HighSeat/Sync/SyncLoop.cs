using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The server's send loop for big servers, in place of the game's (ZDOMan.SendZDOToPeers2), which serves one player a
    /// frame in turn and then waits 0.05 s: with 100 players each is served about every 2 s and gets about 5 KB/s,
    /// whatever the send rate. Here every player is served every round. A round starts once the last one has finished,
    /// its changes are all handed out and <see cref="RoundGap"/> s have passed since it started, as the game's. At its
    /// start the players are read again (<see cref="SyncPeers"/>) and the objects changed since the last round are set
    /// aside to hand out (<see cref="Changes"/>). Then, over as many frames as it takes: every player's area is brought up
    /// to date and one player's whole area is queued for the game's own look (<see cref="RoundStart"/>,
    /// <see cref="FullCheck"/>, whose looks run in up to half of each frame's time), the changed objects are handed to the
    /// players near them, and each player gets their package (<see cref="PeerSend"/>, prepared in batches on worker threads
    /// where there are cores for them, <see cref="PrepPool"/>), starting one player further on each round so nobody always
    /// goes last. A frame spends at most <see cref="MsPerFrame"/> ms on all of it, and one player's send more: what is left
    /// goes on the next frame, and once the areas are done at least one player is served a frame, so a round always ends.
    /// Server only, dedicated or hosting, while Big server sync is on (turned off, the game's own loop sends from its own
    /// state, untouched meanwhile); a player's game keeps the game's own loop. If anything here throws, it is logged once
    /// and the game's own loop sends for the rest of the session (the world): a broken send loop must never stop a server
    /// from sending. A stuck worker thread (<see cref="PrepPool.Hung"/>) hands back for as long as the game runs.
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOToPeers2))]
    internal static class SyncLoop
    {
        private const float RoundGap = 0.05f;
        private const double MsPerFrame = 3.0;

        private static readonly long TicksPerFrame = (long)(Stopwatch.Frequency * MsPerFrame / 1000.0);
        private static readonly List<PeerSync> Order = new List<PeerSync>();
        private static readonly Stopwatch Clock = new Stopwatch();
        private static ZDOMan _man;
        private static bool _failed;
        private static bool _inRound;
        private static float _sinceRound;
        private static int _first;
        private static int _next;

        private static bool Prefix(ZDOMan __instance, float dt)
        {
            try
            {
                if (!Usable(__instance))
                {
                    return true;
                }
                Run(__instance, dt);
                return false;
            }
            catch (Exception e)
            {
                Fail(e);
                return true;
            }
        }

        /// <summary>Hands sending back to the game's own loop for the rest of the session, logged once. Never throws.</summary>
        public static void Fail(Exception e)
        {
            if (_failed)
            {
                return;
            }
            _failed = true;
            Changes.Recording = false;
            SyncStats.Active = false;
            try
            {
                HighSeat.Log.LogError($"the send loop failed, so the game's own loop sends for the rest of this session: {e}");
                Changes.Reset();
                LetGo();
            }
            catch (Exception)
            {
                // Already handed back; what is left over is only memory, freed when the world closes.
            }
        }

        /// <summary>The world is closing: stop recording and let go of everything; the next world begins afresh.</summary>
        public static void End()
        {
            Changes.Reset();
            SyncStats.Active = false;
            LetGo();
            _man = null;
        }

        private static void LetGo()
        {
            _inRound = false;
            Order.Clear();
            RoundStart.Clear();
            PeerSend.Clear();
            FullCheck.Clear();
            SyncPeers.Reset();
            Interest.Clear();
        }

        // False on a player's game, with Big server sync off and once the loop failed this session; a new session (world),
        // or the setting turned on again, starts afresh. Never again once a worker thread got stuck.
        private static bool Usable(ZDOMan man)
        {
            if (!ZNet.instance.IsServer() || !Settings.BigServerSync.Value || PrepPool.Hung)
            {
                if (_man != null)
                {
                    End();
                }
                Changes.Recording = false;
                return false;
            }
            if (man != _man)
            {
                Begin(man);
            }
            return !_failed;
        }

        // The session is taken first, so that if starting it throws, the failure stands for the session and is not retried.
        private static void Begin(ZDOMan man)
        {
            _man = man;
            _failed = false;
            LetGo();
            _sinceRound = 0f;
            Interest.Reset(man);
            Changes.Reset();
            Throttle.Reset();
            SyncStats.Reset();
            PrepPool.Begin();
            Premade.Begin();
            RecordChecks.Begin();
        }

        // A frame: the round's start where due, its areas, the hand-out, the queued looks, then players while time is left.
        private static void Run(ZDOMan man, float dt)
        {
            Clock.Restart();
            bool playersOn = man.m_peers.Count > 0;
            Changes.Watch(playersOn);
            _sinceRound += dt;
            if (!_inRound && playersOn && _sinceRound > RoundGap && !Changes.HandingOut && ZoneSystem.instance != null)
            {
                StartRound(man);
            }
            bool areasDone = !_inRound || RoundStart.Run(man, Order, Clock, TicksPerFrame);
            if (areasDone && Changes.HandingOut)
            {
                HandOut(man);
            }
            FullCheck.Run(man, Clock, TicksPerFrame / 2);
            if (_inRound && areasDone)
            {
                Continue(man);
            }
            SyncStats.Frame(Clock.ElapsedTicks);
        }

        private static void StartRound(ZDOMan man)
        {
            _sinceRound = 0f;
            float now = Time.time;
            SyncPeers.Refresh(man);
            BuildOrder(SyncPeers.All);
            if (Order.Count == 0)
            {
                return;
            }
            Throttle.Started(now);
            Changes.Swap();
            RoundStart.Begin(now);
            PeerSend.Share(Order.Count);
            _inRound = true;
        }

        // This round's order: everyone, starting one further on than last round.
        private static void BuildOrder(List<PeerSync> peers)
        {
            Order.Clear();
            _next = 0;
            if (peers.Count == 0)
            {
                return;
            }
            _first = (_first + 1) % peers.Count;
            for (int i = 0; i < peers.Count; i++)
            {
                Order.Add(peers[(_first + i) % peers.Count]);
            }
        }

        private static void HandOut(ZDOMan man)
        {
            long start = Clock.ElapsedTicks;
            Changes.HandOut(man, Clock, TicksPerFrame);
            SyncStats.HandedOut(Clock.ElapsedTicks - start);
        }

        // Serves the players left in this round until the frame's time is spent, at least one a frame.
        private static void Continue(ZDOMan man)
        {
            Records.NewFrame();
            _next = PeerSend.Serve(man, Order, _next, Clock, TicksPerFrame);
            if (_next >= Order.Count)
            {
                _inRound = false;
                Order.Clear();
                SyncStats.Round();
            }
        }
    }
}
