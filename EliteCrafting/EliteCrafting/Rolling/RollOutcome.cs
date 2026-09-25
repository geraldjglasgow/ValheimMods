using EliteCrafting.Affixes;

namespace EliteCrafting.Rolling
{
    /// <summary>The result of a roll: the new state (not yet written) or why there is none.</summary>
    public readonly struct RollOutcome
    {
        private RollOutcome(ItemState? state, RollFailure failure)
        {
            State = state;
            Failure = failure;
        }

        /// <summary>The rolled state; hand it to <see cref="ItemState.Write"/> to commit. Null on failure.</summary>
        public ItemState? State { get; }

        public RollFailure Failure { get; }

        public bool Success => Failure == RollFailure.None && State != null;

        public static RollOutcome Ok(ItemState state) => new RollOutcome(state, RollFailure.None);

        public static RollOutcome Fail(RollFailure failure) => new RollOutcome(null, failure);
    }
}
