using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// The game's level for a creature this mod's stars put at level 1. A fresh roll parks the level the game, a spawner
    /// or another mod gave it in the ZDO before setting 1 (<see cref="Park"/>); a tame that takes the game's stars gets
    /// it back, and one that takes this mod's stars again parks it once more (<see cref="Follow"/>). Owner only:
    /// SetLevel writes the ZDO, and every other machine reads the level as it loads the creature.
    /// </summary>
    internal static class GameLevel
    {
        private static readonly int Hash = TraitKeys.GameLevel.GetStableHashCode();

        /// <summary>OWNER: this mod's stars take over; a level above 1 is kept aside and the creature goes to level 1.</summary>
        public static void Park(Character creature, ZDO zdo)
        {
            int level = creature.GetLevel();
            if (level > 1)
            {
                zdo.Set(Hash, level);
            }
            creature.SetLevel(1);
        }

        /// <summary>OWNER: a tame's stars change hands - back to the game's level, or parked again for this mod's stars
        /// (unless they are off for its kind and it has none: then its level stays the game's).</summary>
        public static void Follow(Character creature, ZDO zdo, bool gameStars)
        {
            if (gameStars)
            {
                Restore(creature, zdo);
            }
            else if (!RuleState.Active.KeepsLevel(TraitStore.Load(zdo).Stars, isBoss: false))
            {
                Park(creature, zdo);
            }
        }

        // SetLevel runs the game's own health setup for the level, replacing the maximum this mod's stars gave it - also
        // for a creature with no level kept (it appeared before this was kept), which stays at its level. Cleared to 0,
        // not removed: a removal is not sent to other machines.
        private static void Restore(Character creature, ZDO zdo)
        {
            int parked = zdo.GetInt(Hash);
            if (parked > 0)
            {
                zdo.Set(Hash, 0);
            }
            creature.SetLevel(parked > 1 ? parked : creature.GetLevel());
        }
    }
}
