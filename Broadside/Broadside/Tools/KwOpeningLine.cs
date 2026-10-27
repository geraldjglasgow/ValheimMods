using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>An opening on a side's line as the part aimed at reads it: its place along and its sill in that part's measures.</summary>
    public sealed class KwLineOpening
    {
        public KwLineOpening(KwPartMap map, KwOpening opening)
        {
            (Map, Opening) = (map, opening);
            (U, Sill) = (map.Back(opening.U), opening.Sill - map.Dz);
        }

        public KwPartMap Map { get; }
        public KwOpening Opening { get; }
        public float U { get; }
        public float Sill { get; }
    }

    /// <summary>
    /// One side the Doorway and Window tools place along (<see cref="KwOpeningPlace"/>, <see cref="KwOpeningFill"/>): the parts in
    /// line with the part aimed at, as maps (<see cref="KwOpeningHull.Line"/>: the planking along the same keel line on the same
    /// side; a cabin's wall or raised side alone), the openings of the tool's kind on them (doorways for the Doorway tool,
    /// windows for the Window tool) in the aimed part's measures, nearest first along, and how far the side reaches. Which
    /// part holds an opening at a place along (between its ribs or corner posts). The side's sections (<see cref="Section"/>):
    /// the middle, its planking from rib to rib together, and each end part alone. Pure.
    /// </summary>
    public sealed class KwOpeningLine
    {
        // The middle of a hull's side: its planking parts from rib to rib, one section together.
        private static readonly object Middle = new object();

        public KwOpeningLine(List<KwPartMap> maps, bool doorways)
        {
            Maps = maps;
            foreach (KwPartMap map in maps)
            {
                foreach (KwOpening o in map.Site.Openings)
                {
                    if (o.Wall == map.Site.Wall && o.IsDoorway == doorways && KwOpeningSurfaces.Fits(map.Site.Surface, o))
                        Openings.Add(new KwLineOpening(map, o));
                }
            }
            Openings.Sort((a, b) => a.U.CompareTo(b.U));
        }

        // Some of a line's parts and their openings, as read already.
        private KwOpeningLine(List<KwPartMap> maps, List<KwLineOpening> openings)
        {
            Maps = maps;
            Openings.AddRange(openings);
        }

        public List<KwPartMap> Maps { get; }
        public List<KwLineOpening> Openings { get; } = new List<KwLineOpening>();

        /// <summary>
        /// The section of its side a part is in (the user's words, 2026-10-25: "the hull pieces between supports (ribs) should
        /// all be treated as one piece, and the hull piece that goes from a support to the actual keel (the end) should be
        /// treated as a separate whole piece"): every planking part from rib to rib is the middle, one section together; a
        /// part from a rib to a keel end (the bow's or the stern's), one from keel end to keel end with no rib, and a cabin's
        /// wall are each a section of their own.
        /// </summary>
        public static object Section(KwPartMap map) => map.Site.Planking && map.Site.Span != null && map.Site.Span.RibEnded ? Middle : map.Site.Of ?? map;

        /// <summary>The section of the part aimed at (the line's first).</summary>
        public object Aimed => Section(Maps[0]);

        /// <summary>The side's parts in one section and the openings on them (what the snaps count from).</summary>
        public KwOpeningLine Within(object section) =>
            new KwOpeningLine(Maps.FindAll(m => Equals(Section(m), section)), Openings.FindAll(o => Equals(Section(o.Map), section)));

        /// <summary>The side's reach along, from its first part's end to its last's, in the aimed part's measures.</summary>
        public (float From, float To) Reach
        {
            get
            {
                (float from, float to) = (float.MaxValue, float.MinValue);
                foreach (KwPartMap map in Maps)
                {
                    (float a, float c) = map.Site.Surface.Reach;
                    (float x, float y) = (map.Back(a), map.Back(c));
                    (from, to) = (Mathf.Min(from, Mathf.Min(x, y)), Mathf.Max(to, Mathf.Max(x, y)));
                }
                return (from, to);
            }
        }

        /// <summary>The line's opening nearest a place along; null on a side with none.</summary>
        public KwLineOpening? Nearest(float u)
        {
            KwLineOpening? best = null;
            foreach (KwLineOpening o in Openings)
            {
                if (best == null || Mathf.Abs(o.U - u) < Mathf.Abs(best.U - u))
                    best = o;
            }
            return best;
        }

        /// <summary>
        /// The part holding an opening's middle at a place along (as wide as half twice, with a frame so wide, between two
        /// heights in the aimed part's measures): the one whose allowed reach takes it; null when none does (a rib there).
        /// </summary>
        public KwPartMap? Holding(float u, float half, float wide, float z0, float z1)
        {
            foreach (KwPartMap map in Maps)
            {
                (float lo, float hi) = map.Site.Allowed(half, wide, map.Z(z0), map.Z(z1));
                float at = map.U(u);
                if (at >= lo - 0.001f && at <= hi + 0.001f)
                    return map;
            }
            return null;
        }

        /// <summary>
        /// The place nearest u between lo and hi where a part in line holds an opening so wide (its middle in a part's allowed
        /// reach, <see cref="Holding"/>): off the rib between two planking parts; null when no part holds one between them.
        /// </summary>
        public float? Held(float u, float half, float wide, float z0, float z1, float lo, float hi)
        {
            float? best = null;
            foreach (KwPartMap map in Maps)
            {
                (float a, float c) = map.Site.Allowed(half, wide, map.Z(z0), map.Z(z1));
                (float from, float to) = (Mathf.Max(Mathf.Min(map.Back(a), map.Back(c)), lo), Mathf.Min(Mathf.Max(map.Back(a), map.Back(c)), hi));
                if (a > c || from > to)
                    continue;
                float at = Mathf.Clamp(u, from, to);
                if (best == null || Mathf.Abs(at - u) < Mathf.Abs(best.Value - u))
                    best = at;
            }
            return best;
        }

        /// <summary>The part whose wall reaches a place along at all (the one to say why an opening there is refused); the aimed part when none does.</summary>
        public KwPartMap Reaching(float u)
        {
            foreach (KwPartMap map in Maps)
            {
                (float from, float to) = map.Site.Surface.Reach;
                float at = map.U(u);
                if (at >= from && at <= to)
                    return map;
            }
            return Maps[0];
        }
    }

    /// <summary>
    /// How far apart a side's openings stand (the user's words, 2026-10-25: "The spacing is the gap between the last two
    /// placed on that line"): the last two this player placed with the tool, while both are openings of the line given (found
    /// by where they stand in the world, since a part cut is a new part); else the gap from the opening nearest the aim to
    /// its own nearest neighbour; else the type's own spacing. A placing gives the aimed part's section of its side
    /// (<see cref="KwOpeningLine.Within"/>). Kept for the session, local to this player.
    /// </summary>
    public static class KwOpeningSpacing
    {
        /// <summary>The closest and furthest a side's openings are spaced by what is on it.</summary>
        public const float Closest = 0.5f, Furthest = 8f;

        private static readonly Dictionary<string, List<Vector3>> placed = new Dictionary<string, List<Vector3>>();

        /// <summary>Openings the tool just placed, by their middles in the world, in the order placed (the last two kept).</summary>
        public static void Placed(string tool, IEnumerable<Vector3> middles)
        {
            if (!placed.TryGetValue(tool, out List<Vector3> last))
                placed[tool] = last = new List<Vector3>();
            last.AddRange(middles);
            if (last.Count > 2)
                last.RemoveRange(0, last.Count - 2);
        }

        /// <summary>The line's spacing for the tool (middle: an opening of the line's middle in the world; nearest: the one nearest the aim).</summary>
        public static float Of(string tool, KwOpeningLine line, System.Func<KwLineOpening, Vector3> middle, KwLineOpening? nearest, float fallback)
        {
            if (placed.TryGetValue(tool, out List<Vector3> last) && last.Count == 2)
            {
                KwLineOpening? a = line.Openings.Find(o => (middle(o) - last[0]).sqrMagnitude < 0.0025f);
                KwLineOpening? b = line.Openings.Find(o => (middle(o) - last[1]).sqrMagnitude < 0.0025f);
                if (a != null && b != null && a != b && Spaced(Mathf.Abs(a.U - b.U)))
                    return Mathf.Abs(a.U - b.U);
            }
            KwLineOpening? next = nearest != null ? Neighbour(line, nearest) : null;
            return next != null && Spaced(Mathf.Abs(next.U - nearest!.U)) ? Mathf.Abs(next.U - nearest.U) : fallback;
        }

        // The opening of the line nearest another along, besides itself.
        private static KwLineOpening? Neighbour(KwOpeningLine line, KwLineOpening of)
        {
            KwLineOpening? best = null;
            foreach (KwLineOpening o in line.Openings)
            {
                if (o != of && (best == null || Mathf.Abs(o.U - of.U) < Mathf.Abs(best.U - of.U)))
                    best = o;
            }
            return best;
        }

        private static bool Spaced(float gap) => gap >= Closest && gap <= Furthest;

        /// <summary>Forgets what was placed (offline checks).</summary>
        public static void Forget() => placed.Clear();
    }
}
