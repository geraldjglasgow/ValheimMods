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

        /// <summary>A boss's aspect; always None for a creature. Rolled once with the stars and stored beside them.</summary>
        public Aspect Aspect;

        /// <summary>The aspects a boss carries beside its headline <see cref="Aspect"/>, packed one bit per aspect value -
        /// Bountiful's two. 0 for every creature and for a boss with one aspect.</summary>
        public int ExtraAspects;

        /// <summary>True on the copies a Phantom boss brings - never on the boss itself. Read from the copy's ZDO.</summary>
        public bool PhantomCopy;

        /// <summary>The world tier a wild creature was rolled at; 0 for tier 0, and for a newborn, boss or console spawn.</summary>
        public int Tier;

        public CreatureTraits(int stars, int mask)
        {
            Stars = stars;
            Mask = mask;
        }

        public CreatureTraits(int stars, Aspect aspect)
        {
            Stars = stars;
            Aspect = aspect;
        }

        public bool Has(Mutation mutation) => (Mask & (1 << (int)mutation)) != 0;

        public void Add(Mutation mutation) => Mask |= 1 << (int)mutation;

        public void Remove(Mutation mutation) => Mask &= ~(1 << (int)mutation);

        public bool Any => Mask != 0;

        /// <summary>True when the boss carries this aspect, as its headline or beside it. Never true for None.</summary>
        public bool HasAspect(Aspect aspect) =>
            aspect != Aspect.None && (Aspect == aspect || (ExtraAspects & (1 << (int)aspect)) != 0);

        /// <summary>Every aspect the boss carries, headline first, then the extras in catalog order; empty for None.</summary>
        public IEnumerable<Aspect> Aspects()
        {
            if (Aspect != Aspect.None)
            {
                yield return Aspect;
            }
            foreach (Aspect extra in AspectCatalog.InOrder)
            {
                if (extra != Aspect && (ExtraAspects & (1 << (int)extra)) != 0)
                {
                    yield return extra;
                }
            }
        }

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
