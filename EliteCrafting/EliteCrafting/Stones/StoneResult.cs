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

        /// <summary>A pending sigil steered this use and is cleared by it (already reflected in <see cref="State"/>).</summary>
        public string? SpentSigilName { get; private set; }

        /// <summary>Reflection: the copy's state; the commit adds a new item carrying it to the inventory.</summary>
        public Affixes.ItemState? CopyState { get; private set; }

        /// <summary>The verb could be steered but did not use the pending sigil (a Chance fizzle): it stays pending.</summary>
        public bool SigilUnused { get; private set; }

        public bool Refused => Refusal != null;

        public static StoneResult Refuse(string id, params string[] words) =>
            new StoneResult(new StoneMessage(id, words), null, null);

        public static StoneResult Success(Affixes.ItemState state, string feedbackId, params string[] words) =>
            new StoneResult(null, state, new StoneMessage(feedbackId, words));

        /// <summary>A success that also creates a copy of the item (Reflection).</summary>
        public static StoneResult Duplicate(Affixes.ItemState original, Affixes.ItemState copy, string feedbackId,
            params string[] words) =>
            new StoneResult(null, original, new StoneMessage(feedbackId, words)) { CopyState = copy };

        /// <summary>A success that did not use the pending sigil (it stays pending unless the owner spends it anyway).</summary>
        public static StoneResult Unsteered(Affixes.ItemState state, string feedbackId, params string[] words) =>
            new StoneResult(null, state, new StoneMessage(feedbackId, words)) { SigilUnused = true };

        /// <summary>The same success with the pending sigil cleared from the state (a copy is untouched: it never has one).</summary>
        public StoneResult WithSigilSpent(Affixes.ItemState state, string sigilName) =>
            new StoneResult(null, state, Feedback) { SpentSigilName = sigilName, CopyState = CopyState, SigilUnused = SigilUnused };
    }
}
