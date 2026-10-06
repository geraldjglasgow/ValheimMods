using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// A fire or station that has room but found nothing to take (nothing near it, or its station entry disabled) looks
    /// again only after <see cref="WaitSeconds"/>, so ones far from any chest do not scan the containers on every tick.
    /// Local to this machine: a new owner looks at once. A fire burns one unit in 5000 s or more and a station works one
    /// item in 15 s or more, so the wait never lets one run dry for long.
    /// </summary>
    public static class TakeRetry
    {
        public const float WaitSeconds = 10f;

        private static readonly Dictionary<Component, float> waitUntil = new Dictionary<Component, float>();
        private static readonly PruneMark pruneMark = new PruneMark(128);

        public static bool Due(Component taker)
        {
            return !waitUntil.TryGetValue(taker, out float until) || Time.time >= until;
        }

        public static void Later(Component taker)
        {
            waitUntil[taker] = Time.time + WaitSeconds;
            if (pruneMark.Due(waitUntil.Count))
                Prune();
        }

        /// <summary>Drops fires and stations that were destroyed or unloaded and waits that are over.</summary>
        private static void Prune()
        {
            List<Component> gone = new List<Component>();
            foreach (KeyValuePair<Component, float> entry in waitUntil)
            {
                if (entry.Key == null || Time.time >= entry.Value)
                    gone.Add(entry.Key);
            }
            foreach (Component taker in gone)
                waitUntil.Remove(taker);
            pruneMark.Pruned(waitUntil.Count);
        }
    }
}
