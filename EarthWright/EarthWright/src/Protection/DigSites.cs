using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// Where the dig-exception objects and tar are, per 64 m zone, from the ZDOs this machine holds (the owner of a
    /// terrain compiler holds the ZDOs around it, whether or not their objects are instantiated). Listed objects are
    /// matched by prefab hash; tar is the game's "TarLiquid" volume, whose depth grid tells where tar actually lies (the
    /// volume itself spans the whole zone). Each zone's scan is kept for five seconds; a mined-out deposit's ZDO is
    /// destroyed, so the exception ends with it.
    /// </summary>
    public static class DigSites
    {
        private const float Lifetime = 5f;
        private const float TarMargin = 2f;
        private const float TarDepth = 0.05f;
        private static readonly int TarHash = "TarLiquid".GetStableHashCode();

        private sealed class ZoneSites
        {
            public float Until;
            public readonly List<Vector3> Markers = new List<Vector3>();
            public readonly List<ZDO> Tar = new List<ZDO>();
        }

        private static readonly Dictionary<long, ZoneSites> zones = new Dictionary<long, ZoneSites>();
        private static readonly List<ZDO> none = new List<ZDO>();
        private static string hashesFrom;
        private static HashSet<int> hashes = new HashSet<int>();

        /// <summary>A listed object stands within the radius (XZ) of the position.</summary>
        public static bool NearListed(Vector3 position, float radius)
        {
            float squared = radius * radius;
            foreach (ZoneSites sites in Around(position))
            {
                foreach (Vector3 marker in sites.Markers)
                {
                    float dx = marker.x - position.x;
                    float dz = marker.z - position.z;
                    if (dx * dx + dz * dz <= squared)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Tar lies at the position or within two metres of it.</summary>
        public static bool InTar(Vector3 position)
        {
            foreach (ZoneSites sites in Around(position))
            {
                foreach (ZDO zdo in sites.Tar)
                {
                    LiquidVolume tar = TarVolume(zdo);
                    if (tar != null && TarNear(tar, position))
                        return true;
                }
            }
            return false;
        }

        /// <summary>The position's zone and its eight neighbours (the radius is at most 32 m).</summary>
        private static IEnumerable<ZoneSites> Around(Vector3 position)
        {
            Vector2s center = ZoneSystem.GetZone(position);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                    yield return Sites(new Vector2s(center.x + dx, center.y + dz));
            }
        }

        private static ZoneSites Sites(Vector2s zone)
        {
            long key = ((long)zone.x << 32) ^ (uint)zone.y;
            float now = Time.time;
            if (zones.TryGetValue(key, out ZoneSites sites) && now < sites.Until && sites.Until - now <= Lifetime)
                return sites;
            if (zones.Count > 256)
                zones.Clear();
            sites = Scan(zone, now + Lifetime);
            zones[key] = sites;
            return sites;
        }

        private static ZoneSites Scan(Vector2s zone, float until)
        {
            ZoneSites sites = new ZoneSites { Until = until };
            HashSet<int> wanted = Hashes();
            foreach (ZDO zdo in ZoneObjects(zone))
            {
                if (zdo == null)
                    continue;
                int prefab = zdo.GetPrefab();
                if (wanted.Contains(prefab))
                    sites.Markers.Add(zdo.GetPosition());
                else if (prefab == TarHash)
                    sites.Tar.Add(zdo);
            }
            return sites;
        }

        /// <summary>The ZDOs of one zone (the game's sectors are its zones) held by this machine.</summary>
        private static List<ZDO> ZoneObjects(Vector2s zone)
        {
            ZDOMan man = ZDOMan.instance;
            if (man == null || man.m_objectsBySector == null)
                return none;
            ZoneSystem.SectorIndex index = ZoneSystem.SectorToIndex(zone);
            if (index.Sector >= man.m_objectsBySector.Length)
                return none;
            return man.m_objectsBySector[index.Sector] ?? none;
        }

        /// <summary>The stable hashes of the listed prefab names, rebuilt when the setting changes.</summary>
        private static HashSet<int> Hashes()
        {
            NameList list = ProtectionSettings.DigObjectNames;
            if (list == null || ReferenceEquals(list.Source, hashesFrom))
                return hashes;
            HashSet<int> built = new HashSet<int>();
            foreach (string name in list.Names)
                built.Add(name.GetStableHashCode());
            hashes = built;
            hashesFrom = list.Source;
            zones.Clear();
            return hashes;
        }

        private static LiquidVolume TarVolume(ZDO zdo)
        {
            ZNetView view = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdo) : null;
            return view != null ? view.GetComponent<LiquidVolume>() : null;
        }

        /// <summary>The tar's depth at the position or two metres around it; the volume's grid is local to its centre.</summary>
        private static bool TarNear(LiquidVolume tar, Vector3 position)
        {
            if (tar.m_liquidType != LiquidType.Tar || tar.m_depths == null)
                return false;
            float size = tar.m_width * tar.m_scale;
            Vector3 center = tar.transform.position;
            if (Mathf.Abs(position.x - center.x) > size * 0.5f + TarMargin || Mathf.Abs(position.z - center.z) > size * 0.5f + TarMargin)
                return false;
            return DepthAt(tar, position) || DepthAt(tar, position + new Vector3(TarMargin, 0f, 0f)) || DepthAt(tar, position - new Vector3(TarMargin, 0f, 0f))
                || DepthAt(tar, position + new Vector3(0f, 0f, TarMargin)) || DepthAt(tar, position - new Vector3(0f, 0f, TarMargin));
        }

        private static bool DepthAt(LiquidVolume tar, Vector3 position)
        {
            Vector2 local = tar.WorldToLocal(position);
            if (local.x < 0f || local.y < 0f || local.x > tar.m_width || local.y > tar.m_width)
                return false;
            return tar.GetDepth(local.x, local.y) > TarDepth;
        }
    }
}
