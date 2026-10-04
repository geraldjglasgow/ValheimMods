namespace DevBridge.Capture
{
    /// <summary>One frame of a burst, shrunk to its cell, and when it was taken (counted from the first after the run).</summary>
    internal sealed class Shot
    {
        internal int Index;
        internal int Frame;        // rendered frames
        internal float Real;       // real seconds
        internal float Game;       // game seconds: Time.time, which Time.timeScale slows, speeds or stops
        internal float TimeScale;
        internal int[] Screen;
        internal Canvas Image;
    }
}
