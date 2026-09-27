namespace OpenKeep.Homestead
{
    /// <summary>What one area repair did with the pieces touching the repaired one: the message count and the log line.</summary>
    public sealed class RepairTally
    {
        private readonly int[] counts = new int[5];

        public RepairTally(int touching)
        {
            Touching = touching;
        }

        public int Touching { get; }

        /// <summary>The cap was reached while touching pieces were still unchecked.</summary>
        public bool Capped { get; set; }

        public int Repaired => counts[(int)RepairOutcome.Repaired];

        public void Count(RepairOutcome outcome) => counts[(int)outcome]++;

        public override string ToString()
        {
            string text = $"{Repaired} of {Touching} touching pieces repaired, {counts[(int)RepairOutcome.Undamaged]} not damaged, "
                + $"{counts[(int)RepairOutcome.NoStation]} without their station in range, {counts[(int)RepairOutcome.Warded]} in a ward, "
                + $"{counts[(int)RepairOutcome.NotAPiece]} not pieces";
            return Capped ? text + $"; stopped at the cap of {RepairArea.MaxRepairs}" : text;
        }
    }
}
