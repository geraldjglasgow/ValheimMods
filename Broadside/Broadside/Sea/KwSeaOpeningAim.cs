using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the Doorway and Window tools are aimed on a launched ship (<see cref="KwSeaAim"/>: the camera's ray and the box
    /// it met, in the ship's frame), as the dock's sight is (<see cref="KwOpeningAimed"/>): an opening the line of sight
    /// passes through before the box it met (its cut leaves nothing there to hit), else the wall of the planking part or
    /// cabin whose face is nearest the point met (<see cref="KwOpeningAim.Pick"/>, each part in its own kit axes) and the
    /// opening on it there. Plain arithmetic on the ship's reads (<see cref="KwSeaOpeningShip"/>). Pure.
    /// </summary>
    public static class KwSeaOpeningAim
    {
        // How far the tools reach, and the slack past the hit an opening still counts in (a shut lid stands proud of its wall).
        private const float Reach = 50f, Slack = 0.15f;
        private const string Off = "Click the hull's planking or a cabin's wall";

        /// <summary>The sight of a tool's aim on a launched ship.</summary>
        public static KwOpeningSight Of(KwSeaOpeningShip ship, KwSeaAim aim) => Of(ship, aim.From, aim.Way, aim.Hit, aim.Point, aim.Distance);

        /// <summary>The sight of a ray in the ship's frame (a unit way) that met the ship's boxes at a point so far along it, or none.</summary>
        public static KwOpeningSight Of(KwSeaOpeningShip ship, Vector3 from, Vector3 way, bool hit, Vector3 point, float distance)
        {
            (KwOpeningSite Site, KwOpening Opening, float T)? through = Through(ship, from, way, hit ? distance + Slack : Reach);
            if (through != null)
                return new KwOpeningSight(through.Value.Site, through.Value.Opening, null, "", ship);
            if (!hit)
                return new KwOpeningSight(null, null, null, Off, ship);
            KwOpeningSite? site = Nearest(ship, point, out string why);
            KwOpening? at = site != null ? KwOpeningAim.At(site.Openings, site.Wall, site.U, site.Z) : null;
            return new KwOpeningSight(site, at, null, site != null ? "" : why, ship);
        }

        /// <summary>The nearest opening of the ship's parts a ray passes through within so far, at its middle, and how far along; null for none.</summary>
        public static (KwOpeningSite Site, KwOpening Opening, float T)? Through(KwSeaOpeningShip ship, Vector3 from, Vector3 way, float most)
        {
            (KwSeaOpeningPart? part, KwOpening? opening, string wall, float t) = (null, null, "", most);
            foreach (KwSeaOpeningPart p in ship.Parts)
            {
                if (p.Cut.Count == 0)
                    continue;
                var crossed = KwOpeningAim.Through(p.Cut, p.Kit(from), p.KitWay(way), t);
                if (crossed != null)
                    (part, opening, wall, t) = (p, crossed.Value.Opening, crossed.Value.Wall.Surface.Wall, crossed.Value.T);
            }
            if (part == null || opening == null)
                return null;
            KwOpeningSite? site = part.Wall(wall)?.At(opening.U, opening.Sill + opening.Height / 2f);
            KwOpening? saved = site?.Openings.Find(o => o.Id == opening.Id);
            return site != null && saved != null ? (site, saved, t) : ((KwOpeningSite, KwOpening, float)?)null;
        }

        /// <summary>The wall of the ship's parts a point of its frame is on (the nearest face of those it is near), at the place; null and why off them.</summary>
        public static KwOpeningSite? Nearest(KwSeaOpeningShip ship, Vector3 point, out string why)
        {
            (KwOpeningSite? best, float off) = (null, float.MaxValue);
            why = Off;
            foreach (KwSeaOpeningPart part in ship.Parts)
            {
                Vector3 kit = part.Kit(point);
                KwOpeningSite? site = KwOpeningAim.Pick(part.Walls, part.Tool, kit, out string said);
                float d = site != null ? Distance(site, kit) : float.MaxValue;
                if (site == null && best == null && said.Length > 0 && Near(part, kit))
                    why = said;
                if (site != null && d < off)
                    (best, off) = (site, d);
            }
            return best;
        }

        // How far a kit point is from a site's wall's outer or inner face at the site's place.
        private static float Distance(KwOpeningSite site, Vector3 kit) =>
            Mathf.Min(Vector3.Distance(site.Surface.Point(site.U, site.Z, 0f), kit), Vector3.Distance(site.Surface.Point(site.U, site.Z, -site.Surface.Thick), kit));

        // Whether a kit point is within a metre of a part's walls' reach (its refusal is the one worth saying).
        private static bool Near(KwSeaOpeningPart part, Vector3 kit)
        {
            foreach (KwOpeningSite wall in part.Walls)
            {
                (float from, float to) = wall.Surface.Reach;
                float u = wall.AlongOf(kit);
                if (u >= from - 1f && u <= to + 1f && Vector3.Distance(wall.Surface.Point(Mathf.Clamp(u, from, to), kit.z, 0f), kit) < 1f)
                    return true;
            }
            return false;
        }
    }
}
