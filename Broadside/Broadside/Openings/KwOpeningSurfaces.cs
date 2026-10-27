using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>A wall of a part with the openings cut in it (those that fit it).</summary>
    public sealed class KwOpeningWall
    {
        public KwOpeningWall(KwOpeningSurface surface, List<KwOpening> openings)
        {
            Surface = surface;
            Openings = openings;
        }

        public KwOpeningSurface Surface { get; }
        public List<KwOpening> Openings { get; }
    }

    /// <summary>
    /// A part's walls that take openings, each with the openings saved for it that fit it: a planking part's two sides
    /// (<see cref="KwOpening.Port"/>, <see cref="KwOpening.Starboard"/>) on its stations; a cabin's own walls by name and its
    /// raised hull sides on each side at the hull (<see cref="KwOpening.RaisedLeft"/>, <see cref="KwOpening.RaisedRight"/>).
    /// An opening fits its wall while it lies within the wall's reach along and starts below the wall's top (a cabin's own
    /// wall: wholly within it); one left behind by an edit (a cabin's side brought back to the hull, a wall made shorter)
    /// is neither cut, framed nor hung. The same for every peer, a blueprint and a launched ship. Pure.
    /// </summary>
    public static class KwOpeningSurfaces
    {
        /// <summary>
        /// A generated part's walls with their openings, from its tool and saved text; none for a part that takes none.
        /// Planking is seated where seat says (<see cref="KwSeat"/>), as its mesh is, so frames and leaves fit its cuts.
        /// </summary>
        public static List<KwOpeningWall> Of(string tool, string saved, KwSeatAt? seat = null)
        {
            List<KwOpening> openings = KwOpeningText.Of(saved);
            if (openings.Count == 0)
                return new List<KwOpeningWall>();
            if (KwHouses.Owns(tool))
                return KwCabinSpec.TryParse(saved, out KwCabinSpec spec) ? Cabin(spec) : new List<KwOpeningWall>();
            if (tool != "planking" || !KwSpan.TryParse(saved, out KwSpan span))
                return new List<KwOpeningWall>();
            return Planking((seat?.Seated(span) ?? span).Stations(), openings);
        }

        /// <summary>A planking part's two sides with their openings.</summary>
        public static List<KwOpeningWall> Planking(List<Station> stations, List<KwOpening> openings)
        {
            List<KwOpeningWall> walls = new List<KwOpeningWall>();
            Add(walls, new KwPlankSurface(KwOpening.Port, stations, 1f), openings);
            Add(walls, new KwPlankSurface(KwOpening.Starboard, stations, -1f), openings);
            return walls;
        }

        /// <summary>A cabin's own walls and its raised sides with their openings.</summary>
        public static List<KwOpeningWall> Cabin(KwCabinSpec spec)
        {
            List<KwOpeningWall> walls = new List<KwOpeningWall>();
            foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                Add(walls, new KwWallSurface(wall), spec.Openings);
            foreach (KwOpeningSurface raised in Raised(spec))
                Add(walls, raised, spec.Openings);
            return walls;
        }

        /// <summary>A cabin's raised hull sides as walls: on each side at the hull, while the hull is raised there at all.</summary>
        public static List<KwOpeningSurface> Raised(KwCabinSpec spec)
        {
            List<KwOpeningSurface> sides = new List<KwOpeningSurface>();
            List<Station> raised = KwCabinShape.Raised(spec);
            if (!raised.Exists(s => s.Half[1].y - s.Half[0].y > 0.01f))
                return sides;
            if (spec.Left.AtHull)
                sides.Add(new KwPlankSurface(KwOpening.RaisedLeft, raised, 1f));
            if (spec.Right.AtHull)
                sides.Add(new KwPlankSurface(KwOpening.RaisedRight, raised, -1f));
            return sides;
        }

        /// <summary>The openings in a cabin's raised sides that fit them, for the raised strakes' cuts.</summary>
        public static List<KwOpening> RaisedOpenings(KwCabinSpec spec)
        {
            List<KwOpeningWall> walls = new List<KwOpeningWall>();
            if (spec.Openings.Count > 0)
            {
                foreach (KwOpeningSurface raised in Raised(spec))
                    Add(walls, raised, spec.Openings);
            }
            return walls.ConvertAll(w => w.Openings).SelectMany(o => o).ToList();
        }

        // A wall with the openings saved for it that fit it, when it has any, each fitted to it (its outline on the face at
        // its true size, KwOpeningSurface.Fit) and, on planking or a raised side, given its tunnel square to the hull there
        // (KwOpeningRings).
        private static void Add(List<KwOpeningWall> walls, KwOpeningSurface surface, List<KwOpening> openings)
        {
            List<KwOpening> mine = KwOpeningRings.Bind(surface, openings.FindAll(o => o.Wall == surface.Wall && Fits(surface, o)));
            if (mine.Count > 0)
                walls.Add(new KwOpeningWall(surface, mine));
        }

        /// <summary>
        /// Whether an opening lies on its wall: within its reach along, its sill under the wall's top all along it; on a
        /// cabin's own wall wholly inside its outline.
        /// </summary>
        public static bool Fits(KwOpeningSurface surface, KwOpening o)
        {
            (float from, float to) = surface.Reach;
            if (o.U0 < from - 0.001f || o.U1 > to + 0.001f)
                return false;
            foreach (float u in new[] { o.U0, o.U, o.U1 })
            {
                (float lo, float hi) = surface.Range(u);
                if (o.Sill >= hi - 0.01f || (surface is KwWallSurface && (o.Sill < lo - 0.001f || o.Top > hi + 0.001f)))
                    return false;
            }
            return true;
        }

        /// <summary>The cut outlines of the openings on one wall of a part (none: null).</summary>
        public static List<List<Vector2>>? Holes(List<KwOpening> openings, string wall)
        {
            List<List<Vector2>>? holes = null;
            foreach (KwOpening o in openings)
            {
                if (o.Wall == wall)
                    (holes ??= new List<List<Vector2>>()).Add(o.Outline());
            }
            return holes;
        }
    }
}
