namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// One game clip in a sound cue (<see cref="HeadsmanSoundTable"/>): where in the clip it starts, how long after the
    /// cue, its volume, its high- and low-pass (Hz, 0 for none), its pitch, how long it plays, and whether it fades in
    /// (a clip cut at the start would click).
    /// </summary>
    public sealed class HeadsmanSoundLayer
    {
        public readonly string Clip;
        public readonly float Start, Delay, Volume, Low, High, Pitch, Longest;
        public readonly bool FadeIn;

        public HeadsmanSoundLayer(string clip, float start, float delay, float volume, float low, float high, float pitch, float longest, bool fadeIn)
        {
            (Clip, Start, Delay, Volume, Low, High) = (clip, start, delay, volume, low, high);
            (Pitch, Longest, FadeIn) = (pitch, longest, fadeIn);
        }
    }
}
