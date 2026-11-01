namespace HighSeatPlus
{
    /// <summary>
    /// What this game received in the delta format since it connected, for checking from DevBridge with /eval (for
    /// example HighSeatPlus.DeltaClientStats.Deltas). A few additions per package.
    /// </summary>
    public static class DeltaClientStats
    {
        /// <summary>Delta packages received, objects taken as deltas, and objects taken whole in them.</summary>
        public static int Packages;

        public static int Deltas;

        public static int Whole;

        /// <summary>Objects asked for whole again because a delta did not fit this game's copy.</summary>
        public static int Asked;

        /// <summary>True once this connection gave up on deltas (a package it could not read).</summary>
        public static bool GaveUp;

        /// <summary>Objects whose revisions are noted as the server sent them.</summary>
        public static int Held => HeldCopies.Count;

        internal static void Received(int deltas, int whole)
        {
            Packages++;
            Deltas += deltas;
            Whole += whole;
        }

        internal static void CountAsked(int count) => Asked += count;

        internal static void Reset()
        {
            Packages = 0;
            Deltas = 0;
            Whole = 0;
            Asked = 0;
            GaveUp = false;
        }
    }
}
