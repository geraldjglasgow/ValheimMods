using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>Server: every sea gate pillar ZDO in the world. The server holds every ZDO, but finding one prefab among
    /// them means walking all of them (<c>ZDOMan.GetAllZDOsWithPrefabIterative</c> walks up to 400 occupied sectors a
    /// call and adds the out-of-world sector and the portal lists on the last call), so a pass is spread over frames,
    /// one call per frame, and starts only when someone asks and the last pass is older than
    /// <see cref="FreshSeconds"/>. The result is kept as ZDOIDs, not ZDOs: the game pools ZDOs and reuses a destroyed
    /// one for another object, while a destroyed object's id simply resolves to null. A lookup that misses may scan at
    /// once (<see cref="ScanNow"/>), so a gate paired a moment ago is still found.</summary>
    internal static class SeaGateScan
    {
        internal const float FreshSeconds = 5f;

        private static readonly List<ZDOID> found = new List<ZDOID>();
        private static readonly List<ZDO> buffer = new List<ZDO>();
        private static ZDOMan scannedOn;
        private static ZDOMan runningOn;
        private static float doneAt;
        private static int index;

        /// <summary>A pass of this world finished less than <see cref="FreshSeconds"/> ago.</summary>
        internal static bool IsFresh => scannedOn != null && scannedOn == ZDOMan.instance && Time.time - doneAt < FreshSeconds;

        private static bool Running => runningOn != null && runningOn == ZDOMan.instance;

        /// <summary>Starts a pass unless one is running or the last one is still fresh.</summary>
        internal static void Want()
        {
            if (Running || IsFresh || ZDOMan.instance == null)
                return;
            buffer.Clear();
            index = 0;
            runningOn = ZDOMan.instance;
        }

        /// <summary>One step of a running pass; called every frame on the server.</summary>
        internal static void Step()
        {
            if (!Running)
            {
                runningOn = null;
                return;
            }
            if (runningOn.GetAllZDOsWithPrefabIterative(SeaGateFields.PillarPrefab, buffer, ref index))
                Finish(runningOn);
        }

        /// <summary>A whole pass at once, for a lookup that cannot wait. Finishes a running pass too.</summary>
        internal static void ScanNow()
        {
            ZDOMan man = ZDOMan.instance;
            if (man == null)
                return;
            buffer.Clear();
            int at = 0;
            while (!man.GetAllZDOsWithPrefabIterative(SeaGateFields.PillarPrefab, buffer, ref at))
            {
            }
            Finish(man);
        }

        /// <summary>The pillars of the last pass that still exist.</summary>
        internal static List<ZDO> Pillars()
        {
            List<ZDO> pillars = new List<ZDO>(found.Count);
            ZDOMan man = ZDOMan.instance;
            if (man == null || scannedOn != man)
                return pillars;
            foreach (ZDOID id in found)
            {
                ZDO zdo = man.GetZDO(id);
                if (zdo != null && zdo.IsValid() && SeaGateFields.IsPillar(zdo))
                    pillars.Add(zdo);
            }
            return pillars;
        }

        /// <summary>Keeps the ids, once each: the out-of-world sector is walked twice by the game's iterator.</summary>
        private static void Finish(ZDOMan man)
        {
            HashSet<ZDOID> seen = new HashSet<ZDOID>();
            found.Clear();
            foreach (ZDO zdo in buffer)
            {
                if (zdo != null && zdo.IsValid() && SeaGateFields.IsPillar(zdo) && seen.Add(zdo.m_uid))
                    found.Add(zdo.m_uid);
            }
            buffer.Clear();
            scannedOn = man;
            runningOn = null;
            doneAt = Time.time;
        }
    }
}
