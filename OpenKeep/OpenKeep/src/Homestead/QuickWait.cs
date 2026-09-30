using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// A wait that grows with distance, shared by Quick Respawn (<see cref="BedWait"/>) and Quick Portals
    /// (<see cref="PortalQuick"/>): the shortest wait at 0 m, the game's full wait at the range and beyond, in
    /// proportion between. The game's waits are timers compared with fixed seconds, so the features run those timers
    /// faster by <see cref="Speed"/> rather than change the seconds; the game's own "is the area loaded" checks stay.
    /// </summary>
    public static class QuickWait
    {
        /// <summary>No timer runs more than this many times faster than the game's, so a wait never collapses to nothing.</summary>
        private const float MinSeconds = 0.05f;

        public static float Seconds(float metres, float range, float shortest, float full)
        {
            float share = range > 0f ? Mathf.Clamp01(metres / range) : 1f;
            return Mathf.Lerp(Mathf.Clamp(shortest, 0f, full), full, share);
        }

        /// <summary>How many times faster than the game a timer runs so that the full wait lasts <paramref name="seconds"/>.</summary>
        public static float Speed(float seconds, float full) => full / Mathf.Max(seconds, MinSeconds);
    }
}
