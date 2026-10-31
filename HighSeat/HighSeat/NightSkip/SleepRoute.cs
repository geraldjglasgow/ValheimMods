using System.Collections.Generic;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The start and end of a night skip the vote passed (<see cref="NightVote"/>) reach only the players who were in bed
    /// when it passed. The game sends both to everyone (routed RPCs "SleepStart" and "SleepStop", to Everybody), which is
    /// right when everyone is in bed but not for a vote. On a player's game (Game.SleepStart, Game.SleepStop) SleepStart
    /// puts the local player to sleep wherever they stand (Player.SetSleeping: InCutscene, so no control and a black
    /// screen while the clock races, in a fight too), and SleepStop ends that sleep (the morning message and the Rested
    /// effect, only for a player who was asleep), detaches the player from whatever they sit on (Player.AttachStop: a
    /// chair, a ship's helm, a saddle, a bed) and saves their profile. So the start goes, as a routed RPC to their peer,
    /// only to the players in bed, and the end only to those same players; the time itself is the server's, so the
    /// morning comes for everyone everywhere. The server always runs the end itself, as the game does: there SleepStop
    /// saves the world (a dedicated server has no player to wake). On a hosted game the host is a player too, put to
    /// sleep and woken only when it was in bed; when it was not, its own SleepStop runs with the host's Player.AttachStop
    /// held back (this class's prefix), so the host keeps its seat while the world save still happens.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AttachStop))]
    internal static class SleepRoute
    {
        private const string Start = "SleepStart";
        private const string Stop = "SleepStop";
        private static readonly List<long> Sleepers = new List<long>();
        private static bool _hostSlept;
        private static bool _keepHostSeated;

        /// <summary>True from a vote's night skip until its end has been sent.</summary>
        public static bool Started { get; private set; }

        /// <summary>Puts to sleep the players in <paramref name="inBed"/> (peer ids; the host's is its session id).</summary>
        public static void Begin(List<long> inBed)
        {
            long own = ZDOMan.GetSessionID();
            Sleepers.Clear();
            Sleepers.AddRange(inBed);
            _hostSlept = Sleepers.Contains(own);
            Started = true;
            SendToPeers(Start, own);
            if (_hostSlept)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(own, Start);
            }
        }

        /// <summary>Wakes the players put to sleep, and runs the server's own end of the night.</summary>
        public static void End()
        {
            long own = ZDOMan.GetSessionID();
            SendToPeers(Stop, own);
            _keepHostSeated = !_hostSlept;
            try
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(own, Stop);
            }
            finally
            {
                _keepHostSeated = false;
                Forget();
            }
        }

        /// <summary>Drops the sleepers without sending anything, for when the game's own rule takes over.</summary>
        public static void Forget()
        {
            Sleepers.Clear();
            _hostSlept = false;
            Started = false;
        }

        // A peer that left meanwhile is skipped by the game's routing; the host is never sent through the network.
        private static void SendToPeers(string method, long own)
        {
            foreach (long sleeper in Sleepers)
            {
                if (sleeper != own)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(sleeper, method);
                }
            }
        }

        private static bool Prefix(Player __instance) => !_keepHostSeated || __instance != Player.m_localPlayer;
    }
}
