namespace EarthWright.History
{
    /// <summary>
    /// Numbers each press of the primary (place) button, so every edit sent while one press is held - a dragged
    /// stroke, the brush's hold-to-repeat - joins the same undo step. Sampled every frame and again just before an
    /// edit is recorded, because the game may place in the same frame the button went down, before our own update.
    /// </summary>
    internal static class PressTracker
    {
        private static bool wasHeld;
        private static int serial;

        public static void Sample()
        {
            bool held = ZInput.GetButton("Attack") || ZInput.GetButton("JoyPlace");
            if (held && !wasHeld)
                serial++;
            wasHeld = held;
        }

        /// <summary>The number of the press being held now, or -1 when the button is up.</summary>
        public static int Current
        {
            get
            {
                Sample();
                return wasHeld ? serial : -1;
            }
        }
    }
}
