using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship as the ladder tool sees it at sea (<see cref="KwSeaLadderPlanner"/>), from its blueprint alone, in the
    /// ship's frame (the blueprint's, Unity axes): its decks, cabins, hatches, ladders, roof railings and cut planking by
    /// their place in the blueprint; what is walked on (the decks round their hatch openings, the hold floors and the
    /// cabins' roofs, <see cref="Floor"/>); the hull's sides and the fittings a ladder may not hang through
    /// (<see cref="Blocked"/>). Plain maths, so the hover, the click and a check away from the game read the same ship.
    /// Made once a revision of the ship (<see cref="KwSeaLadderTool"/>).
    /// </summary>
    public sealed class KwSeaLadderShip
    {
        private static readonly string DeckPrefab = KwToolPieces.PartPrefix + "deck", LadderPrefab = KwToolPieces.PartPrefix + KwLadderTool.Tool;
        private static readonly string Rail = KwToolPieces.PartPrefix + KwBiomeCost.Rail, Planking = KwToolPieces.PartPrefix + "planking";

        private readonly List<KwAnchorHull> hulls;
        private readonly List<KwAnchorDeck> walked = new List<KwAnchorDeck>();

        public KwSeaLadderShip(KwBlueprint blueprint)
        {
            Blueprint = blueprint;
            KwBpPiece probe = new KwBpPiece("", Vector3.zero, Quaternion.identity, new List<KwBpValue>());
            hulls = new KwAnchorShip(blueprint.Pieces, probe).Hulls;
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                Take(i, blueprint.Pieces[i]);
            Floors(blueprint);
        }

        public KwBlueprint Blueprint { get; }

        /// <summary>The fittings a ladder may not hang through, each with its piece's place in the blueprint (no ladders: they bound an edge their own way).</summary>
        public List<(int Index, KwAnchorSolid Solid)> Solids { get; } = new List<(int, KwAnchorSolid)>();

        /// <summary>The deck parts: their place in the blueprint, seated shape, and top (whole, its hatch openings not cut).</summary>
        public List<(int Index, KwBpPiece Piece, KwDef Def, KwAnchorDeck Top)> Decks { get; } = new List<(int, KwBpPiece, KwDef, KwAnchorDeck)>();
        public List<(int Index, KwBpPiece Piece, KwCabinSpec Spec, KwRigCabin Solid)> Cabins { get; } = new List<(int, KwBpPiece, KwCabinSpec, KwRigCabin)>();
        public List<(int Index, KwBpPiece Piece, Vector2 Opening)> Hatches { get; } = new List<(int, KwBpPiece, Vector2)>();
        public List<(int Index, KwBpPiece Piece, float Length)> Ladders { get; } = new List<(int, KwBpPiece, float)>();
        public List<(int Index, KwBpPiece Piece, KwRoofRail Rail, string Saved)> RoofRails { get; } = new List<(int, KwBpPiece, KwRoofRail, string)>();

        /// <summary>The planking parts with openings cut in them, and their saved texts.</summary>
        public List<(KwBpPiece Piece, string Saved)> Cut { get; } = new List<(KwBpPiece, string)>();

        // One piece by what it is (a removed one, a tombstone, is none of these).
        private void Take(int index, KwBpPiece piece)
        {
            string prefab = piece.Prefab, saved = KwShipParts.Saved(piece);
            if (KwHatch.Opening(prefab) is Vector2 opening)
                Hatches.Add((index, piece, opening));
            else if (prefab == LadderPrefab && KwLadder.TryLength(saved, out float length))
                Ladders.Add((index, piece, length));
            else if (KwHouses.IsPart(prefab) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                Cabins.Add((index, piece, spec, new KwRigCabin(Pose(piece), spec, KwBiomeTiers.Of(piece.Data))));
            else if (prefab == Rail && KwRoofRail.TryParse(saved, out KwRoofRail rail))
                RoofRails.Add((index, piece, rail, saved));
            else if (prefab == Planking && saved.Contains("|open="))
                Cut.Add((piece, saved));
            if (prefab != LadderPrefab && KwSeaLadderSolids.Fitting(prefab))
                KwSeaLadderSolids.Add(Solids, index, piece, Blueprint);
        }

        // The decks (whole for the aim, round their hatch openings to stand on) and the hold floors less the low decks'
        // stretches, from the seated spans, as the ship's colliders lay them.
        private void Floors(KwBlueprint blueprint)
        {
            List<(Vector3 At, Vector3 Forward, KwDef Def)> decks = KwSole.Decks(blueprint);
            List<(Vector3 Middle, Vector3[] Corners)> openings = Openings();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                KwAnchorPose pose = Pose(piece);
                if (tool == "deck")
                {
                    KwDef def = span.Def(tool);
                    Quaternion back = KwAnchorPose.Back(piece.Rotation);
                    Decks.Add((blueprint.Pieces.IndexOf(piece), piece, def, new KwAnchorDeck(pose, def, new List<Rect>())));
                    walked.Add(new KwAnchorDeck(pose, def, KwDeckHoles.Of(p => back * (p - piece.Position), def, openings)));
                }
                else if (tool == "planking" && KwSole.Def(span, span.Stations(), 0) is KwDef floor)
                    walked.Add(new KwAnchorDeck(pose, floor, KwSole.Gaps(piece.Position, piece.Rotation * Vector3.forward, floor, decks)));
            }
        }

        // Every hatch's middle and its opening's corners, in the ship's frame (as the ship's decks are cut round them).
        private List<(Vector3 Middle, Vector3[] Corners)> Openings()
        {
            List<(Vector3, Vector3[])> openings = new List<(Vector3, Vector3[])>();
            foreach ((int _, KwBpPiece piece, Vector2 opening) in Hatches)
            {
                float hx = opening.x / 2f, hy = opening.y / 2f;
                Vector3[] corners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                    corners[i] = Out(piece, new Vector3(i == 1 || i == 2 ? hx : -hx, i >= 2 ? hy : -hy, 0f));
                openings.Add((piece.Position, corners));
            }
            return openings;
        }

        /// <summary>Whether a piece is a deck part.</summary>
        public static bool IsDeck(KwBpPiece piece) => piece.Prefab == DeckPrefab;

        /// <summary>A piece's pose in the ship's kit frame.</summary>
        public static KwAnchorPose Pose(KwBpPiece piece) => new KwAnchorPose(piece.Position, piece.Rotation);

        /// <summary>A point of a piece's own kit frame in the ship's frame.</summary>
        public static Vector3 Out(KwBpPiece piece, Vector3 kit) => piece.Position + piece.Rotation * KwMesh.ToUnity(kit);

        /// <summary>A point of the ship's frame in a piece's own kit frame.</summary>
        public static Vector3 In(KwBpPiece piece, Vector3 local) => KwMesh.ToKit(KwAnchorPose.Back(piece.Rotation) * (local - piece.Position));

        /// <summary>The highest floor (a deck, a hold's floor, a cabin's roof) under a point of the ship's frame that is lower than a height; null for none.</summary>
        public float? Floor(Vector3 local, float under)
        {
            Vector3 k = KwMesh.ToKit(local);
            float? best = null;
            foreach (KwAnchorDeck deck in walked)
                best = Higher(best, deck.Top(k.x, k.y), under);
            foreach ((int _, KwBpPiece _, KwCabinSpec _, KwRigCabin cabin) in Cabins)
                best = Higher(best, cabin.RoofTop(k), under);
            return best;
        }

        /// <summary>What is at a point of the ship's frame, within a clearance, a piece left out: the hull's side, a fitting; null for nothing.</summary>
        public KwAnchorSolid? Blocked(Vector3 local, float r, int leave)
        {
            Vector3 k = KwMesh.ToKit(local);
            KwAnchorHull? hull = hulls.Find(h => h.Blocks(k, r, false));
            if (hull != null)
                return hull;
            foreach ((int index, KwAnchorSolid solid) in Solids)
            {
                if (index != leave && Near(solid, k, r) && solid.Blocks(k, r, false))
                    return solid;
            }
            return null;
        }

        // Whether a point (kit) is within a solid's ball, so it is worth testing.
        private static bool Near(KwAnchorSolid solid, Vector3 k, float r) =>
            solid.Radius == float.MaxValue || (k - solid.Middle).sqrMagnitude <= (solid.Radius + r) * (solid.Radius + r);

        /// <summary>Whether a point of the ship's frame is inside a cabin (its walls, roof and raised sides).</summary>
        public bool InCabin(Vector3 local, float r)
        {
            Vector3 k = KwMesh.ToKit(local);
            return Cabins.Exists(c => c.Solid.Blocks(k, r, false));
        }

        private static float? Higher(float? best, float? top, float under) =>
            top.HasValue && top.Value < under && (best == null || top.Value > best.Value) ? top : best;
    }
}
