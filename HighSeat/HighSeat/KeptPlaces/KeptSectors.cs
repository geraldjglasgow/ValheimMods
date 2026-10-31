using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The still objects (<see cref="StillKinds"/>, persistent ones only) of every held zone (<see cref="KeptZones"/>),
    /// added to the game's near list in ZNetScene.CreateDestroyObjects, 30 times a second: the game's RemoveObjects then
    /// leaves their instances, and its CreateObjects makes the missing ones. Each zone's objects are cached and the list
    /// read again from the game's sector table at most every RefreshSeconds, one zone a pass; at once when a player's area
    /// stops covering the zone, so what they built just before leaving stays loaded; never a scan of every object. A
    /// cached object destroyed since is skipped by its id (the game pools ZDO objects and hands them to new objects).
    /// Only zones whose ground is built are added (a terrain compiler made before its ground never applies its edits),
    /// each once and marked visited, so nothing is listed twice; a zone a player's area already covers is left to it.
    /// New instances are made at most NewPerPass a pass (NewPerPassHosted on a hosted game, whose own area has a budget
    /// of ten), and only for an object still in its zone, so loading a place never holds up the objects around players;
    /// a leaving zone gets none, and the first leaving zone is released each pass. At most MaxObjects are held in all:
    /// past that, the remaining zones are left as the game leaves them.
    /// </summary>
    internal static class KeptSectors
    {
        private const float RefreshSeconds = 4f;
        private const int NewPerPass = 10;
        private const int NewPerPassHosted = 3;
        private const int MaxObjects = 40000;

        private sealed class Sector
        {
            public readonly List<ZDO> Zdos = new List<ZDO>();
            public readonly List<ZDOID> Ids = new List<ZDOID>();
            public float Refreshed = float.NegativeInfinity;
            public bool Taken;
        }

        private static readonly Dictionary<Vector2s, Sector> Cache = new Dictionary<Vector2s, Sector>();
        private static readonly Stack<Sector> Spare = new Stack<Sector>();
        private static readonly List<Vector2s> Unused = new List<Vector2s>();
        private static readonly List<ZDO> Found = new List<ZDO>();
        private static readonly HashSet<ZoneSystem.SectorIndex> FoundIn = new HashSet<ZoneSystem.SectorIndex>();
        private static int _cursor;
        private static int _budget;
        private static int _held;

        public static void Reset()
        {
            Cache.Clear();
            Spare.Clear();
            _cursor = 0;
        }

        /// <summary>
        /// Adds every held zone's still objects to <paramref name="near"/>, then releases one leaving zone and refreshes
        /// one cache.
        /// </summary>
        public static void Add(List<ZDO> near, HashSet<ZoneSystem.SectorIndex> visited)
        {
            _budget = ZNet.instance.IsDedicated() ? NewPerPass : NewPerPassHosted;
            _held = 0;
            foreach (Vector2s zone in KeptZones.Kept)
            {
                AddZone(near, visited, zone, true);
            }
            List<Vector2s> leaving = KeptZones.Leaving;
            for (int i = 1; i < leaving.Count; i++)
            {
                AddZone(near, visited, leaving[i], false);
            }
            if (KeptZones.ReleaseFirst(out Vector2s gone))
            {
                Recycle(gone);
            }
            RefreshOne();
        }

        private static void AddZone(List<ZDO> near, HashSet<ZoneSystem.SectorIndex> visited, Vector2s zone, bool create)
        {
            if (!Cache.TryGetValue(zone, out Sector sector) || !ZoneSystem.instance.m_zones.ContainsKey(zone))
            {
                return;
            }
            ZoneSystem.SectorIndex index = ZoneSystem.SectorToIndex(zone);
            if (visited.Contains(index))
            {
                sector.Taken = true;
                return;
            }
            if (sector.Taken || float.IsNegativeInfinity(sector.Refreshed))
            {
                Refresh(zone, sector);
            }
            if (_held + sector.Zdos.Count > MaxObjects)
            {
                return;
            }
            visited.Add(index);
            _held += sector.Zdos.Count;
            AddObjects(near, sector, index, create);
        }

        // Made ones always (so they stay); missing ones within the budget, and only while still in this zone.
        private static void AddObjects(List<ZDO> near, Sector sector, ZoneSystem.SectorIndex index, bool create)
        {
            List<ZDO> zdos = sector.Zdos;
            List<ZDOID> ids = sector.Ids;
            for (int i = 0; i < zdos.Count; i++)
            {
                ZDO zdo = zdos[i];
                if (zdo.m_uid != ids[i])
                {
                    continue;
                }
                if (zdo.Created)
                {
                    near.Add(zdo);
                }
                else if (create && _budget > 0 && zdo.GetSectorIndex() == index)
                {
                    near.Add(zdo);
                    _budget--;
                }
            }
        }

        private static void Refresh(Vector2s zone, Sector sector)
        {
            Found.Clear();
            FoundIn.Clear();
            ZDOMan.instance.FindObjects(zone, Found, FoundIn);
            sector.Zdos.Clear();
            sector.Ids.Clear();
            foreach (ZDO zdo in Found)
            {
                if (zdo.Persistent && StillKinds.Is(zdo))
                {
                    sector.Zdos.Add(zdo);
                    sector.Ids.Add(zdo.m_uid);
                }
            }
            Found.Clear();
            sector.Refreshed = Time.unscaledTime;
            sector.Taken = false;
        }

        // The next kept zone due, round the list; a zone a player covers waits until they leave it.
        private static void RefreshOne()
        {
            List<Vector2s> kept = KeptZones.Kept;
            float now = Time.unscaledTime;
            for (int tried = 0; tried < kept.Count; tried++)
            {
                _cursor = (_cursor + 1) % kept.Count;
                Vector2s zone = kept[_cursor];
                if (Cache.TryGetValue(zone, out Sector sector) && !sector.Taken && now - sector.Refreshed >= RefreshSeconds)
                {
                    Refresh(zone, sector);
                    return;
                }
            }
        }

        /// <summary>A cache for every kept zone; the caches of zones no longer held go back to the spares.</summary>
        public static void Sync()
        {
            foreach (Vector2s zone in KeptZones.Kept)
            {
                if (!Cache.ContainsKey(zone))
                {
                    Cache[zone] = Spare.Count > 0 ? Spare.Pop() : new Sector();
                }
            }
            Unused.Clear();
            foreach (Vector2s zone in Cache.Keys)
            {
                if (!KeptZones.Holds(zone))
                {
                    Unused.Add(zone);
                }
            }
            foreach (Vector2s zone in Unused)
            {
                Recycle(zone);
            }
            Unused.Clear();
        }

        private static void Recycle(Vector2s zone)
        {
            if (!Cache.TryGetValue(zone, out Sector sector))
            {
                return;
            }
            Cache.Remove(zone);
            sector.Zdos.Clear();
            sector.Ids.Clear();
            sector.Refreshed = float.NegativeInfinity;
            sector.Taken = false;
            Spare.Push(sector);
        }
    }
}
