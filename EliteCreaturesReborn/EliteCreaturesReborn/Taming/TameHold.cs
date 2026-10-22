using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// Where a tame's rolled stars and mutations wait while `15 - Tamed` takes them (<see cref="TameStrip"/>). The
    /// creature's own keys, <c>ecr_stars</c> and <c>ecr_mask</c>, always say what it has now, 0 while taken, because other
    /// mods read them straight from the ZDO: SeneaL UI drew a tame's five taken stars on its nameplate (Discord,
    /// 2026-10-22). The owner moves them aside as the bits are set and back as they clear, and once for a tame from
    /// 3.30.0 to 3.32.0, which kept them in place under the bits.
    /// </summary>
    internal static class TameHold
    {
        private static readonly int OwnStarsHash = TraitKeys.Stars.GetStableHashCode();
        private static readonly int OwnMaskHash = TraitKeys.Mask.GetStableHashCode();
        private static readonly int HeldStarsHash = TraitKeys.TameStars.GetStableHashCode();
        private static readonly int HeldMaskHash = TraitKeys.TameMask.GetStableHashCode();

        /// <summary>The traits as rolled: what the creature has now, with whatever is held aside put back.</summary>
        public static CreatureTraits Rolled(ZDO zdo)
        {
            CreatureTraits traits = TraitStore.Load(zdo);
            int stars = zdo.GetInt(HeldStarsHash);
            if (stars > 0)
            {
                traits.Stars = stars;
            }
            traits.Mask |= zdo.GetInt(HeldMaskHash);
            return traits;
        }

        /// <summary>OWNER: the stars and the mask moved aside for each bit of <paramref name="strip"/> and back for each bit
        /// it lacks; true when anything moved.</summary>
        public static bool Align(ZDO zdo, int strip)
        {
            bool stars = Move(zdo, OwnStarsHash, HeldStarsHash, aside: (strip & TameStrip.Stars) != 0);
            bool mask = Move(zdo, OwnMaskHash, HeldMaskHash, aside: (strip & TameStrip.Mutations) != 0);
            return stars || mask;
        }

        // Aside, the value leaves the creature's key for the held one; back, the other way. The emptied key is set to 0,
        // not removed: a removal is not sent to other machines.
        private static bool Move(ZDO zdo, int own, int held, bool aside)
        {
            int from = aside ? own : held;
            int value = zdo.GetInt(from);
            if (value == 0)
            {
                return false;
            }
            zdo.Set(aside ? held : own, value);
            zdo.Set(from, 0);
            return true;
        }
    }
}
