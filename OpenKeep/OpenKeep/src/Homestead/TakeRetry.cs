using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// A fire that has room but found nothing to take (no fuel near it, or its station entry disabled) looks again only
    /// after <see cref="WaitSeconds"/>, so fires far from any chest do not scan the containers every two seconds. Local
    /// to this machine: a new owner looks at once. A fire burns one unit in 5000 s or more, so the wait never lets one die.
    /// </summary>
    public static class FuelRetry
    {
        public const float WaitSeconds = 10f;

        private static readonly Dictionary<Fireplace, float> waitUntil = new Dictionary<Fireplace, float>();

        public static bool Due(Fireplace fire)
        {
            return !waitUntil.TryGetValue(fire, out float until) || Time.time >= until;
        }

        public static void Later(Fireplace fire)
        {
            waitUntil[fire] = Time.time + WaitSeconds;
            if (waitUntil.Count > 128)
                Prune();
        }

        /// <summary>Drops fires that were destroyed or unloaded and waits that are over.</summary>
        private static void Prune()
        {
            List<Fireplace> gone = new List<Fireplace>();
            foreach (KeyValuePair<Fireplace, float> entry in waitUntil)
            {
                if (entry.Key == null || Time.time >= entry.Value)
                    gone.Add(entry.Key);
            }
            foreach (Fireplace fire in gone)
                waitUntil.Remove(fire);
        }
    }
}
