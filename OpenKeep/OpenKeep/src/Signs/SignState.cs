namespace OpenKeep.Signs
{
    /// <summary>The refresh tick's bookkeeping for one loaded container.</summary>
    public sealed class SignState
    {
        /// <summary>Time.time before which nothing is written again (the Update Seconds throttle).</summary>
        public float NextWrite { get; set; }

        /// <summary>The container ZDO's data revision the text was last computed for.</summary>
        public uint Revision { get; set; } = uint.MaxValue;

        /// <summary>The rules generation the sign's place and text were last checked against.</summary>
        public int Rules { get; set; } = -1;
    }
}
