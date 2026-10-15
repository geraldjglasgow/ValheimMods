using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The CPU guard: the server's frame time, smoothed over about a second. Above the budget the server takes on no more
    /// creatures (<see cref="Ownership"/>); those it runs stay, so nothing flips back and forth while it recovers.
    /// </summary>
    internal static class FrameGuard
    {
        private const float Smoothing = 0.05f;
        private static float _average = 1f / 60f;
        private static bool _warned;

        public static bool Busy { get; private set; }

        public static void Measure()
        {
            if (!ServerRun.Active)
            {
                return;
            }
            _average += (Time.unscaledDeltaTime - _average) * Smoothing;
            bool busy = _average * 1000f > Settings.FrameBudgetMs.Value;
            if (busy && !_warned)
            {
                HighSeat.Log.LogWarning($"server frames average {_average * 1000f:0} ms, over the {Settings.FrameBudgetMs.Value} ms " +
                    "budget: new creatures are left to players until it recovers");
            }
            _warned = busy;
            Busy = busy;
        }
    }
}
