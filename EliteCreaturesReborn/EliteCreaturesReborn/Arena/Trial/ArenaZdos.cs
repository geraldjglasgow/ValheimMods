using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The server's hand on the arena's objects by their ZDOs, for when no machine has them loaded (features/arena.md
    /// section 4): it holds every ZDO of the world, so it finds them by the zones round a point and removes those that
    /// match - taking each over first, since only an owner may destroy - with no death, so nothing drops. Not for a hot
    /// path: a search of a few zones, run when a trial nobody is at is closed and once at the start against an older
    /// world's arena.
    /// </summary>
    internal static class ArenaZdos
    {
        private static readonly List<ZDO> Sector = new List<ZDO>();

        /// <summary>Removes every ZDO within <paramref name="zones"/> zones of <paramref name="at"/> that
        /// <paramref name="match"/> picks; how many went. Server only.</summary>
        public static int Remove(Vector3 at, int zones, Func<ZDO, bool> match)
        {
            ZDOMan zdos = ZDOMan.instance;
            if (zdos == null || ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return 0;
            }
            Sector.Clear();
            zdos.FindSectorObjects(ZoneSystem.GetZone(at), new SimulationDistance(zones, 0, classic: true), Sector);
            int removed = 0;
            foreach (ZDO zdo in Sector)
            {
                if (match(zdo))
                {
                    zdo.SetOwner(ZDOMan.GetSessionID());
                    zdos.DestroyZDO(zdo);
                    removed++;
                }
            }
            Sector.Clear();
            return removed;
        }
    }
}
