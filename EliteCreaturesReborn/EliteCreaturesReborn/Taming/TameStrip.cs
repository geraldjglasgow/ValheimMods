using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// What `15 - Tamed` takes from a creature, as bits: its stars (<see cref="Stars"/>) and its mutations
    /// (<see cref="Mutations"/>). The owner decides from the settings and writes the bits to the ZDO
    /// (<see cref="TameSettle"/>); every machine reads the bits, never its own settings, so every player loads the
    /// creature alike. The rolled stars and mask stay stored beside them: the bits only hide them.
    /// </summary>
    internal static class TameStrip
    {
        public const int Stars = 1;
        public const int Mutations = 2;

        private static readonly int Hash = TraitKeys.TameStrip.GetStableHashCode();

        /// <summary>What the settings take from this creature now: nothing unless it is tamed, and never from a boss.</summary>
        public static int Wanted(Character creature)
        {
            if (!creature.IsTamed() || creature.IsBoss())
            {
                return 0;
            }
            return (TamedSettings.StarsKept ? 0 : Stars) | (TamedSettings.MutationsKept ? 0 : Mutations);
        }

        /// <summary>What the owner has taken, from the ZDO; 0 on every creature it takes nothing from.</summary>
        public static int Of(ZDO zdo) => zdo.GetInt(Hash);

        /// <summary>OWNER: records what is taken. Set to 0 rather than removed: a removal is not sent to other machines.</summary>
        public static void Set(ZDO zdo, int strip) => zdo.Set(Hash, strip);

        /// <summary>The stored traits as they work under <paramref name="strip"/>: changed in place and returned.</summary>
        public static CreatureTraits View(CreatureTraits stored, int strip)
        {
            if ((strip & Stars) != 0)
            {
                stored.Stars = 0;
                stored.GameStars = true; // its level is the game's: KeepsLevel
            }
            if ((strip & Mutations) != 0)
            {
                stored.Mask = 0;
            }
            return stored;
        }
    }
}
