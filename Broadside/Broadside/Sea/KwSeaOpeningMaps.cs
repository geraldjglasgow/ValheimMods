using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The other parts of a launched ship in line with a site at sea, as maps (<see cref="KwPartMap"/>, worked out as the
    /// dock's are, <see cref="KwOpeningHull.Mapped"/>, from the parts' places in the ship's frame): the planking along the
    /// same keel line on the same side, the site's own part first (a row of ports runs along it); and a doorway's partners,
    /// the planking under a cabin's raised side or the raised side over the planking. Pure.
    /// </summary>
    public static class KwSeaOpeningMaps
    {
        /// <summary>The planking in line with a site on the same side, its own part first; the site alone for a cabin's wall.</summary>
        public static List<KwPartMap> Line(KwSeaOpeningShip ship, KwOpeningSite site)
        {
            List<KwPartMap> line = new List<KwPartMap> { KwPartMap.Same(site) };
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (part == null || !site.Planking)
                return line;
            foreach (KwSeaOpeningPart other in ship.Parts)
            {
                KwPartMap? map = other != part && other.Planking ? Map(site, part, other) : null;
                if (map != null)
                    line.Add(map);
            }
            return line;
        }

        /// <summary>A doorway's possible partners: for a raised side the planking in line under it, for planking the cabins' raised sides over it; none for a cabin's own wall.</summary>
        public static List<KwPartMap> Partners(KwSeaOpeningShip ship, KwOpeningSite site)
        {
            List<KwPartMap> partners = new List<KwPartMap>();
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (part == null || (!site.Planking && !site.Raised))
                return partners;
            foreach (KwSeaOpeningPart other in ship.Parts)
            {
                if (other == part || other.Planking == site.Planking)
                    continue;
                KwPartMap? map = Map(site, part, other);
                if (map != null && (map.Site.Planking || map.Site.Raised))
                    partners.Add(map);
            }
            return partners;
        }

        /// <summary>How a place on a site's wall reads on another part of the ship in line with it; null when not in line.</summary>
        public static KwPartMap? Map(KwOpeningSite from, KwSeaOpeningPart part, KwSeaOpeningPart to) =>
            KwOpeningHull.Mapped(from, p => to.Kit(part.Ship(p)), to.Planking, (wall, u, z) => to.Wall(wall)?.At(u, z));
    }
}
