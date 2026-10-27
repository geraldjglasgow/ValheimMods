using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship as its boarding ladder sees it (<see cref="KwShipLadderFind"/>), from its blueprint alone, in the
    /// blueprint's frame as kit axes (x forward, y to port, z up): the top of the hull's sides and the outer edges of its
    /// cabins' roofs (the ropes' tie places, <see cref="KwRigShip"/>: none under a cabin's raised side or a higher planking
    /// part), what is walked on (the decks round their hatch openings and the hold floors as the ship's colliders lay them,
    /// <see cref="KwShipColliders"/>, from the seated spans; the cabins' roofs), the fittings in the way
    /// (<see cref="KwAnchorFixtures"/>), the openings in the hull's sides and the cabins' raised sides
    /// (<see cref="KwShipLadderHoles"/>), where the anchor's cable leaves the ship (<see cref="KwAnchorPlan"/>), the
    /// rudders' posts and the waterline. Plain maths over the saved shapes, so every peer, a dedicated server too, places the
    /// ladder the same.
    /// </summary>
    public sealed class KwShipLadderShip
    {
        public KwShipLadderShip(KwBlueprint blueprint, KwBpHull hull)
        {
            KwBpPiece probe = new KwBpPiece("", Vector3.zero, Quaternion.identity, new List<KwBpValue>());
            Rig = new KwRigShip(blueprint.Pieces, probe);
            Rooms = Rig.Cabins.ConvertAll(c => new KwAnchorCabin(c.Pose, c.Spec));
            Holes = KwShipLadderHoles.Of(Rig);
            (Water, Widest) = (hull.Keel + KwShipTuning.Draught(hull), hull.SectionZ);
            Floors(blueprint);
            foreach (KwBpPiece piece in blueprint.Pieces)
                Take(piece, blueprint);
            Anchor(blueprint);
        }

        /// <summary>The hull's planking, railings and decks, its cabins, roof railings and tie places.</summary>
        public KwRigShip Rig { get; }

        /// <summary>Each cabin's room (inside its walls and under its roof), in the order of <see cref="KwRigShip.Cabins"/>.</summary>
        public List<KwAnchorCabin> Rooms { get; }

        public List<KwLadderHole> Holes { get; }

        /// <summary>The waterline's height and where the hull's widest section stands along the ship.</summary>
        public float Water { get; }
        public float Widest { get; }

        /// <summary>What is walked on: the decks and, marked, the hold floors.</summary>
        public List<(KwAnchorDeck Deck, bool Hold)> Walked { get; } = new List<(KwAnchorDeck Deck, bool Hold)>();

        /// <summary>The fittings: masts, ladders, hatches, posts, the bowsprit, rudders, caps, windlasses, a wheel, a name board.</summary>
        public List<KwAnchorSolid> Fixtures { get; } = new List<KwAnchorSolid>();

        /// <summary>Where the anchor's cable leaves the ship: its fairlead, the hawse and the weighed anchor's ring.</summary>
        public List<Vector3> Cable { get; } = new List<Vector3>();

        /// <summary>Each rudder's post.</summary>
        public List<Vector3> Posts { get; } = new List<Vector3>();

        // The decks round their hatch openings and the hold floors less the low decks' stretches, from the seated spans.
        private void Floors(KwBlueprint blueprint)
        {
            List<(Vector3 Middle, Vector3[] Corners)> hatches = Hatches(blueprint.Pieces);
            List<(Vector3 At, Vector3 Forward, KwDef Def)> decks = KwSole.Decks(blueprint);
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                KwAnchorPose pose = new KwAnchorPose(piece.Position, piece.Rotation);
                Quaternion back = KwAnchorPose.Back(piece.Rotation);
                if (tool == "deck")
                {
                    KwDef def = span.Def(tool);
                    Walked.Add((new KwAnchorDeck(pose, def, KwDeckHoles.Of(p => back * (p - piece.Position), def, hatches)), false));
                }
                else if (tool == "planking" && KwSole.Def(span, span.Stations(), 0) is KwDef floor)
                    Walked.Add((new KwAnchorDeck(pose, floor, KwSole.Gaps(piece.Position, piece.Rotation * Vector3.forward, floor, decks)), true));
            }
        }

        // Every hatch's middle and its opening's corners, in the blueprint's frame (as a launched ship's are).
        private static List<(Vector3 Middle, Vector3[] Corners)> Hatches(List<KwBpPiece> pieces)
        {
            List<(Vector3, Vector3[])> hatches = new List<(Vector3, Vector3[])>();
            foreach (KwBpPiece piece in pieces)
            {
                if (!(KwHatch.Opening(piece.Prefab) is Vector2 opening))
                    continue;
                float hx = opening.x / 2f, hy = opening.y / 2f;
                Vector3[] corners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                    corners[i] = piece.Position + piece.Rotation * KwMesh.ToUnity(new Vector3(i == 1 || i == 2 ? hx : -hx, i >= 2 ? hy : -hy, 0f));
                hatches.Add((piece.Position, corners));
            }
            return hatches;
        }

        // A fitting in the way, from any piece but the hull's own parts and the cabins (read already); a rudder's post too.
        private void Take(KwBpPiece piece, KwBlueprint blueprint)
        {
            string prefab = piece.Prefab;
            string tool = prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) ? prefab.Substring(KwToolPieces.PartPrefix.Length) : "";
            if (KwHouses.IsPart(prefab) || tool == "planking" || tool == "deck" || tool == "rail")
                return;
            KwAnchorFixtures.Add(Fixtures, piece, new KwAnchorPose(piece.Position, piece.Rotation), blueprint);
            if (KwRudder.Is(prefab))
                Posts.Add(KwMesh.ToKit(piece.Position));
        }

        // The first windlass's lead overboard, as the ship works it out (KwShipSailing): where its cable leaves the ship.
        private void Anchor(KwBlueprint blueprint)
        {
            KwBpPiece? windlass = blueprint.Pieces.Find(p => KwAnchor.Is(p.Prefab));
            KwAnchorLead? lead = windlass != null ? Lead(blueprint, windlass) : null;
            if (lead == null || !lead.Found)
                return;
            foreach (Vector3 p in new[] { lead.Spot!.At, lead.Hawse, lead.Ring })
                Cable.Add(KwMesh.ToKit(windlass!.Position + windlass.Rotation * KwMesh.ToUnity(p)));
        }

        private static KwAnchorLead? Lead(KwBlueprint blueprint, KwBpPiece windlass)
        {
            try
            {
                return KwAnchorPlan.For(blueprint.Pieces, windlass);
            }
            catch (System.Exception)
            {
                return null; // the ship's own anchor step says why; the ladder keeps clear of nothing
            }
        }
    }
}
