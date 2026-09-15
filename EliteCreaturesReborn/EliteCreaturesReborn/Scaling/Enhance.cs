using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// The large-star enhancement rule in one place: a mutation sitting on a large star has its gain, and only its
    /// gain, multiplied by the biome's <c>large star power</c>. Costs are never multiplied. Two shapes of value need
    /// scaling differently - a stat read as <c>1 + bonus</c> (health, movement, swing speed) scales its bonus, while a
    /// raw magnitude (a percentage, a distance, a flat number) scales whole. Which fields scale for which mutation is
    /// fixed by the specification and lives at each call site; this class only knows how to apply the multiplier.
    /// </summary>
    public static class Enhance
    {
        /// <summary>A stat multiplier read as <c>1 + bonus</c>: enhanced, the bonus above 1 is multiplied.</summary>
        public static float Stat(BiomeRules rules, CreatureTraits traits, Mutation mutation, string field)
        {
            float value = rules.PowerOf(mutation, field);
            return traits.OnLargeStar(mutation) ? 1f + (value - 1f) * rules.LargeStarPower : value;
        }

        /// <summary>A raw magnitude (percent, distance, flat amount): enhanced, the whole value is multiplied.</summary>
        public static float Magnitude(BiomeRules rules, CreatureTraits traits, Mutation mutation, string field)
        {
            float value = rules.PowerOf(mutation, field);
            return traits.OnLargeStar(mutation) ? value * rules.LargeStarPower : value;
        }
    }
}
