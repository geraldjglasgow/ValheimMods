using System.Collections.Generic;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The creatures in the hall this machine has loaded, listed as their <see cref="ArenaFighter"/> wakes and dropped as
    /// it goes, so the core's owner counts a trial's living creatures and bosses, and sends its own away at the end,
    /// without a scene or ZDO search (features/arena.md section 4). The core's owner holds every one of them: the hall
    /// lies in the one zone its machine has loaded around the core. A count reads each creature's tag in its ZDO (the tag
    /// moves as creatures are adopted or tamed) - a few dozen lookups twice a second at most.
    /// </summary>
    internal static class ArenaFighters
    {
        private static readonly List<ArenaFighter> Live = new List<ArenaFighter>();

        public static void Add(ArenaFighter fighter)
        {
            if (!Live.Contains(fighter))
            {
                Live.Add(fighter);
            }
        }

        // Swap-remove: the order means nothing. Called from OnDestroy, never while a loop below walks the list.
        public static void Remove(ArenaFighter fighter)
        {
            int at = Live.IndexOf(fighter);
            if (at < 0)
            {
                return;
            }
            Live[at] = Live[Live.Count - 1];
            Live.RemoveAt(Live.Count - 1);
        }

        /// <summary>The creatures of trial <paramref name="trial"/> alive here now: not dead, not tamed.</summary>
        public static int Alive(long trial) => Count(trial, bossesOnly: false);

        /// <summary>The bosses of trial <paramref name="trial"/> alive here now.</summary>
        public static int Bosses(long trial) => Count(trial, bossesOnly: true);

        /// <summary>
        /// The trial has ended: each of its creatures this machine owns vanishes now, dropping nothing
        /// (<see cref="ArenaOrders.Vanish"/>); those another machine owns vanish on their own owner within a second, as
        /// they read the trial is over. A vanishing only destroys the object at the end of the frame, so the list is never
        /// changed under this loop.
        /// </summary>
        public static void VanishOwned(long trial)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                ArenaFighter fighter = Live[i];
                ZDO? zdo = fighter != null ? fighter.Zdo : null;
                if (zdo != null && fighter!.View.IsOwner() && ArenaTag.Trial(zdo) == trial)
                {
                    ArenaOrders.Vanish(fighter);
                }
            }
        }

        private static int Count(long trial, bool bossesOnly)
        {
            int count = 0;
            for (int i = 0; i < Live.Count; i++)
            {
                ArenaFighter fighter = Live[i];
                ZDO? zdo = fighter != null ? fighter.Zdo : null;
                if (zdo == null || ArenaTag.Trial(zdo) != trial || fighter!.Body.IsDead() || fighter.Body.IsTamed())
                {
                    continue;
                }
                count += !bossesOnly || ArenaTag.IsBoss(zdo) ? 1 : 0;
            }
            return count;
        }
    }
}
