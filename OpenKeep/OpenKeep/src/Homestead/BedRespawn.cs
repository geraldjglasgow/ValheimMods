using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The bed a player wakes in. At death, on the dying player's own client while its player still exists, the
    /// known beds plus the profile's own spawn point (a bed claimed before the mod that has not loaded since) are
    /// ordered by map distance from the death point. The nearest becomes the profile's custom spawn point, which the
    /// game reads twice: <c>Game.FindSpawnPoint</c> once the wait is over, and <c>SaveLogoutPoint</c> when the player
    /// logs out while dead. <see cref="Prefer"/> puts another of them there instead (the bed clicked on the map). The
    /// others wait as the fallback, nearest first: when the game finds no bed of this player at the point and clears
    /// it, <see cref="TryNext"/> hands out the next one. Cleared at every spawn.
    /// </summary>
    public static class BedRespawn
    {
        private static readonly List<Vector3> ordered = new List<Vector3>();
        private static readonly List<Vector3> fallback = new List<Vector3>();
        private static string fallbackScope;

        /// <summary>Sets the nearest bed and returns every candidate, nearest first; empty when there is none.</summary>
        public static List<Vector3> Choose(Vector3 deathPoint)
        {
            Clear();
            string scope = BedStore.Scope();
            if (!BedStore.Writable(scope))
                return new List<Vector3>();
            ordered.AddRange(Candidates(deathPoint));
            if (ordered.Count == 0)
                return new List<Vector3>();
            fallbackScope = scope;
            Use(ordered[0]);
            float metres = BedPoints.MapDistance(ordered[0], deathPoint);
            Plugin.Log.LogInfo($"OpenKeep: died at {BedPoints.Format(deathPoint)}; waking in the bed at {BedPoints.Format(ordered[0])}, {metres:F0} m away ({ordered.Count} beds known)");
            return new List<Vector3>(ordered);
        }

        /// <summary>One of the candidates becomes the spawn point; the rest stay the fallback, nearest first.</summary>
        public static void Prefer(Vector3 bed)
        {
            if (!BedPoints.Contains(ordered, bed))
                return;
            Use(bed);
            Plugin.Log.LogInfo($"OpenKeep: chose the bed at {BedPoints.Format(bed)}");
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
            ordered.Clear();
            fallback.Clear();
            fallbackScope = null;
        }

        private static void Use(Vector3 bed)
        {
            Game.instance.GetPlayerProfile().SetCustomSpawnPoint(bed);
            fallback.Clear();
            fallback.AddRange(ordered.FindAll(p => !BedPoints.Same(p, bed)));
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
