using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where on a part the Doorway and Window tools are aimed, from a point in the part's kit axes: on planking, the side
    /// the point is on (by the sign of its kit y) at its x along and its z; on a cabin, the nearest of its own walls
    /// (<see cref="KwCabinWalls"/>, the place along its line) and its raised hull sides (<see cref="KwOpeningSurfaces.Raised"/>,
    /// the place along being the cabin's x), within a hand's breadth of its face; a cabin's roof is no wall. And which of a
    /// part's openings a line of sight passes through (the cut leaves nothing there to hit) or a point on a wall falls in,
    /// each opening counted with its frame round it. Pure.
    /// </summary>
    public static class KwOpeningAim
    {
        /// <summary>How far off a wall's faces a point may be and still be on the wall.</summary>
        public const float Near = 0.3f;

        /// <summary>The wall of a part a kit point is on, as a site with the place on it; null and why when it is on none.</summary>
        public static KwOpeningSite? Pick(string tool, string saved, KwBiomeTier tier, Vector3 kit, out string why, Piece? piece = null) =>
            Pick(Walls(tool, saved, tier, piece), tool, kit, out why);

        /// <summary>
        /// A part's walls as sites read once, at no place yet (<see cref="KwOpeningSite.At"/> puts one at a place): a
        /// planking part's port and starboard sides (seated where its piece stands, as its mesh is, <see cref="KwSeat"/>); a
        /// cabin's own walls and raised sides. None for a text that does not read. A launched ship's part is seated where
        /// seat says, on its blueprint's keels (<see cref="KwSeaOpeningPart"/>).
        /// </summary>
        public static List<KwOpeningSite> Walls(string tool, string saved, KwBiomeTier tier, Piece? piece = null, KwSeatAt? seat = null)
        {
            List<KwOpeningSite> walls = new List<KwOpeningSite>();
            KwSeatAt? at = seat ?? (piece != null ? KwSeatAt.World(piece.transform) : (KwSeatAt?)null);
            List<Station>? stations = tool == "planking" && KwSpan.TryParse(saved, out KwSpan span)
                ? (at?.Seated(span) ?? span).Stations() : null;
            if (stations != null && stations.Count >= 2)
            {
                walls.Add(new KwOpeningSite("planking", saved, tier, new KwPlankSurface(KwOpening.Port, stations, 1f), null, piece));
                walls.Add(new KwOpeningSite("planking", saved, tier, new KwPlankSurface(KwOpening.Starboard, stations, -1f), null, piece));
            }
            else if (KwHouses.Owns(tool) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
            {
                foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                    walls.Add(new KwOpeningSite(tool, saved, tier, new KwWallSurface(wall), wall, piece));
                foreach (KwOpeningSurface raised in KwOpeningSurfaces.Raised(spec))
                    walls.Add(new KwOpeningSite(tool, saved, tier, raised, null, piece));
            }
            return walls;
        }

        /// <summary>The wall among a part's walls (read once, <see cref="Walls"/>) a kit point is on, at the place; null and why off them.</summary>
        public static KwOpeningSite? Pick(List<KwOpeningSite> walls, string tool, Vector3 kit, out string why)
        {
            if (tool == "planking")
                return Plank(walls, kit, out why);
            if (KwHouses.Owns(tool))
                return Cabin(walls, kit, out why);
            why = "Click the hull's planking or a cabin's wall";
            return null;
        }

        // A planking part's side at the point, while the point is on its face (not a transom board or a stem post).
        private static KwOpeningSite? Plank(List<KwOpeningSite> walls, Vector3 kit, out string why)
        {
            why = "Click the side of the planking";
            KwOpeningSite? side = walls.Find(w => w.Wall == (kit.y >= 0f ? KwOpening.Port : KwOpening.Starboard));
            if (side == null)
                return null;
            KwOpeningSurface surface = side.Surface;
            (float from, float to) = surface.Reach;
            if (kit.x < from || kit.x > to)
                return null;
            (float lo, float hi) = surface.Range(kit.x);
            if (kit.z < lo - 0.05f || kit.z > hi + 0.05f || Off(surface, kit.x, kit.z, kit) > Near)
                return null;
            return side.At(kit.x, kit.z);
        }

        // The cabin's own wall or raised side nearest the point, while it is within reach of one.
        private static KwOpeningSite? Cabin(List<KwOpeningSite> walls, Vector3 kit, out string why)
        {
            why = "This cabin cannot be read";
            if (walls.Count == 0)
                return null;
            why = "Click a wall of the cabin, not its roof";
            if (kit.z > walls[0].Head - 0.005f)
                return null;
            why = "Click a wall of the cabin or its raised side";
            (KwOpeningSite? best, float u, float off) = (null, 0f, Near);
            foreach (KwOpeningSite wall in walls)
            {
                (float along, float d) = wall.Own != null ? OnPath(wall.Own, kit) : (kit.x, 0f);
                if (wall.Own == null)
                    d = Within(wall.Surface, along, kit.z) ? Off(wall.Surface, along, kit.z, kit) : float.MaxValue;
                if (d < off && Within(wall.Surface, along, kit.z))
                    (best, u, off) = (wall, along, d);
            }
            return best?.At(u, kit.z);
        }

        // Whether a place is on a wall: within its reach along and its heights there.
        private static bool Within(KwOpeningSurface surface, float u, float z)
        {
            (float from, float to) = surface.Reach;
            (float lo, float hi) = surface.Range(u);
            return u >= from - 0.01f && u <= to + 0.01f && z >= lo - 0.05f && z <= hi + 0.05f;
        }

        // How far a point is from a wall's outer or inner face at a place.
        private static float Off(KwOpeningSurface surface, float u, float z, Vector3 kit) =>
            Mathf.Min(Vector3.Distance(surface.Point(u, z, 0f), kit), Vector3.Distance(surface.Point(u, z, -surface.Thick), kit));

        /// <summary>The place along a cabin wall's line nearest a point seen from above, and how far off the line it is.</summary>
        public static (float U, float Off) OnPath(KwCabinWall wall, Vector3 kit)
        {
            Vector2 at = new Vector2(kit.x, kit.y);
            (float u, float off) = (0f, float.MaxValue);
            for (int i = 0; i + 1 < wall.Path.Count; i++)
            {
                Vector2 a = wall.Path[i], c = wall.Path[i + 1];
                float length = Vector2.Distance(a, c);
                float t = length > 1e-5f ? Mathf.Clamp01(Vector2.Dot(at - a, c - a) / (length * length)) : 0f;
                float d = Vector2.Distance(at, Vector2.Lerp(a, c, t));
                if (d < off)
                    (u, off) = (wall.Marks[i] + t * length, d);
            }
            return (u, off);
        }

        /// <summary>The opening on one of a part's walls that a place on that wall falls in, its frame counted; null for none.</summary>
        public static KwOpening? At(IEnumerable<KwOpening> openings, string wall, float u, float z)
        {
            foreach (KwOpening o in openings)
            {
                if (o.Wall == wall && Inside(o.Outline(KwOpeningFrames.Width(o.Frame).Wide), new Vector2(u, z)))
                    return o;
            }
            return null;
        }

        /// <summary>
        /// The nearest opening of a part's walls a line of sight goes through (kit axes: from a point, a unit way, no further
        /// than most), its frame counted: the wall, the opening, and how far along the line; null for none.
        /// </summary>
        public static (KwOpeningWall Wall, KwOpening Opening, float T)? Through(List<KwOpeningWall> walls, Vector3 from, Vector3 way, float most)
        {
            (KwOpeningWall Wall, KwOpening Opening, float T)? best = null;
            foreach (KwOpeningWall wall in walls)
            {
                foreach (KwOpening o in wall.Openings)
                {
                    float t = Crossing(wall.Surface, o, from, way);
                    if (t >= 0f && t <= most && (best == null || t < best.Value.T))
                        best = (wall, o, t);
                }
            }
            return best;
        }

        // How far along a line of sight it crosses an opening's face (its frame counted); -1 when it misses it.
        private static float Crossing(KwOpeningSurface s, KwOpening o, Vector3 from, Vector3 way)
        {
            float mid = o.Sill + o.Height / 2f;
            Vector3 centre = s.Point(o.U, mid, 0f), n = s.Out(o.U, mid);
            float facing = Vector3.Dot(way, n);
            if (Mathf.Abs(facing) < 1e-4f)
                return -1f;
            float t = Vector3.Dot(centre - from, n) / facing;
            Vector3 p = from + way * t - centre;
            Vector2 at = new Vector2(o.U + Vector3.Dot(p, s.Along(o.U, mid)), mid + Vector3.Dot(p, s.Up(o.U, mid)));
            return t >= 0f && Inside(o.Outline(KwOpeningFrames.Width(o.Frame).Wide), at) ? t : -1f;
        }

        /// <summary>Whether a point is inside a convex outline wound anticlockwise.</summary>
        public static bool Inside(List<Vector2> ring, Vector2 p)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], e = ring[(i + 1) % ring.Count] - a, d = p - a;
                if (e.x * d.y - e.y * d.x < -1e-6f)
                    return false;
            }
            return true;
        }
    }
}
