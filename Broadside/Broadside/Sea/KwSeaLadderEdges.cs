using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The open edges the ladder tool stands a ladder against on a launched ship (<see cref="KwSeaLadderTool"/>), as the
    /// dock's (<see cref="KwLadderEdges"/>) but from the ship's blueprint (<see cref="KwSeaLadderShip"/>), in its local
    /// frame: a deck part's two ends, each open when no other deck part of the ship carries on from it at its height; a
    /// cabin roof's sides, joined a side, their ends named from the cabin. Each edge keeps its part's place in the
    /// blueprint. Worked out once a part and a ship's revision (<see cref="Of"/>). Plain maths.
    /// </summary>
    public static class KwSeaLadderEdges
    {
        private const float Past = 0.2f;

        private static KwSeaLadderShip? measured;
        private static readonly Dictionary<int, List<KwLadderEdge>> edges = new Dictionary<int, List<KwLadderEdge>>();

        /// <summary>The deck's end or the roof's side the aim (local) means on a part, when it is open; null and why for none.</summary>
        public static KwLadderEdge? Find(KwSeaLadderShip ship, int part, Vector3 aim, out string why) =>
            KwLadderEdges.Choose(Of(ship, part), aim, KwSeaLadderShip.IsDeck(ship.Blueprint.Pieces[part]), out why);

        /// <summary>A deck part's ends or a cabin's roof sides (none for any other part), worked out once a ship.</summary>
        public static List<KwLadderEdge> Of(KwSeaLadderShip ship, int part)
        {
            if (!ReferenceEquals(ship, measured))
            {
                measured = ship;
                edges.Clear();
            }
            if (!edges.TryGetValue(part, out List<KwLadderEdge> found))
                edges[part] = found = Measure(ship, part);
            return found;
        }

        private static List<KwLadderEdge> Measure(KwSeaLadderShip ship, int part)
        {
            foreach ((int index, KwBpPiece piece, KwDef def, KwAnchorDeck _) in ship.Decks)
            {
                if (index == part)
                    return DeckEnds(ship, index, piece, def);
            }
            foreach ((int index, KwBpPiece piece, KwCabinSpec spec, KwRigCabin _) in ship.Cabins)
            {
                if (index == part)
                    return RoofEdges(piece, spec, index);
            }
            return new List<KwLadderEdge>();
        }

        // A deck part's ends, each open unless another deck carries on from it, both ends of each the hull's sides.
        private static List<KwLadderEdge> DeckEnds(KwSeaLadderShip ship, int index, KwBpPiece deck, KwDef def)
        {
            List<KwLadderEdge> found = new List<KwLadderEdge>();
            foreach ((Vector3 a, Vector3 b, Vector3 way) in KwLadderEdges.Ends(def))
            {
                Vector3 outward = KwLadderEdges.Flat(deck.Rotation * KwMesh.ToUnity(way)).normalized;
                Vector3 la = KwSeaLadderShip.Out(deck, a), lb = KwSeaLadderShip.Out(deck, b);
                bool open = !Carried(ship, index, (la + lb) / 2f + outward * Past);
                found.Add(new KwLadderEdge(new List<Vector3> { la, lb }, new List<Vector3> { outward }, null, open, KwLadderBound.HullSide, KwLadderBound.HullSide, index));
            }
            return found;
        }

        // Whether another deck of the ship carries on at a local point.
        private static bool Carried(KwSeaLadderShip ship, int index, Vector3 point) =>
            ship.Decks.Exists(d => d.Index != index && KwLadderEdges.Carries(d.Def, KwSeaLadderShip.In(d.Piece, point)));

        // A cabin roof's sides at its top, in the local frame (as KwCabinRoofs.Roof lays them in the world).
        private static List<KwLadderEdge> RoofEdges(KwBpPiece cabin, KwCabinSpec spec, int index)
        {
            float top = KwSeaLadderShip.Out(cabin, new Vector3(0f, 0f, spec.Top)).y;
            List<KwRoofEdge> roof = new List<KwRoofEdge>();
            foreach ((Vector3 a, Vector3 b, string side) in KwCabinRoofs.KitEdges(spec))
            {
                Vector3 run = b - a, outward = KwLadderEdges.Flat(cabin.Rotation * KwMesh.ToUnity(new Vector3(-run.y, run.x, 0f)));
                roof.Add(new KwRoofEdge(KwSeaLadderShip.Out(cabin, a), KwSeaLadderShip.Out(cabin, b), outward.normalized, side));
            }
            return KwLadderEdges.RoofRuns(roof, top, spec, null, index);
        }
    }
}
