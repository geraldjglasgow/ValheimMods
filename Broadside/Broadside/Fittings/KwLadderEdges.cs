using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The open edges the ladder tool stands a ladder against besides a hatch's (<see cref="KwLadderTool"/>), each a
    /// <see cref="KwLadderEdge"/>: a deck part's two ends (straight across the ship at its first and last station, as wide
    /// as its top there, both ends of it the hull's sides), each open when no other deck part loaded round it carries on
    /// from it at its height (worked out from the deck parts' spans: a point just past the end over another deck's length
    /// and within <see cref="Carry"/> of its top), the end nearer the aim within <see cref="EndReach"/> along the deck; and a
    /// cabin roof's sides (<see cref="KwCabinRoofs"/>: its back, its front, and each side whole, a side along the hull's
    /// line joined from its stretches), each end of one a corner or the hull's side, the one nearest the aim of those long
    /// enough for a ladder. A deck's or a roof's edges are worked out once while it is aimed at (again after a second, as
    /// parts come and go, and after a ladder is built). What stands beyond and on an edge is <see cref="KwLadderDrop"/>'s,
    /// what stands along it <see cref="KwLadderNear"/>'s. On a launched ship the same edges come from its blueprint
    /// (<see cref="KwSeaLadderEdges"/>).
    /// </summary>
    public static class KwLadderEdges
    {
        /// <summary>How far along a deck from its end an aim still means that end; how near another deck's top makes it carry on.</summary>
        public const float EndReach = 1.5f, Carry = 0.35f;

        private const float Past = 0.2f, Again = 1f;
        private static readonly string DeckPrefab = KwToolPieces.PartPrefix + "deck";
        private static readonly List<Piece> Found = new List<Piece>();
        private static Piece? measured;
        private static float measuredAt;
        private static List<KwLadderEdge> edges = new List<KwLadderEdge>();

        /// <summary>The deck's end or the roof's side the aim means, when it is open; null and why for none.</summary>
        public static KwLadderEdge? Find(Piece piece, Vector3 aim, out string why)
        {
            bool deck = Utils.GetPrefabName(piece.gameObject) == DeckPrefab;
            if (piece != measured || Time.time - measuredAt > Again)
                (measured, measuredAt, edges) = (piece, Time.time, deck ? DeckEnds(piece) : RoofEdges(piece));
            return Choose(edges, aim, deck, out why);
        }

        /// <summary>Of a deck's ends or a roof's sides, the one the aim means when it is open (a deck's within reach along it); null and why for none.</summary>
        public static KwLadderEdge? Choose(List<KwLadderEdge> among, Vector3 aim, bool deck, out string why)
        {
            why = deck ? "Aim at a deck's end, where it stops over a drop" : "Aim at a hatch, or at the open edge of a deck or a cabin's roof";
            KwLadderEdge? best = Nearest(among, aim, deck);
            if (best != null && !best.Open)
                why = "Another deck carries on from that end";
            return best != null && best.Open ? best : null;
        }

        /// <summary>Every edge worked out again on the next look, and what stands along them (a ladder was built).</summary>
        public static void Forget()
        {
            measured = null;
            KwLadderNear.Forget();
        }

        // The edge nearest the aim (across the ground); for a deck only an end within reach of it along the deck.
        private static KwLadderEdge? Nearest(List<KwLadderEdge> among, Vector3 aim, bool deck)
        {
            KwLadderEdge? best = null;
            float nearest = deck ? EndReach : float.MaxValue;
            foreach (KwLadderEdge edge in among)
            {
                float off = edge.Off(aim);
                if (off < nearest && edge.Length >= 2f * (KwLadderSpots.Half + KwLadderSpots.Room))
                    (best, nearest) = (edge, off);
            }
            return best;
        }

        /// <summary>A deck's two ends in its own frame (kit axes): each from one side to the other at its top, and the way out.</summary>
        public static List<(Vector3 A, Vector3 B, Vector3 Out)> Ends(KwDef deck)
        {
            Func<float, float> z = KwMesh.DeckHeight(deck);
            List<Vector2> outline = KwShapes.DeckEdge(deck.Stations, z, KwShapes.InsideWidth);
            List<(Vector3, Vector3, Vector3)> ends = new List<(Vector3, Vector3, Vector3)>();
            if (outline.Count < 2)
                return ends;
            foreach ((Vector2 end, float way) in new[] { (outline[0], -1f), (outline[outline.Count - 1], 1f) })
                ends.Add((new Vector3(end.x, -end.y, z(end.x)), new Vector3(end.x, end.y, z(end.x)), new Vector3(way, 0f, 0f)));
            return ends;
        }

        /// <summary>Whether a deck (a point in its own frame) carries on there: over its length and within <see cref="Carry"/> of its top.</summary>
        public static bool Carries(KwDef deck, Vector3 kit)
        {
            List<Vector2> outline = KwShapes.DeckEdge(deck.Stations, KwMesh.DeckHeight(deck), KwShapes.InsideWidth);
            if (outline.Count < 2 || kit.x < outline[0].x - 0.05f || kit.x > outline[outline.Count - 1].x + 0.05f)
                return false;
            return Mathf.Abs(kit.z - KwMesh.DeckHeight(deck)(kit.x)) < Carry;
        }

        // A deck part's ends in the world, each open unless another deck carries on from it, both ends of each the hull's sides.
        private static List<KwLadderEdge> DeckEnds(Piece deck)
        {
            List<KwLadderEdge> found = new List<KwLadderEdge>();
            KwDef? def = DeckDef(deck);
            Transform at = deck.transform;
            foreach ((Vector3 a, Vector3 b, Vector3 way) in def != null ? Ends(def) : new List<(Vector3, Vector3, Vector3)>())
            {
                Vector3 outward = Flat(at.TransformDirection(KwMesh.ToUnity(way))).normalized;
                Vector3 wa = at.TransformPoint(KwMesh.ToUnity(a)), wb = at.TransformPoint(KwMesh.ToUnity(b));
                bool open = !Carried(deck, (wa + wb) / 2f + outward * Past);
                found.Add(new KwLadderEdge(new List<Vector3> { wa, wb }, new List<Vector3> { outward }, deck, open, KwLadderBound.HullSide, KwLadderBound.HullSide));
            }
            return found;
        }

        // Whether another deck loaded round a point carries on there.
        private static bool Carried(Piece deck, Vector3 point)
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(point, 6f, Found);
            bool carried = Found.Exists(other => other != null && other != deck && Utils.GetPrefabName(other.gameObject) == DeckPrefab
                && DeckDef(other) is KwDef def && Carries(def, KwMesh.ToKit(other.transform.InverseTransformPoint(point))));
            Found.Clear();
            return carried;
        }

        // A deck part's shape from its span, its keel ends as they stand.
        private static KwDef? DeckDef(Piece deck)
        {
            ZDO? zdo = deck.m_nview != null && deck.m_nview.IsValid() ? deck.m_nview.GetZDO() : null;
            return zdo != null && KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span) ? KwSpanEnds.Live(span, deck.transform).Def("deck") : null;
        }

        // A cabin roof's sides at its top.
        private static List<KwLadderEdge> RoofEdges(Piece cabin)
        {
            if (!KwCabinRoofs.Roof(cabin, out float top, out List<KwRoofEdge> roof) || roof == null || !KwCabinRoofs.Spec(cabin, out KwCabinSpec spec))
                return new List<KwLadderEdge>();
            return RoofRuns(roof, top, spec, cabin);
        }

        /// <summary>
        /// A cabin roof's edges (world, in <see cref="KwCabinRoofs.Roof"/>'s order) joined into one edge a side at its top's
        /// height, each with its level way out, its ends named from the cabin: a side at the hull ends the back and front at
        /// the hull's side, a drawn-in one at a corner; a side ends at a corner, or at the hull's side where a pointed end
        /// the hull closes brings the two sides together.
        /// </summary>
        public static List<KwLadderEdge> RoofRuns(List<KwRoofEdge> roof, float top, KwCabinSpec spec, Piece? piece, int part = -1)
        {
            List<KwLadderEdge> runs = new List<KwLadderEdge>();
            for (int i = 0; i < roof.Count;)
            {
                List<Vector3> points = new List<Vector3> { Level(roof[i].A, top) }, outs = new List<Vector3>();
                int j = i;
                for (; j < roof.Count && roof[j].Side == roof[i].Side; j++)
                {
                    points.Add(Level(roof[j].B, top));
                    outs.Add(Flat(roof[j].Outward).normalized);
                }
                (string start, string end) = Named(spec, roof[i].Side);
                runs.Add(new KwLadderEdge(points, outs, piece, true, start, end, part));
                i = j;
            }
            return runs;
        }

        // What a roof's side starts and ends at, along it (the back runs right to left, the front left to right, the left
        // side back to front, the right front to back).
        private static (string Start, string End) Named(KwCabinSpec spec, string side) => side switch
        {
            "aft" => (OfSide(spec.Right), OfSide(spec.Left)),
            "fore" => (OfSide(spec.Left), OfSide(spec.Right)),
            "port" => (OfEnd(spec, spec.Back), OfEnd(spec, spec.Front)),
            _ => (OfEnd(spec, spec.Front), OfEnd(spec, spec.Back)),
        };

        private static string OfSide(KwCabinSide side) => side.AtHull ? KwLadderBound.HullSide : KwLadderBound.Corner;

        private static string OfEnd(KwCabinSpec spec, KwCabinEnd end) =>
            end.Hull == KwCabinEnd.Pointed && KwCabinOutline.Closed(spec, end) ? KwLadderBound.HullSide : KwLadderBound.Corner;

        private static Vector3 Level(Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>A way laid level.</summary>
        public static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
