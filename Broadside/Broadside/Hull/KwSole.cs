using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hold's floor (the sole): flat floorboards over the ribs and the keel in every planking part, so the crew walks
    /// the hold on boards instead of in the hull's V among the ribs' feet. It is laid as a deck is, in its tier's deck
    /// boards (<see cref="KwDeckBoards"/>) a plank thick, at <see cref="Height"/>: the lowest level a little over the ribs'
    /// floor timbers where the hull is <see cref="Walk"/> wide between the ribs' inner faces, at most halfway up the hull;
    /// a taper's floor slopes from its first rib's height to its last's, so the parts either side meet it. It reaches out
    /// to the planking's inside, as a deck does (<see cref="KwShapes.InsideWidth"/>), so the ribs rise from its edges, and it
    /// ends square where the hull grows narrower than <see cref="Narrowest"/> or the keel climbs up under it (at a stem); in
    /// a part laid from keel end to keel end it starts square the same way, once past the first stem.
    /// Where a deck lies less than <see cref="Headroom"/> over it (a low deck built in the hold) it gives way to that deck
    /// along the stretch the deck covers. Pure numbers from a part's span and stations and the decks round it, so every
    /// peer, a launched ship and a blueprint's ghost lay the same floor from the same pieces. Kit axes.
    /// </summary>
    public static class KwSole
    {
        /// <summary>Metres: the walkway the floor's height leaves between the ribs, the narrowest floor laid, and how near over it a deck takes its place.</summary>
        public const float Walk = 1f, Narrowest = 0.6f, Headroom = 0.5f;

        // over the floor timbers; the highest share of the hull's depth; clear of the keel; the search's step; a deck's
        // line off the part's; a gap's reach across; too short to count
        private const float OverTimbers = 0.01f, Highest = 0.5f, KeelClear = 0.03f, Step = 0.01f, Aside = 0.3f, Wide = 100f, Tiny = 1e-3f;

        /// <summary>The floor's top over the keel line in a hull of this shape and frame.</summary>
        public static float Height(string family, KwFrame frame)
        {
            Vector2[] ribs = KwPlanks.Offset(KwJoints.Section(family, frame), -(KwPlanks.Thick + KwRibs.BarDepth));
            float low = KwRibs.TimberTop + KwPlanks.Thick + OverTimbers, high = Mathf.Max(low, Highest * frame.Height);
            for (int i = 0; low + i * Step < high; i++)
            {
                if (2f * KwShapes.WidthAt(ribs, low + i * Step) >= Walk)
                    return low + i * Step;
            }
            return high;
        }

        /// <summary>
        /// A planking part's floor as a deck (its stations, its height at both ends, its own edge and seed), or null where the
        /// hull is nowhere wide enough for one; <see cref="Dress"/> adds its boards and the decks' gaps. A seated part's floor
        /// counts its height from the level keel line, as its ribs do (<see cref="KwSeat"/>), so floors meet at a rib.
        /// </summary>
        public static KwDef? Def(KwSpan span, List<Station> stations, int seed)
        {
            float level = span.Seat?.Level ?? 0f;
            float a = Height(span.Family, span.FrameA) + level, b = span.FrameA.Same(span.FrameB) ? a : Height(span.Family, span.FrameB) + level;
            KwDef floor = new KwDef("KW_gen_sole", "Hold floor", "deck") { Stations = stations, DeckZ = a, DeckZEnd = b == a ? float.NaN : b, Seed = seed };
            floor.Edge = Edge(stations, KwMesh.DeckHeight(floor), span.TwoEnded);
            return floor.Edge.Count >= 2 ? floor : null;
        }

        /// <summary>The floor ready to build in a tier: the gaps the decks over it make left out, laid in the tier's deck boards.</summary>
        public static KwDef Dress(KwDef floor, List<Rect> gaps, KwBiomeTier tier)
        {
            floor.Holes.AddRange(gaps);
            floor.Boards = KwDeckLayout.Of(tier);
            return floor;
        }

        /// <summary>A floor's seed for its boards: where its part stands along its own line, to the centimetre, so parts in a row lay different boards.</summary>
        public static int Seed(Vector3 position, Vector3 forward) => Mathf.RoundToInt(Vector3.Dot(position, forward) * 100f) ^ 0x50AE;

        /// <summary>
        /// The floor's port edge along a part, as (x, half width): each station's while the floor is laid there, then where it
        /// stops. late: the floor may start further along, where it first has room (a part laid from keel end to keel end
        /// starts at a stem's tip, where the keel stands up under it); otherwise it is laid only when there is room at the start.
        /// </summary>
        public static List<Vector2> Edge(List<Station> stations, Func<float, float> z, bool late = false)
        {
            List<Vector2> edge = new List<Vector2>();
            for (int i = 0; i < stations.Count; i++)
            {
                Station s = stations[i];
                float w = Width(s.Half, z(s.X));
                if (w > 0f && edge.Count == 0 && i > 0)
                    Add(edge, Cross(s, stations[i - 1], z));
                if (w > 0f)
                {
                    edge.Add(new Vector2(s.X, w));
                    continue;
                }
                if (edge.Count > 0)
                    Add(edge, Cross(stations[i - 1], s, z));
                if (edge.Count > 0 || !late)
                    break;
            }
            return edge;
        }

        private static void Add(List<Vector2> edge, Vector2? point)
        {
            if (point != null)
                edge.Add(point.Value);
        }

        // Half the floor's width over a section at its height: the planking's inside there, or 0 where that is narrower
        // than the narrowest floor or the keel comes up under the boards.
        private static float Width(Vector2[] half, float z)
        {
            float w = KwShapes.InsideWidth(half, z);
            return 2f * w >= Narrowest && half[0].y + KeelClear <= z - KwPlanks.Thick ? w : 0f;
        }

        // Where between a station the floor reaches and a neighbour it does not (either way along) the floor ends, found by
        // halving the way between them (their sections blended), so the floor ends square there: (x, half width), or null
        // when that is at the station it reaches.
        private static Vector2? Cross(Station reach, Station miss, Func<float, float> z)
        {
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 12; i++)
            {
                float t = (lo + hi) / 2f;
                if (Width(KwJoints.Blend(reach.Half, miss.Half, t), z(Mathf.Lerp(reach.X, miss.X, t))) > 0f)
                    lo = t;
                else
                    hi = t;
            }
            float x = Mathf.Lerp(reach.X, miss.X, lo);
            return Mathf.Abs(x - reach.X) > Tiny ? new Vector2(x, Width(KwJoints.Blend(reach.Half, miss.Half, lo), z(x))) : (Vector2?)null;
        }

        /// <summary>
        /// The stretches of a part's floor (x along it) that decks take over, as openings across the whole floor: every deck
        /// along the part's keel line (heading either way) lying less than <see cref="Headroom"/> over the floor, or under it,
        /// where it covers the part. part, forward: where the part starts and the way it runs; each deck the same and its
        /// definition, all in one frame (the world, or a blueprint's).
        /// </summary>
        public static List<Rect> Gaps(Vector3 part, Vector3 forward, KwDef floor, IEnumerable<(Vector3 At, Vector3 Forward, KwDef Def)> decks)
        {
            List<Rect> gaps = new List<Rect>();
            List<Vector2> edge = floor.Edge ?? new List<Vector2>();
            float top = float.IsNaN(floor.DeckZEnd) ? floor.DeckZ : Mathf.Max(floor.DeckZ, floor.DeckZEnd);
            foreach ((Vector3 at, Vector3 way, KwDef deck) in decks)
            {
                if (edge.Count < 2 || !Over(part, forward, at, way, deck, out float from, out float to, out float low))
                    continue;
                (from, to) = (Mathf.Max(from, edge[0].x), Mathf.Min(to, edge[edge.Count - 1].x));
                if (to - from > Tiny && low < top + Headroom)
                    gaps.Add(Rect.MinMaxRect(from, -Wide, to, Wide));
            }
            return gaps;
        }

        // Whether a deck lies along a part's keel line, and then the stretch it covers (x along the part) and its lowest
        // height over the part's keel line.
        private static bool Over(Vector3 part, Vector3 forward, Vector3 at, Vector3 way, KwDef deck, out float from, out float to, out float low)
        {
            (from, to, low) = (0f, 0f, 0f);
            Vector3 off = at - part, aside = off - forward * Vector3.Dot(off, forward);
            float turn = Vector3.Dot(forward, way);
            if (Mathf.Abs(turn) < 0.98f || new Vector2(aside.x, aside.z).magnitude > Aside || deck.Stations.Count < 2)
                return false;
            float a = deck.Stations[0].X, c = deck.Stations[deck.Stations.Count - 1].X, along = Vector3.Dot(off, forward), sign = Mathf.Sign(turn);
            Func<float, float> z = KwMesh.DeckHeight(deck);
            (from, to) = (Mathf.Min(along + sign * a, along + sign * c), Mathf.Max(along + sign * a, along + sign * c));
            low = off.y + Mathf.Min(z(a), z(c));
            return true;
        }

        /// <summary>A blueprint's decks, each where it starts, the way it runs (the blueprint's frame) and its definition, for <see cref="Gaps"/>.</summary>
        public static List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks(KwBlueprint blueprint)
        {
            List<(Vector3, Vector3, KwDef)> decks = new List<(Vector3, Vector3, KwDef)>();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                if (tool == "deck")
                    decks.Add((piece.Position, piece.Rotation * Vector3.forward, span.Def(tool)));
            }
            return decks;
        }
    }
}
