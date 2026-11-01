using System.Collections.Generic;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// The part of a round's start that grows with the players, spread over frames within the send loop's frame time:
    /// every player's area brought up to date in <see cref="Interest"/> (a cheap test, unless their zone or simulation
    /// distance changed: then every sector left or taken in, each searched among the players already watching it, which
    /// for a crowd crossing a zone line together all fell in one frame), then the next player's safety-net look queued
    /// (<see cref="FullCheck.SafetyNet"/>). The changed objects are handed out only after that (<see cref="Changes"/>), by
    /// the areas as they now stand, and players are served only after that too, so nobody is served before their area is
    /// set (a player who just joined has none yet). The areas are taken in the round's order, so one who left meanwhile
    /// is passed over.
    /// </summary>
    internal static class RoundStart
    {
        private static int _at;
        private static float _time;
        private static bool _done = true;

        /// <summary>A round starts at this Time.time.</summary>
        public static void Begin(float time)
        {
            _time = time;
            _at = 0;
            _done = false;
        }

        public static void Clear()
        {
            _at = 0;
            _done = true;
        }

        /// <summary>
        /// Brings areas up to date until the clock reaches <paramref name="until"/> (one at least); true once all are and
        /// the safety net is queued.
        /// </summary>
        public static bool Run(ZDOMan man, List<PeerSync> order, Stopwatch clock, long until)
        {
            while (!_done && _at < order.Count)
            {
                PeerSync ps = order[_at++];
                if (!ps.Gone)
                {
                    Interest.Update(man, ps);
                }
                if (_at < order.Count && clock.ElapsedTicks >= until)
                {
                    return false;
                }
            }
            if (!_done)
            {
                _done = true;
                FullCheck.SafetyNet(SyncPeers.All, _time);
            }
            return true;
        }
    }
}
