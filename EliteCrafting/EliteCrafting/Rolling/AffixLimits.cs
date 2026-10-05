using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The rarity's prefix and suffix limits during one roll (classes-and-tiers.md section 6): how many of each kind the
    /// item holds (dormant inscriptions count by their definition; orphaned ones count for nothing) and how many it may
    /// hold, the Serpent's overflow added. The rarity is the builder's: a fresh roll and a promotion set it first, so the
    /// new rarity's limits apply. An unknown rarity id limits nothing.
    /// </summary>
    internal sealed class AffixLimits
    {
        private readonly int _maxPrefixes;
        private readonly int _maxSuffixes;
        private int _prefixes;
        private int _suffixes;

        private AffixLimits(int maxPrefixes, int maxSuffixes)
        {
            _maxPrefixes = maxPrefixes;
            _maxSuffixes = maxSuffixes;
        }

        public static AffixLimits For(ItemStateBuilder builder, RollContext context)
        {
            EconomyRules economy = context.Rules.Economy;
            RarityDef? rarity = builder.RarityId == null ? economy.BaseRarity : economy.Rarity(builder.RarityId);
            int overflow = System.Math.Max(context.LimitOverflow, 0);
            AffixLimits limits = rarity == null
                ? new AffixLimits(int.MaxValue / 2, int.MaxValue / 2)
                : new AffixLimits(rarity.MaxPrefixes + overflow, rarity.MaxSuffixes + overflow);
            foreach (AffixRoll roll in builder.Affixes)
            {
                limits.Count(context.Rules.Affixes.Get(roll.Id));
            }
            return limits;
        }

        /// <summary>Whether one more inscription of this kind keeps the item within its limits.</summary>
        public bool Allows(AffixKind kind) =>
            kind == AffixKind.Prefix ? _prefixes < _maxPrefixes : _suffixes < _maxSuffixes;

        /// <summary>Counts an inscription now on the item.</summary>
        public void Count(AffixDef? def)
        {
            if (def == null)
            {
                return;
            }
            if (def.Kind == AffixKind.Prefix)
            {
                _prefixes++;
            }
            else
            {
                _suffixes++;
            }
        }
    }
}
