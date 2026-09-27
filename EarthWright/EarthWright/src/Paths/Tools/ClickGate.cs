using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Lets through one ramp or road click per press of the place button (mouse or gamepad), so a held button, a
    /// bouncing mouse or a repeat feature never sets several points, or sets the points and builds, in one go. A
    /// press counts in its own frame, or for a moment after it when the game places a little later; for that same
    /// moment after a click, no further press counts.
    /// </summary>
    public static class ClickGate
    {
        private const float Window = 0.3f;

        private static float pressedAt = -10f;
        private static float takenAt = -10f;

        /// <summary>Every frame: remembers when the place button last went down.</summary>
        public static void Track()
        {
            if (NewPress)
                pressedAt = Time.time;
        }

        /// <summary>True once per press of the place button.</summary>
        public static bool Take()
        {
            if (!NewPress && Time.time - pressedAt > Window)
                return false;
            takenAt = Time.time;
            pressedAt = -10f;
            return true;
        }

        /// <summary>The place button went down this frame, and not as the press of a click just taken.</summary>
        private static bool NewPress => Time.time - takenAt > Window && (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"));
    }
}
