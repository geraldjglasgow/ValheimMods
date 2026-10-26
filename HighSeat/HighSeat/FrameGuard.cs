using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The CPU guard: the server's frame time, smoothed over about a second. Busy only once the average has stayed above
    /// the budget for a few seconds, so one long frame (a world save) does not count; measured only while players are on.
    /// While busy the server takes on no more creatures (<see cref="Ownership"/>); those it runs stay, so nothing flips
    /// back and forth while it recovers.
    /// </summary>
    internal static class FrameGuard
    {
        private const float Smoothing = 0.05f;
        private const float SustainSeconds = 3f;
        private static float _average = 1f / 60f;
        private static float _overSince = -1f;

        public static bool Busy { get; private set; }

        public static void Measure()
        {
            if (!ServerRun.Active || ZNet.instance.GetNrOfPlayers() == 0)
            {
                Reset();
                return;
            }
            _average += (Time.unscaledDeltaTime - _average) * Smoothing;
            bool busy = OverLongEnough();
            if (busy && !Busy)
            {
                HighSeat.Log.LogWarning($"server frames average {_average * 1000f:0} ms, over the {Settings.FrameBudgetMs.Value} ms " +
                    "budget: new creatures are left to players until it recovers");
            }
            Busy = busy;
        }

        // True once the average has been over the budget for SustainSeconds without a break.
        private static bool OverLongEnough()
        {
            if (_average * 1000f <= Settings.FrameBudgetMs.Value)
            {
                _overSince = -1f;
                return false;
            }
            if (_overSince < 0f)
            {
                _overSince = Time.unscaledTime;
            }
            return Time.unscaledTime - _overSince >= SustainSeconds;
        }

        private static void Reset()
        {
            _average = 1f / 60f;
            _overSince = -1f;
            Busy = false;
        }
    }
}
