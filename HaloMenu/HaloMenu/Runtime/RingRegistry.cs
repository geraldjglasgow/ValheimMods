using HaloMenu.Api;

namespace HaloMenu.Runtime
{
    /// <summary>Only one ring across the whole game may be open at a time. Opening a second closes whichever ring
    /// was open with a cancel. Also the single flag the Harmony patches check, so they stay cheap and correct
    /// regardless of which ring (default or API-created) is the one currently open.</summary>
    public static class RingRegistry
    {
        public static RingRuntime CurrentlyOpen { get; private set; }

        public static bool AnyRingOpen => CurrentlyOpen != null;

        /// <summary>Called by a RingRuntime as it starts to open. Closes any other ring first (as a cancel).</summary>
        public static void NotifyOpening(RingRuntime ring)
        {
            if (CurrentlyOpen != null && CurrentlyOpen != ring)
                CurrentlyOpen.CancelFromOutside();
            CurrentlyOpen = ring;
        }

        public static void NotifyClosed(RingRuntime ring)
        {
            if (CurrentlyOpen == ring)
                CurrentlyOpen = null;
        }
    }
}
