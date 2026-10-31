using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The lone set of each sector (<see cref="LoneLoad"/>), kept between load ticks so the server does not sort every
    /// object of every lone player's area 30 times a second. A sector's lists are rebuilt before they are next read
    /// whenever an object entered or left it (<see cref="SectorChanges"/>: every new, destroyed, received and moved ZDO
    /// passes ZDOMan.AddToSector or RemoveFromSector), so a list never holds a ZDO the game has destroyed and handed out
    /// again, and every <see cref="RefreshSeconds"/> anyway, for objects that changed hands (one the server owns is in the
    /// set). Kept beside it per sector: the objects left out that block a spawn point (position and footprint, for
    /// <see cref="SpawnPoints"/>, counted only while the sector is in use as a lone one) and the distant ones left out (a
    /// hosting player sees those from afar). A sector nobody used for <see cref="ForgetSeconds"/> is dropped; every one
    /// when a new world loads.
    /// </summary>
    internal static class LoneSectors
    {
        /// <summary>Seconds after which a sector's lists are rebuilt even when nothing entered or left it.</summary>
        public const float RefreshSeconds = 10f;

        /// <summary>Seconds a sector's lists are kept after it was last loaded as a lone one.</summary>
        public const float ForgetSeconds = 60f;

        // How recently a sector must have been loaded as a lone one for its left-out objects to block spawn points.
        private const float InUseSeconds = 0.5f;

        private struct Blocker
        {
            public float X;
            public float Z;
            public float RadiusSqr;
        }

        private sealed class Cache
        {
            public readonly List<ZDO> Kept = new List<ZDO>();
            public readonly List<ZDO> Far = new List<ZDO>();
            public readonly List<Blocker> Blocks = new List<Blocker>();
            public float BuiltAt;
            public float UsedAt;
            public bool Dirty;
        }

        private static readonly Dictionary<uint, Cache> Caches = new Dictionary<uint, Cache>();
        private static readonly Stack<Cache> Spare = new Stack<Cache>();
        private static readonly List<uint> Stale = new List<uint>();
        private static ZDOMan _world = null!;

        /// <summary>An object entered or left the sector: its lists are rebuilt before they are read again.</summary>
        public static void MarkDirty(uint index)
        {
            if (Caches.TryGetValue(index, out Cache cache))
            {
                cache.Dirty = true;
            }
        }

        /// <summary>Adds the sector's lone set to the game's near list, as loaded this tick.</summary>
        public static void AddNear(uint index, List<ZDO> near, float now)
        {
            Cache cache = Current(index, now);
            cache.UsedAt = now;
            near.AddRange(cache.Kept);
        }

        /// <summary>True when the sector was loaded as a lone one this tick.</summary>
        public static bool LoadedLone(uint index, float now) => Caches.TryGetValue(index, out Cache cache) && cache.UsedAt == now;

        /// <summary>Adds the sector's distant objects the lone set left out, for a hosting player's distant view.</summary>
        public static void AddFar(uint index, List<ZDO> distant, float now) => distant.AddRange(Current(index, now).Far);

        /// <summary>True when an object the server left out of a lone sector in use stands over the point.</summary>
        public static bool Blocks(uint index, Vector3 point, float now)
        {
            if (!Caches.TryGetValue(index, out Cache cache) || now - cache.UsedAt > InUseSeconds)
            {
                return false;
            }
            foreach (Blocker blocker in Current(index, now).Blocks)
            {
                float dx = point.x - blocker.X;
                float dz = point.z - blocker.Z;
                if (dx * dx + dz * dz <= blocker.RadiusSqr)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Drops the lists of sectors nobody loaded as lone for <see cref="ForgetSeconds"/>.</summary>
        public static void Forget(float now)
        {
            foreach (KeyValuePair<uint, Cache> entry in Caches)
            {
                if (now - entry.Value.UsedAt > ForgetSeconds)
                {
                    Stale.Add(entry.Key);
                }
            }
            foreach (uint index in Stale)
            {
                Release(index);
            }
            Stale.Clear();
        }

        private static Cache Current(uint index, float now)
        {
            if (_world != ZDOMan.instance)
            {
                ForgetWorld();
            }
            if (!Caches.TryGetValue(index, out Cache cache))
            {
                cache = Spare.Count > 0 ? Spare.Pop() : new Cache();
                cache.Dirty = true;
                cache.UsedAt = now;
                Caches[index] = cache;
            }
            if (cache.Dirty || now - cache.BuiltAt > RefreshSeconds)
            {
                Rebuild(index, cache, now);
            }
            return cache;
        }

        private static void Rebuild(uint index, Cache cache, float now)
        {
            Clear(cache);
            cache.Dirty = false;
            cache.BuiltAt = now;
            List<ZDO> all = ZDOMan.instance.m_objectsBySector[index];
            if (all == null)
            {
                return;
            }
            foreach (ZDO zdo in all)
            {
                Sort(zdo, cache);
            }
        }

        private static void Sort(ZDO zdo, Cache cache)
        {
            if (LoneLoad.Keeps(zdo, out float radius))
            {
                cache.Kept.Add(zdo);
                return;
            }
            if (zdo.Distant)
            {
                cache.Far.Add(zdo);
            }
            if (radius > 0f)
            {
                Vector3 position = zdo.GetPosition();
                cache.Blocks.Add(new Blocker { X = position.x, Z = position.z, RadiusSqr = radius * radius });
            }
        }

        private static void Release(uint index)
        {
            Cache cache = Caches[index];
            Caches.Remove(index);
            Clear(cache);
            Spare.Push(cache);
        }

        private static void ForgetWorld()
        {
            foreach (Cache cache in Caches.Values)
            {
                Clear(cache);
                Spare.Push(cache);
            }
            Caches.Clear();
            _world = ZDOMan.instance;
        }

        private static void Clear(Cache cache)
        {
            cache.Kept.Clear();
            cache.Far.Clear();
            cache.Blocks.Clear();
        }
    }
}
