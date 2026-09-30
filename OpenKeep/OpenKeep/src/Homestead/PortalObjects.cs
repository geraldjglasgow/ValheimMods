using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Quick Area Loading, the objects. <c>ZNetScene.CreateDestroyObjects</c> runs 30 times a second and creates at most
    /// 100 nearby objects each time behind a loading screen (a hundredth of those waiting when more), and none at all
    /// until every zone of the simulation area is loaded. While <see cref="PortalLoad.Busy"/>, this creates more of the
    /// objects the game has just listed, within <see cref="BudgetMs"/> per run, in the game's own order
    /// (<c>ZNetScene.ZDOCompare</c>: by type, then nearest to the player first) and with the game's own checks, but only
    /// in zones whose land is loaded: the whole-area gate is what keeps an object from being made where there is no
    /// ground yet, and this keeps that promise zone by zone. So the objects around the target appear as soon as their
    /// own land has, and the far edge of the area finishes after the landing.
    /// </summary>
    public static class PortalObjects
    {
        private const double BudgetMs = 15.0;

        private static readonly List<ZDO> pending = new List<ZDO>();
        private static readonly Stopwatch clock = new Stopwatch();

        public static void Hurry(ZNetScene scene)
        {
            if (!PortalLoad.Busy() || ZoneSystem.instance == null)
                return;
            Collect(scene.m_tempCurrentObjects, ZNet.instance.GetReferencePosition());
            pending.Sort(ZNetScene.ZDOCompare);
            clock.Restart();
            foreach (ZDO zdo in pending)
            {
                if (clock.Elapsed.TotalMilliseconds >= BudgetMs)
                    break;
                scene.CreateObject(zdo);
            }
            pending.Clear();
        }

        /// <summary>The listed objects not made yet whose zone has its land and is ready for their type, with the game's sort key.</summary>
        private static void Collect(List<ZDO> near, Vector3 at)
        {
            pending.Clear();
            ZoneSystem zones = ZoneSystem.instance;
            foreach (ZDO zdo in near)
            {
                if (zdo.Created)
                    continue;
                Vector2s sector = zdo.GetSector();
                if (!zones.IsZoneLoaded(sector) || !zones.IsZoneReadyForType(sector, zdo.Type))
                    continue;
                zdo.m_tempSortValue = Utils.DistanceSqr(at, zdo.GetPosition());
                pending.Add(zdo);
            }
        }
    }
}
