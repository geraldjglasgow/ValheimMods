using EliteCrafting.Affixes;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// The item's pending sigil, resolved against the rules, and what it steers (sigils.md section 2). The
    /// <c>sigil</c> verb sets it; every other verb asks this struct what it steers. A sigil whose id is gone, disabled,
    /// or not a sigil is dormant: it never steers, is never spent here, and a new sigil replaces it (SIG-8).
    /// </summary>
    internal readonly struct PendingSigil
    {
        private PendingSigil(StoneDef? def)
        {
            Def = def;
        }

        /// <summary>The live sigil definition; null when none is pending or it is dormant.</summary>
        public StoneDef? Def { get; }

        public bool IsLive => Def != null;

        public string Name => Def == null ? "" : Words.Localize(Def.Name);

        public static PendingSigil Resolve(ItemState state, RuleSet rules)
        {
            StoneDef? def = rules.Stone(state.SigilId);
            return new PendingSigil(def != null && def.Enabled && def.Verb == StoneVerb.Sigil ? def : null);
        }

        /// <summary>The steering matrix: Preservation on swap / reroll_affixes / reroll_values / imbue (ESS-7); a category sigil on
        /// every verb that adds; Culling on remove / swap.</summary>
        public bool Steers(StoneVerb verb)
        {
            switch (Def?.Steer ?? SigilSteer.None)
            {
                case SigilSteer.Preserve:
                    return verb == StoneVerb.Swap || verb == StoneVerb.RerollAffixes || verb == StoneVerb.RerollValues
                        || verb == StoneVerb.Imbue;
                case SigilSteer.Category:
                    return verb == StoneVerb.Promote || verb == StoneVerb.Add || verb == StoneVerb.Swap
                        || verb == StoneVerb.RerollAffixes || verb == StoneVerb.Gamble;
                case SigilSteer.Cull:
                    return verb == StoneVerb.Remove || verb == StoneVerb.Swap;
                default:
                    return false;
            }
        }

        public bool Preserves(StoneVerb verb) => Def?.Steer == SigilSteer.Preserve && Steers(verb);

        public bool Culls(StoneVerb verb) => Def?.Steer == SigilSteer.Cull && Steers(verb);

        /// <summary>The category this verb's roll is steered toward, or null.</summary>
        public AffixCategory? CategoryFor(StoneVerb verb) =>
            Def?.Steer == SigilSteer.Category && Steers(verb) ? Def.SteerCategory : null;

        /// <summary>
        /// Finishes a successful use: a sigil the verb steers is spent, and with <c>sigils.consume_on_unsteered</c> a
        /// live sigil is spent by any successful stone (SIG-1). A steerable verb that did not use it (a Chance fizzle)
        /// leaves it. The clearing goes into the dry-run state itself, so the one write commits both. A refusal never
        /// reaches this.
        /// </summary>
        public StoneResult Finish(StoneVerb verb, StoneResult success, RuleSet rules)
        {
            bool spends = (Steers(verb) && !success.SigilUnused) || rules.Economy.Sigils.ConsumeOnUnsteered;
            // The sigil verb sets the pending sigil itself (it refuses while a live one waits), so it never spends one.
            if (!IsLive || success.State == null || verb == StoneVerb.Sigil || !spends)
            {
                return success;
            }
            ItemState cleared = success.State.ToBuilder().SetSigil(null).Build();
            return success.WithSigilSpent(cleared, Name);
        }
    }
}
