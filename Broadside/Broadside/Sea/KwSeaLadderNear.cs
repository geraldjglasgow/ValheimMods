using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What bounds the free spans of an edge on a launched ship, for the ladder tool's centring at sea (<see cref="KwLadderSpans"/>),
    /// as <see cref="KwLadderNear"/> finds it on the dock but from the ship's blueprint (<see cref="KwSeaLadderShip"/>): what is
    /// under the edge every 0.1 m (<see cref="KwSeaLadderDrop.Under"/>: the floor, the hull's side where the floor below ends,
    /// a fitting in the way), the doorways and windows in the walls right under it (the cabins' walls and raised sides, the
    /// planking's), the cabins' corner posts and the ladders already on it (<see cref="KwLadderFinds"/>). Worked out once an
    /// edge. Plain maths.
    /// </summary>
    public static class KwSeaLadderNear
    {
        private static KwLadderEdge? measured;
        private static List<KwLadderBound> bounds = new List<KwLadderBound>();

        /// <summary>The edge's bounds, worked out once for it.</summary>
        public static List<KwLadderBound> Of(KwSeaLadderShip ship, KwLadderEdge edge)
        {
            if (edge != measured)
                (measured, bounds) = (edge, Measure(ship, edge));
            return bounds;
        }

        // What is under the edge, then what the ship's parts put along it.
        private static List<KwLadderBound> Measure(KwSeaLadderShip ship, KwLadderEdge edge)
        {
            List<(float S, float Y)> floors = new List<(float S, float Y)>();
            List<KwLadderBound> found = KwLadderBelow.Runs(edge, s => Sample(ship, edge, s, floors));
            float none = edge.Middle.y - KwLadder.MaxLength;
            Func<float, float> floor = s => KwLadderBelow.Floor(floors, s, none);
            foreach ((int _, KwBpPiece piece, KwCabinSpec spec, KwRigCabin _) in ship.Cabins)
            {
                Func<Vector3, Vector3> world = kit => KwSeaLadderShip.Out(piece, kit);
                KwLadderFinds.Posts(edge, KwCabinPosts.Of(spec), world, floor, found);
                if (spec.Openings.Count > 0)
                    KwLadderFinds.Openings(edge, KwOpeningSurfaces.Cabin(spec), world, floor, found);
            }
            foreach ((KwBpPiece piece, string saved) in ship.Cut)
                KwLadderFinds.Openings(edge, KwOpeningSurfaces.Of("planking", saved), kit => KwSeaLadderShip.Out(piece, kit), floor, found);
            foreach ((int _, KwBpPiece piece, float _) in ship.Ladders)
                KwLadderFinds.Ladder(edge, piece.Position, piece.Rotation * Vector3.forward, found);
            return found;
        }

        // What is under the edge at s: nothing in the way (a floor), something solid by name, or no floor; the height met.
        private static (KwLadderBoundKind?, string) Sample(KwSeaLadderShip ship, KwLadderEdge edge, float s, List<(float S, float Y)> floors)
        {
            KwSeaLadderFoot foot = KwSeaLadderDrop.Under(ship, edge.At(s), edge.OutwardAt(s), edge.Part, true);
            floors.Add((s, foot.Y));
            if (foot.Stands)
                return (null, "");
            if (foot.What == null || foot.What is KwAnchorHull)
                return (KwLadderBoundKind.NoFloor, KwLadderBound.DeckEnd);
            return (KwLadderBoundKind.Solid, KwSeaLadderSolids.Word(foot.What));
        }
    }
}
