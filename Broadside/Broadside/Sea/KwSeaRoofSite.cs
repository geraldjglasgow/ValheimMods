using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's roof on a launched ship as a roof railing would stand on it, from the blueprint alone (plain arithmetic,
    /// as <see cref="KwRoofSite"/> reads a cabin on the dock): the cabin's index, the railing's origin over the cabin's own
    /// at the roof's top and its level heading, the roof's outline in that frame, a gap at every ladder of the blueprint
    /// coming up on it (<see cref="KwLadderGaps.WithGap"/>), and the roof railing already standing there (<see cref="Railed"/>).
    /// </summary>
    public sealed class KwSeaRoofSite
    {
        private const float Reach = 0.35f;

        private KwSeaRoofSite(int cabin, Vector3 origin, Quaternion turn, List<KwRoofSegment> edges) => (Cabin, Origin, Turn, Edges) = (cabin, origin, turn, edges);

        public int Cabin { get; }
        public Vector3 Origin { get; }
        public Quaternion Turn { get; }
        public List<KwRoofSegment> Edges { get; }

        /// <summary>The roof of the blueprint's cabin a point (the ship's frame) is on or just over; null for none.</summary>
        public static KwSeaRoofSite? At(KwBlueprint blueprint, Vector3 point)
        {
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                KwCabinSpec? spec = KwHouses.IsPart(piece.Prefab) ? Spec(piece) : null;
                if (spec != null && On(piece, spec, point))
                    return Of(i, piece, spec);
            }
            return null;
        }

        // A cabin piece's spec, read once a piece (a cabin changed at sea is a new piece).
        private static KwCabinSpec? Spec(KwBpPiece piece)
        {
            if (Specs.TryGetValue(piece, out KwCabinSpec? spec))
                return spec;
            spec = KwCabinSpec.TryParse(KwSeaPiece.Text(piece), out KwCabinSpec read) ? read : null;
            Specs.Add(piece, spec);
            return spec;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<KwBpPiece, KwCabinSpec?> Specs =
            new System.Runtime.CompilerServices.ConditionalWeakTable<KwBpPiece, KwCabinSpec?>();

        // Whether a point is at a cabin's roof's top and inside its outline (a little either way).
        private static bool On(KwBpPiece piece, KwCabinSpec spec, Vector3 point)
        {
            Vector3 kit = KwMesh.ToKit(KwSeaApply.Back(piece.Rotation) * (point - piece.Position));
            if (Mathf.Abs(kit.z - spec.Top) > Reach || kit.x < spec.X0 - Reach || kit.x > spec.X1 + Reach)
                return false;
            float left = KwCabinOutline.Reach(spec, spec.Left, Mathf.Clamp(kit.x, spec.X0, spec.X1), spec.Top);
            float right = KwCabinOutline.Reach(spec, spec.Right, Mathf.Clamp(kit.x, spec.X0, spec.X1), spec.Top);
            return kit.y < left + Reach && kit.y > -right - Reach;
        }

        /// <summary>A cabin piece's roof site.</summary>
        public static KwSeaRoofSite Of(int index, KwBpPiece piece, KwCabinSpec spec)
        {
            Vector3 top = KwSeaApply.OnShip(piece, new Vector3(0f, 0f, spec.Top));
            Vector3 origin = new Vector3(piece.Position.x, top.y, piece.Position.z);
            Quaternion turn = KwSeaApply.Yaw(piece.Rotation);
            List<KwRoofSegment> edges = new List<KwRoofSegment>();
            foreach ((Vector3 a, Vector3 b, string side) in KwCabinRoofs.KitEdges(spec))
            {
                char letter = KwRoofRail.Letter(side);
                if (letter != '?')
                    edges.Add(new KwRoofSegment(letter, Flat(KwRoofFrame.ToKit(KwSeaApply.OnShip(piece, a), origin, turn)), Flat(KwRoofFrame.ToKit(KwSeaApply.OnShip(piece, b), origin, turn))));
            }
            return new KwSeaRoofSite(index, origin, turn, edges);
        }

        /// <summary>A roof railing on this roof: those sides railed, posts that far apart, a gap at every ladder of the blueprint up to it.</summary>
        public KwRoofRail Rail(KwBlueprint blueprint, float spacing, string on)
        {
            KwRoofRail rail = new KwRoofRail(spacing, on, Edges, new List<Vector2>());
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (piece.Prefab == KwToolPieces.PartPrefix + KwLadderTool.Tool)
                    rail = KwLadderGaps.WithGap(rail, Origin, Turn, piece.Position) ?? rail;
            }
            return rail;
        }

        /// <summary>The roof railing standing on this roof already (at its origin): its index, or -1.</summary>
        public int Railed(KwBlueprint blueprint)
        {
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                if (KwRailStretches.IsRail(piece.Prefab) && (piece.Position - Origin).sqrMagnitude < 0.09f && KwRoofRail.TryParse(KwSeaPiece.Text(piece), out _))
                    return i;
            }
            return -1;
        }

        private static Vector2 Flat(Vector3 kit) => new Vector2(kit.x, kit.y);
    }
}
