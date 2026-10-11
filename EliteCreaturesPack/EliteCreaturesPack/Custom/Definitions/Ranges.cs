namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>
    /// A range of numbers written as one number (both ends the same) or as <c>[low, high]</c>: a drop's amount, the
    /// heights a flyer keeps to. Checked when read: low is never above high.
    /// </summary>
    public readonly struct NumberRange
    {
        public NumberRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }

        public override string ToString() => Min == Max ? Min.ToString("0.###") : $"[{Min:0.###}, {Max:0.###}]";
    }

    /// <summary>A range of whole numbers, written like a <see cref="NumberRange"/>: how many of an item drop.</summary>
    public readonly struct CountRange
    {
        public CountRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Min { get; }

        public int Max { get; }

        public override string ToString() => Min == Max ? Min.ToString() : $"[{Min}, {Max}]";
    }
}
