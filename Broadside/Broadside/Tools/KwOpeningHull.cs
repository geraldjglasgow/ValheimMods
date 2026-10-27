using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The parts the Doorway and Window tools work on, read from the world: a piece's tool (planking, a cabin, an old
    /// castle), its saved text and tier as a site at a point (<see cref="KwOpeningAim.Pick"/>), the ship on the dry dock it
    /// stands on (<see cref="KwBpDock"/>), and the other parts in line with it, each as a <see cref="KwPartMap"/>: the
    /// planking along the same keel line on the same side (a row of ports), and a doorway's partners (the planking under a
    /// cabin's raised side, the raised side over the planking). Every part is placed upright, its kit x along its keel line,
    /// so a place on one reads on another by two points carried through the world. Runs on the clicking player's game.
    /// </summary>
    public static class KwOpeningHull
    {
        private static readonly string PlankingPrefab = KwToolPieces.PartPrefix + "planking";

        /// <summary>The tool whose part a piece is, among those openings are cut in: "planking", "cabin" or "castle"; else null.</summary>
        public static string? ToolOf(Piece? piece)
        {
            if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
                return null;
            string prefab = Utils.GetPrefabName(piece.gameObject);
            if (prefab == PlankingPrefab)
                return "planking";
            return KwHouses.IsPart(prefab) ? prefab.Substring(KwToolPieces.PartPrefix.Length) : null;
        }

        public static Vector3 Kit(Piece part, Vector3 world) => KwMesh.ToKit(part.transform.InverseTransformPoint(world));

        public static Vector3 World(Piece part, Vector3 kit) => part.transform.TransformPoint(KwMesh.ToUnity(kit));

        /// <summary>The wall of a part at a world point, as a site; null and why off one.</summary>
        public static KwOpeningSite? Site(Piece piece, Vector3 world, out string why)
        {
            string? tool = ToolOf(piece);
            why = "Click the hull's planking or a cabin's wall";
            if (tool == null)
                return null;
            ZDO zdo = piece.m_nview.GetZDO();
            return KwOpeningAim.Pick(tool, zdo.GetString(KwGenerated.Key), KwBiomeTiers.Of(zdo), Kit(piece, world), out why, piece);
        }

        /// <summary>A site for one wall of a part by its name (an opening's wall), at a place on it; null when the part has no such wall.</summary>
        public static KwOpeningSite? Wall(Piece piece, string wall, float u, float z)
        {
            string? tool = ToolOf(piece);
            if (tool == null)
                return null;
            ZDO zdo = piece.m_nview.GetZDO();
            string saved = zdo.GetString(KwGenerated.Key);
            (KwOpeningSurface? surface, KwCabinWall? own) = Surface(tool, saved, wall);
            return surface == null ? null : new KwOpeningSite(tool, saved, KwBiomeTiers.Of(zdo), surface, own, piece) { U = u, Z = z };
        }

        // A part's wall by name: a planking side, a cabin's own wall, a cabin's raised side.
        private static (KwOpeningSurface? Surface, KwCabinWall? Own) Surface(string tool, string saved, string wall)
        {
            if (tool == "planking")
            {
                if (!KwSpan.TryParse(saved, out KwSpan span) || (wall != KwOpening.Port && wall != KwOpening.Starboard))
                    return (null, null);
                return (new KwPlankSurface(wall, span.Stations(), wall == KwOpening.Port ? 1f : -1f), null);
            }
            if (!KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return (null, null);
            KwCabinWall? own = KwCabinWalls.Of(spec).Find(w => w.Name == wall);
            if (own != null)
                return (new KwWallSurface(own), own);
            return (KwOpeningSurfaces.Raised(spec).Find(s => s.Wall == wall), null);
        }

        /// <summary>Every ship piece over the dry dock a part stands on, the part among them; null when it stands on none.</summary>
        public static List<Piece>? Ship(Piece part)
        {
            KwBpDockInfo? dock = KwBpDock.Find(part.transform.position);
            List<Piece>? ship = dock != null ? KwBpDock.Ship(dock) : null;
            return ship != null && ship.Contains(part) ? ship : null;
        }

        /// <summary>
        /// How a place on a site's wall reads on another part in line with it (its keel line on the site's, its x along the
        /// same way or the other), on that part's wall on the same side of the ship; null when it is not in line, or has no
        /// wall on that side.
        /// </summary>
        public static KwPartMap? Map(KwOpeningSite from, Piece to)
        {
            Piece? part = from.Piece;
            string? tool = ToolOf(to);
            if (part == null || tool == null)
                return null;
            return Mapped(from, p => Kit(to, World(part, p)), tool == "planking", (wall, u, z) => Wall(to, wall, u, z));
        }

        /// <summary>
        /// The map of a site onto another part, given how a kit point of the site's part reads in the other's kit axes, whether
        /// the other is planking, and its wall by name at a place: in line (its keel line on the site's, its x along the same
        /// way or the other) and on the same side; null when not. The dock's parts and a launched ship's alike. Pure.
        /// </summary>
        public static KwPartMap? Mapped(KwOpeningSite from, System.Func<Vector3, Vector3> into, bool planking, System.Func<string, float, float, KwOpeningSite?> wallOf)
        {
            Vector3 p0 = from.Surface.Point(from.U, from.Z, 0f);
            Vector3 q0 = into(p0), q1 = into(p0 + Vector3.right), keel = into(new Vector3(p0.x, 0f, p0.z));
            float b = q1.x - q0.x;
            if (Mathf.Abs(Mathf.Abs(b) - 1f) > 0.02f || Mathf.Abs(q1.y - q0.y) > 0.02f || Mathf.Abs(q1.z - q0.z) > 0.02f || Mathf.Abs(keel.y) > 0.05f)
                return null;
            string wall = planking ? (q0.y >= 0f ? KwOpening.Port : KwOpening.Starboard) : (q0.y >= 0f ? KwOpening.RaisedLeft : KwOpening.RaisedRight);
            KwOpeningSite? site = wallOf(wall, q0.x, q0.z);
            return site == null ? null : new KwPartMap(site, q0.x - b * p0.x, b, q0.z - p0.z);
        }

        /// <summary>
        /// The planking in line with a site on the same side, as maps, the site's own part first when it is planking: the
        /// parts a row of ports runs along. Parts a ward keeps are left out.
        /// </summary>
        public static List<KwPartMap> Line(KwOpeningSite site, List<Piece> ship)
        {
            List<KwPartMap> line = new List<KwPartMap>();
            if (site.Planking)
                line.Add(KwPartMap.Same(site));
            else
                return line;
            foreach (Piece piece in ship)
            {
                KwPartMap? map = piece != site.Piece && ToolOf(piece) == "planking" && KwUndoWork.Removable(piece) ? Map(site, piece) : null;
                if (map != null)
                    line.Add(map);
            }
            return line;
        }

        /// <summary>
        /// A doorway's possible partners for a site: for a cabin's raised side, the planking in line under it on that side;
        /// for planking, the cabins in line whose raised side stands on that side; none for a cabin's own wall.
        /// </summary>
        public static List<KwPartMap> Partners(KwOpeningSite site, List<Piece> ship)
        {
            List<KwPartMap> partners = new List<KwPartMap>();
            if (!site.Planking && !site.Raised)
                return partners;
            foreach (Piece piece in ship)
            {
                string? tool = piece != site.Piece ? ToolOf(piece) : null;
                if (tool == null || (tool == "planking") == site.Planking)
                    continue;
                KwPartMap? map = Map(site, piece);
                if (map != null && (map.Site.Planking || map.Site.Raised))
                    partners.Add(map);
            }
            return partners;
        }
    }
}
