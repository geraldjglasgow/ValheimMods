using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The bed a player wakes in. At death, on the dying player's own client while its player still exists, the
    /// known beds plus the profile's own spawn point (a bed claimed before the mod that has not loaded since) are
    /// ordered by map distance from the death point. The nearest becomes the profile's custom spawn point, which the
    /// game reads twice: <c>Game.FindSpawnPoint</c> after the ten second wait, and <c>SaveLogoutPoint</c> when the
    /// player logs out while dead. The others wait as the fallback, nearest first: when the game finds no bed of
    /// this player at the point and clears it, <see cref="TryNext"/> hands out the next one. Cleared at every spawn.
    /// </summary>
    public static class BedRespawn
    {
        private static readonly List<Vector3> fallback = new List<Vector3>();
        private static string fallbackScope;

        public static void Choose(Vector3 deathPoint)
        {
            fallback.Clear();
            string scope = BedStore.Scope();
            if (!BedStore.Writable(scope))
                return;
            List<Vector3> beds = Candidates(deathPoint);
            if (beds.Count == 0)
                return;
            Game.instance.GetPlayerProfile().SetCustomSpawnPoint(beds[0]);
            fallback.AddRange(beds.GetRange(1, beds.Count - 1));
            fallbackScope = scope;
            float metres = BedPoints.MapDistance(beds[0], deathPoint);
            Plugin.Log.LogInfo($"OpenKeep: died at {BedPoints.Format(deathPoint)}; waking in the bed at {BedPoints.Format(beds[0])}, {metres:F0} m away ({beds.Count} beds known)");
        }

        /// <summary>The next nearest bed after <paramref name="gone"/>, for the same character and world.</summary>
        public static bool TryNext(Vector3 gone, out Vector3 next)
        {
            next = Vector3.zero;
            if (fallbackScope == null || BedStore.Scope() != fallbackScope)
                fallback.Clear();
            fallback.RemoveAll(p => BedPoints.Same(p, gone));
            if (fallback.Count == 0)
                return false;
            next = fallback[0];
            fallback.RemoveAt(0);
            return true;
        }

        public static void Clear()
        {
            fallback.Clear();
            fallbackScope = null;
        }

        private static List<Vector3> Candidates(Vector3 from)
        {
            List<Vector3> beds = BedList.Known();
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile.HaveCustomSpawnPoint() && !BedPoints.Contains(beds, profile.GetCustomSpawnPoint()))
                beds.Add(profile.GetCustomSpawnPoint());
            BedPoints.SortByDistance(beds, from);
            return beds;
        }
    }
}
