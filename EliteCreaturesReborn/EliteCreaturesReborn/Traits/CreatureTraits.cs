using System.Collections.Generic;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The resolved elite identity of one creature, kept in a single place as the specification asks. It carries a
    /// star count and the set of mutations packed as a bitmask (bit i set means mutation with value i is present).
    /// This is a plain value object: it neither reads the ZDO nor applies effects. TraitStore moves it to and from
    /// the ZDO; everything else reads it.
    /// </summary>
    public sealed class CreatureTraits
    {
        public int Stars;
        public int Mask;

        public CreatureTraits(int stars, int mask)
        {
            Stars = stars;
            Mask = mask;
        }

        public bool Has(Mutation mutation) => (Mask & (1 << (int)mutation)) != 0;

        public void Add(Mutation mutation) => Mask |= 1 << (int)mutation;

        public void Remove(Mutation mutation) => Mask &= ~(1 << (int)mutation);

        public bool Any => Mask != 0;

        /// <summary>Glyphs are drawn in fives: this many large stars (worth five each) precede the small ones.</summary>
        public int LargeGlyphs => Stars / 5;

        /// <summary>
        /// True when this mutation is drawn on a large star. Mutations take glyphs in table order, large glyphs first,
        /// so the first <see cref="LargeGlyphs"/> active mutations sit on large stars and are enhanced.
        /// </summary>
        public bool OnLargeStar(Mutation mutation)
        {
            if (!Has(mutation))
            {
                return false;
            }
            int index = 0;
            foreach (Mutation present in Active())
            {
                if (present == mutation)
                {
                    return index < LargeGlyphs;
                }
                index++;
            }
            return false;
        }

        /// <summary>The present mutations, in specification order, ready for names and star colours.</summary>
        public IEnumerable<Mutation> Active()
        {
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                if (Has(mutation))
                {
                    yield return mutation;
                }
            }
        }

        public int Count
        {
            get
            {
                int n = 0;
                foreach (Mutation _ in Active())
                {
                    n++;
                }
                return n;
            }
        }
    }
}
