using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// The owner's half of `15 - Tamed`: when what the settings take from a creature differs from what its ZDO says was
    /// taken (it was just tamed, a setting changed, or it loads tamed from before), the ZDO is brought in line - the new
    /// bits (<see cref="TameStrip"/>), the rolled stars and mutations moved aside or back (<see cref="TameHold"/>), the
    /// game's level given back or parked again (<see cref="GameLevel"/>), and a Thieving creature's stolen goods dropped
    /// at its feet, since a creature with no mutation drops no pouch when it dies. Every machine then loads it again from
    /// the ZDO (<see cref="TameWatch"/>).
    /// </summary>
    internal static class TameSettle
    {
        /// <summary>OWNER: settles the creature's ZDO; true when anything changed.</summary>
        public static bool Settle(Character creature, ZDO zdo)
        {
            int wanted = TameStrip.Wanted(creature);
            int had = TameStrip.Of(zdo);
            bool moved = (wanted | had) != 0 && TameHold.Align(zdo, wanted); // before Follow, which reads the stars back
            if (wanted == had)
            {
                return moved;
            }
            TameStrip.Set(zdo, wanted);
            if ((wanted & ~had & TameStrip.Mutations) != 0)
            {
                GiveBack(creature, zdo);
            }
            if (((wanted ^ had) & TameStrip.Stars) != 0)
            {
                GameLevel.Follow(creature, zdo, gameStars: (wanted & TameStrip.Stars) != 0);
            }
            return true;
        }

        // Exactly the items it took, never re-made: the same drop its death or a despawn would make.
        private static void GiveBack(Character creature, ZDO zdo)
        {
            List<PouchStore.Entry> pouch = PouchStore.Load(zdo);
            if (pouch.Count > 0)
            {
                PouchDrop.DropAll(pouch, creature.transform.position);
                PouchStore.Save(zdo, new List<PouchStore.Entry>());
            }
        }
    }
}
