namespace EliteCrafting.Rules
{
    /// <summary>
    /// One immutable snapshot of the running rules: both YAML families, stamped with a generation number. Swapped
    /// atomically in <see cref="ActiveRules.Current"/>; a reader that holds a snapshot sees a consistent pair even while a
    /// reload replaces it. Item states resolved against an older generation re-resolve on their next read.
    /// </summary>
    public sealed class RuleSet
    {
        public static readonly RuleSet Empty = new RuleSet(AffixRules.Empty, EconomyRules.Empty, 0);

        public RuleSet(AffixRules affixes, EconomyRules economy, int generation)
        {
            Affixes = affixes;
            Economy = economy;
            Generation = generation;
        }

        public AffixRules Affixes { get; }
        public EconomyRules Economy { get; }
        public int Generation { get; }

        public AffixDef? Affix(string? id) => Affixes.Get(id);

        public RarityDef? Rarity(string? id) => Economy.Rarity(id);

        public StoneDef? Stone(string? id) => Economy.Stone(id);
    }
}
