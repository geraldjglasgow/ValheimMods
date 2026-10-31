using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Whether places may be kept right now: the setting is on and the server has stayed within its frame budget
    /// (<see cref="FrameGuard"/>) for the calm time. A busy spell lets every place go (<see cref="KeptZones.LetGo"/>);
    /// they come back only after CalmSeconds without one. Busy again within ForgiveSeconds of coming back doubles the
    /// calm time, up to MaxCalmSeconds, so a server that keeping places pushes over its budget does not load and let go
    /// of them over and over; a busy spell after ForgiveSeconds of keeping starts again from CalmSeconds.
    /// </summary>
    internal static class KeptCalm
    {
        private const float CalmSeconds = 30f;
        private const float MaxCalmSeconds = 600f;
        private const float ForgiveSeconds = 600f;

        private static float _calm = CalmSeconds;
        private static float _busyAt = float.NegativeInfinity;
        private static float _resumedAt = float.NegativeInfinity;
        private static bool _working = true;

        public static void Reset()
        {
            _calm = CalmSeconds;
            _busyAt = float.NegativeInfinity;
            _resumedAt = float.NegativeInfinity;
            _working = true;
        }

        /// <summary>True while places may be kept.</summary>
        public static bool Working()
        {
            float now = Time.unscaledTime;
            if (FrameGuard.Busy)
            {
                if (_working)
                {
                    _calm = now - _resumedAt < ForgiveSeconds ? Mathf.Min(_calm * 2f, MaxCalmSeconds) : CalmSeconds;
                }
                _busyAt = now;
            }
            bool working = Settings.KeptPlaces.Value && now - _busyAt >= _calm;
            if (working && !_working)
            {
                _resumedAt = now;
            }
            _working = working;
            return working;
        }
    }
}
