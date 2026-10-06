using System.Collections;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Config changes re-apply their ship, or every ship for a multiplier, once on the next frame however many arrive
    /// until then: joining a server whose values differ sets many entries in one frame, and each apply walks every
    /// loaded ship. Without a running plugin the apply happens at once; a wait older than a frame was lost (its
    /// coroutine stopped) and is started again.
    /// </summary>
    public static class ShipValueQueue
    {
        private static readonly HashSet<string> pending = new HashSet<string>();
        private static bool pendingAll;
        private static MonoBehaviour host;
        private static int waitingSince = -1;

        public static void Initialize(MonoBehaviour plugin) => host = plugin;

        /// <summary>One ship's entry changed.</summary>
        public static void Request(string name)
        {
            pending.Add(name);
            Schedule();
        }

        /// <summary>A global multiplier changed.</summary>
        public static void RequestAll()
        {
            pendingAll = true;
            Schedule();
        }

        private static void Schedule()
        {
            if (waitingSince >= 0 && Time.frameCount <= waitingSince + 1)
                return;
            if (host == null || !host.isActiveAndEnabled)
            {
                Flush();
                return;
            }
            waitingSince = Time.frameCount;
            host.StartCoroutine(NextFrame());
        }

        private static IEnumerator NextFrame()
        {
            yield return null;
            Guard.Run("apply ship values", Flush);
        }

        private static void Flush()
        {
            waitingSince = -1;
            if (pendingAll)
                ShipValues.ApplyAll();
            else
                foreach (string name in pending)
                    ShipValues.Apply(name);
            pendingAll = false;
            pending.Clear();
        }
    }
}
