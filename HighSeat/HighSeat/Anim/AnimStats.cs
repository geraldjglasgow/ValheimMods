#if DEBUG
namespace HighSeat
{
    /// <summary>
    /// The server's creature animation, for load tests (read from DevBridge with /eval, for example
    /// HighSeat.AnimStats.Quiet): how many creatures the last full scan found animating in full, quiet and at rest
    /// (<see cref="AnimScan"/>), and how many changed mode in the last second. <see cref="QuietAllowed"/> set to false
    /// animates every creature the server runs in full, as before quiet creatures were culled, so both can be measured in
    /// one session (HighSeat.AnimStats.QuietAllowed = false); creatures already quiet are back in full within a scan.
    /// Main thread only.
    /// </summary>
    public static class AnimStats
    {
        /// <summary>False: no creature the server runs goes quiet (for comparing frame times).</summary>
        public static bool QuietAllowed = true;

        /// <summary>Creatures animating in full (AlwaysAnimate) in the last full scan.</summary>
        public static int Full;

        /// <summary>Creatures the server runs that were quiet (CullUpdateTransforms) in the last full scan.</summary>
        public static int Quiet;

        /// <summary>Copies the server does not run, at rest, in the last full scan.</summary>
        public static int Rest;

        /// <summary>Creatures whose animators changed mode in the last second.</summary>
        public static int SwitchesPerSecond;

        private static int _full;
        private static int _quiet;
        private static int _rest;
        private static int _switches;
        private static float _second;

        internal static void Count(CreatureAnim.Mode mode)
        {
            if (mode == CreatureAnim.Mode.Full)
            {
                _full++;
            }
            else if (mode == CreatureAnim.Mode.Quiet)
            {
                _quiet++;
            }
            else if (mode == CreatureAnim.Mode.Rest)
            {
                _rest++;
            }
        }

        internal static void ScanDone()
        {
            (Full, Quiet, Rest) = (_full, _quiet, _rest);
            (_full, _quiet, _rest) = (0, 0, 0);
        }

        internal static void Switched() => _switches++;

        internal static void Tick(float now)
        {
            if (now - _second < 1f)
            {
                return;
            }
            SwitchesPerSecond = _switches;
            _switches = 0;
            _second = now;
        }
    }
}
#endif
