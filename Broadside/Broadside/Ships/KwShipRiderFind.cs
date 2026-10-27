using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's ZDO found by the ship's own id (<see cref="KwShipRiders.IdKey"/>) among the ZDOs this game holds in
    /// the sector round a point and the eight beside it: a rider's ship is never more than a ship's length from it, so
    /// it is there whenever this game knows of it, loaded as an object here or not. Used only for a parked rider (its ship
    /// not in this game's scene), on its owner, at most once a second.
    /// </summary>
    public static class KwShipRiderFind
    {
        private static readonly int ShipPrefab = KwShipPrefab.Name.GetStableHashCode();
        private static readonly List<ZDO> found = new List<ZDO>();
        private static readonly HashSet<ZoneSystem.SectorIndex> seen = new HashSet<ZoneSystem.SectorIndex>();

        /// <summary>The ZDO of the ship of this id near a point, or null.</summary>
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
                if (zdo.GetPrefab() == ShipPrefab && zdo.GetLong(KwShipRiders.IdHash, 0L) == id)
                    return zdo;
            }
            return null;
        }
    }
}
