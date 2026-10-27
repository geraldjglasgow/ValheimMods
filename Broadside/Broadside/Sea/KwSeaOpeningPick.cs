using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's remove on a launched ship's door or window (the user's rule: aimed at one of these parts it removes only
    /// that part; <see cref="KwSeaRemove"/>): the opening the ray passes through (its cut and frame) or whose leaf it meets is
    /// picked at no distance; else the opening nearest the point met on the same wall, by how far that point is from its
    /// frame on the face, so a near miss names it and never breaks the ship up. Removing it fills it in (a doorway's partner
    /// with it, as the Doorway tool's fill-in does, <see cref="KwDoorwayPlan.Twin"/>): the wall made whole again, its frame,
    /// cover or door given back. Pure but for the leaf's object.
    /// </summary>
    public static class KwSeaOpeningPick
    {
        /// <summary>The opening nearest the hammer's aim on a ship; null for none within a metre.</summary>
        public static KwSeaPick? Of(KwSeaKind kind, KwSeaOpeningShip ship, KwSeaAim aim)
        {
            KwShipOpeningLeaf? leaf = aim.Collider != null ? aim.Collider.GetComponentInParent<KwShipOpeningLeaf>() : null;
            KwShipOpening? hung = leaf != null ? Numbered(aim.Ship, leaf.Number) : null;
            if (hung != null)
                return Picked(kind, ship, hung.Piece, hung.Opening.Id, 0f);
            var through = KwSeaOpeningAim.Through(ship, aim.From, aim.Way, aim.Hit ? aim.Distance + 0.15f : 50f);
            if (through != null)
                return Picked(kind, ship, KwSeaOpeningPart.Of(through.Value.Site)!.Index, through.Value.Opening.Id, 0f);
            KwOpeningSite? site = aim.Hit ? KwSeaOpeningAim.Nearest(ship, aim.Point, out _) : null;
            (KwOpening? near, float off) = site != null ? Nearest(site) : (null, 0f);
            return near != null && off <= 1f ? Picked(kind, ship, KwSeaOpeningPart.Of(site)!.Index, near.Id, off) : null;
        }

        // A covered opening by its number in the ship's openings as numbered now.
        private static KwShipOpening? Numbered(KwSeaShip ship, int number)
        {
            KwShipOpenings? openings = ship.GetComponent<KwShipOpenings>();
            KwShipOpeningPlan? plan = openings != null ? openings.Plan : null;
            return number < 0 || plan == null ? null : plan.Openings.Find(o => o.Number == number);
        }

        // An opening on a part as a pick: the part's index, the opening's number on it, how far, what it is, where.
        private static KwSeaPick? Picked(KwSeaKind kind, KwSeaOpeningShip ship, int index, int id, float off)
        {
            KwSeaOpeningPart? part = ship.Part(index);
            KwOpening? o = part?.Cut.Find(w => w.Openings.Exists(x => x.Id == id))?.Openings.Find(x => x.Id == id);
            KwOpeningSite? wall = o != null ? part!.Wall(o.Wall) : null;
            if (o == null || wall == null)
                return null;
            Vector3 middle = part!.Ship(wall.Surface.Point(o.U, o.Sill + o.Height / 2f, 0f));
            return new KwSeaPick(kind, index, id, off, o.IsDoorway ? "doorway" : "window", middle);
        }

        // The opening on a site's wall nearest its place, and how far the place is from its frame's outline (0 inside it).
        private static (KwOpening? Opening, float Off) Nearest(KwOpeningSite site)
        {
            (KwOpening? best, float off) = (null, float.MaxValue);
            foreach (KwOpening o in site.Openings)
            {
                if (o.Wall != site.Wall)
                    continue;
                float d = Outside(o.Outline(KwOpeningFrames.Width(o.Frame).Wide), new Vector2(site.U, site.Z));
                if (d < off)
                    (best, off) = (o, d);
            }
            return (best, off);
        }

        // How far a point is outside a convex outline (0 inside).
        private static float Outside(List<Vector2> ring, Vector2 p)
        {
            if (KwOpeningAim.Inside(ring, p))
                return 0f;
            float best = float.MaxValue;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], e = ring[(i + 1) % ring.Count] - a;
                float t = e.sqrMagnitude > 1e-10f ? Mathf.Clamp01(Vector2.Dot(p - a, e) / e.sqrMagnitude) : 0f;
                best = Mathf.Min(best, Vector2.Distance(p, a + e * t));
            }
            return best;
        }

        /// <summary>The edit that fills a picked opening in (its doorway partners with it); null when it is no longer there.</summary>
        public static KwSeaEdit? Removal(KwSeaOpeningShip ship, int index, int id)
        {
            KwSeaOpeningPart? part = ship.Part(index);
            KwOpening? o = part != null ? KwOpeningText.Of(part.Saved).Find(x => x.Id == id) : null;
            KwOpeningSite? site = o != null ? part!.Wall(o.Wall)?.At(o.U, o.Sill + 1f) : null;
            if (o == null || site == null)
                return null;
            KwOpeningPlan plan = new KwOpeningPlan();
            plan.Part(site).Fill(o);
            if (o.IsDoorway)
            {
                foreach (KwPartMap map in KwSeaOpeningMaps.Partners(ship, site))
                {
                    KwOpening? twin = KwDoorwayPlan.Twin(map, o);
                    if (twin != null)
                        plan.Part(map.Site).Fill(twin);
                }
            }
            plan.Filled = 1;
            return KwSeaOpeningSend.Edit(ship, plan, o.IsDoorway ? "Doorway filled in" : "Window filled in");
        }
    }
}
