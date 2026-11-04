using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's cart's ZDO found by the cart's own id (<see cref="KwCargoCart.IdHash"/>) among the ZDOs this game holds in
    /// the zone round a point and the eight beside it: a crate's cart is never far from it (its owner keeps its place within
    /// 8 m of the cart), so it is there whenever this game knows of it, loaded as an object here or not. Used only for a
    /// parked crate (its cart not in this game's scene), on its owner, at most once a second.
    /// </summary>
    public static class KwCartFind
    {
        private static readonly List<ZDO> found = new List<ZDO>();
        private static readonly HashSet<ZoneSystem.SectorIndex> seen = new HashSet<ZoneSystem.SectorIndex>();

        /// <summary>The ZDO of the cart of this id near a point, or null.</summary>
        public static ZDO? Near(long id, Vector3 at)
        {
            if (id == 0L || ZDOMan.instance == null)
                return null;
            Vector2s zone = ZoneSystem.GetZone(at);
            found.Clear();
            seen.Clear();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                    ZDOMan.instance.FindObjects(new Vector2s(zone.x + x, zone.y + z), found, seen);
            }
            foreach (ZDO zdo in found)
            {
                if (zdo.GetPrefab() == KwCargoCart.PrefabHash && zdo.GetLong(KwCargoCart.IdHash, 0L) == id)
                    return zdo;
            }
            return null;
        }
    }
}
