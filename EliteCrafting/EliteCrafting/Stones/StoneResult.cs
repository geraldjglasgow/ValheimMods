namespace EliteCrafting.Stones
{
    /// <summary>A player-facing message: <c>$ecf_msg_&lt;id&gt;</c> with up to three already-localized words for $1..$3.</summary>
    internal sealed class StoneMessage
    {
        public StoneMessage(string id, string[] words)
        {
            Id = id;
            Words = words;
        }

        public string Id { get; }
        public string[] Words { get; }

        public string Key => "$ecf_msg_" + Id;
    }

    /// <summary>
    /// What the pipeline decided for one click (applying-stones.md section 2): a refusal, or the dry-run state that
    /// will be written unchanged on commit, with its feedback message. A refusal never changed anything.
    /// </summary>
    internal sealed class StoneResult
    {
        private StoneResult(StoneMessage? refusal, Affixes.ItemState? state, StoneMessage? feedback)
        {
            Refusal = refusal;
            State = state;
            Feedback = feedback;
        }

        public StoneMessage? Refusal { get; }

        /// <summary>The dry-run result: exactly what commits (nothing is rolled twice).</summary>
        public Affixes.ItemState? State { get; }

        public StoneMessage? Feedback { get; }

        public bool Refused => Refusal != null;

        public static StoneResult Refuse(string id, params string[] words) =>
            new StoneResult(new StoneMessage(id, words), null, null);

        public static StoneResult Success(Affixes.ItemState state, string feedbackId, params string[] words) =>
            new StoneResult(null, state, new StoneMessage(feedbackId, words));
    }
}
