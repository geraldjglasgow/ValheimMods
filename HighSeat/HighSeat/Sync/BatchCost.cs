using System;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// How many players the next batch of sends takes (<see cref="PeerSend"/>): as many as fit the frame's time left at
    /// the cost measured so far. A batch costs its preparation, spread over the threads that prepare it
    /// (<see cref="PrepPool"/>: a wave of one player per thread at a time), and then each player's package on the main
    /// thread. Both are measured on every batch, per player, and smoothed (<see cref="Smoothing"/>), starting from
    /// <see cref="PrepGuessMs"/> and <see cref="SendGuessMs"/> (a send with 100 players in a crowd was about 0.32 ms, half
    /// of it preparation, 2026-10-31). The time left can be none: the caller still sends one batch of one player a frame.
    /// </summary>
    internal static class BatchCost
    {
        private const double PrepGuessMs = 0.16;
        private const double SendGuessMs = 0.16;
        private const double Smoothing = 0.2;

        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;
        private static double _prep = PrepGuessMs * TicksPerMs;
        private static double _send = SendGuessMs * TicksPerMs;

        /// <summary>Players that fit in this many Stopwatch ticks on this many threads, at most <paramref name="most"/>.</summary>
        public static int Players(long ticks, int lanes, int most)
        {
            if (ticks <= 0)
            {
                return 0;
            }
            int n = (int)Math.Min(most, ticks / (_send + _prep / lanes));
            while (n > 0 && Cost(n, lanes) > ticks)
            {
                n--;
            }
            return n;
        }

        /// <summary>A batch of this many players, prepared on this many threads, took these ticks to prepare and to send.</summary>
        public static void Measured(int players, int lanes, long prepTicks, long sendTicks)
        {
            if (players <= 0)
            {
                return;
            }
            int waves = (players + lanes - 1) / lanes;
            _prep += (prepTicks / (double)waves - _prep) * Smoothing;
            _send += (sendTicks / (double)players - _send) * Smoothing;
        }

        private static double Cost(int players, int lanes)
        {
            int waves = (players + lanes - 1) / lanes;
            return waves * _prep + players * _send;
        }
    }
}
