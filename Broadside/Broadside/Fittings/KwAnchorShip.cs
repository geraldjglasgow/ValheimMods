using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship round an anchor windlass, read from its pieces as blueprint pieces in any one frame (a launched ship's
    /// blueprint, or the dock's pieces read the same way, <see cref="KwAnchorDock"/>), all in the windlass's kit frame (x
    /// forward, y left, z up from the deck under it): its planking parts (<see cref="KwAnchorHull"/>), railings
    /// (<see cref="KwAnchorRail"/>), decks with their hatch openings (<see cref="KwAnchorDeck"/>), cabins
    /// (<see cref="KwAnchorCabin"/>), ribs, and what else stands in the way (<see cref="KwAnchorFixtures"/>). Plain maths
    /// over the pieces' saved shapes, never the physics, so every peer works out the same from the same pieces.
    /// </summary>
    public sealed class KwAnchorShip
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        private readonly List<KwAnchorSolid> solids = new List<KwAnchorSolid>();
        private readonly List<KwAnchorSolid> own = KwAnchorFixtures.Own();

        public KwAnchorShip(List<KwBpPiece> pieces, KwBpPiece windlass)
        {
            KwBlueprint ship = new KwBlueprint("");
            ship.Pieces.AddRange(pieces);
            List<(Vector3 Middle, Vector3[] Corners)> hatches = Hatches(pieces);
            foreach (KwBpPiece piece in pieces)
            {
                if (!ReferenceEquals(piece, windlass))
                    Take(piece, KwAnchorPose.Of(windlass, piece), ship, hatches);
            }
            foreach (KwAnchorCabin cabin in Cabins)
                cabin.Holds = cabin.Inside(new Vector3(0f, 0f, 0.45f), 0f);
            solids.AddRange(Hulls);
            solids.AddRange(Rails);
            solids.AddRange(Decks);
            solids.AddRange(Cabins);
        }

        public List<KwAnchorHull> Hulls { get; } = new List<KwAnchorHull>();
        public List<KwAnchorRail> Rails { get; } = new List<KwAnchorRail>();
        public List<KwAnchorDeck> Decks { get; } = new List<KwAnchorDeck>();
        public List<KwAnchorCabin> Cabins { get; } = new List<KwAnchorCabin>();

        /// <summary>Where each rib stands (windlass kit).</summary>
        public List<Vector3> Ribs { get; } = new List<Vector3>();

        /// <summary>Whether the windlass stands inside a cabin (its cable then goes out through a hawse pipe in a side at the hull).</summary>
        public bool Housed => Cabins.Exists(c => c.Holds);

        /// <summary>What is in the way at a point with a clearance (null: nothing); the windlass's own frame counted when asked.</summary>
        public string? Blocker(Vector3 p, float r, bool outboard, bool withOwn = false) => Blocker(p, r, outboard, solids) ?? (withOwn ? Blocker(p, r, outboard, own) : null);

        /// <summary>What among some solids is in the way at a point with a clearance (null: nothing).</summary>
        public static string? Blocker(Vector3 p, float r, bool outboard, List<KwAnchorSolid> among)
        {
            foreach (KwAnchorSolid solid in among)
            {
                if (solid.Blocks(p, r, outboard))
                    return solid.What;
            }
            return null;
        }

        /// <summary>The solids (the windlass's own frame among them) whose balls come within a reach of the run from a to b.</summary>
        public List<KwAnchorSolid> Near(Vector3 a, Vector3 b, float reach)
        {
            List<KwAnchorSolid> near = new List<KwAnchorSolid>();
            foreach (List<KwAnchorSolid> list in new[] { solids, own })
            {
                foreach (KwAnchorSolid solid in list)
                {
                    if (KwAnchorRod.Near(solid.Middle, a, b) < solid.Radius + reach)
                        near.Add(solid);
                }
            }
            return near;
        }

        /// <summary>The deck's top under a point (windlass kit x, y) nearest a height, within a reach of it; null with none.</summary>
        public float? DeckTop(float x, float y, float near, float within)
        {
            float? best = null;
            foreach (KwAnchorDeck deck in Decks)
            {
                float? top = deck.Top(x, y);
                if (top.HasValue && Mathf.Abs(top.Value - near) <= within && (best == null || Mathf.Abs(top.Value - near) < Mathf.Abs(best.Value - near)))
                    best = top;
            }
            return best;
        }

        // One piece: a hull part, a railing, a deck, a cabin, a rib, or a fitting in the way.
        private void Take(KwBpPiece piece, KwAnchorPose pose, KwBlueprint ship, List<(Vector3 Middle, Vector3[] Corners)> hatches)
        {
            string saved = KwBpData.Text(piece.Data, SpanHash) ?? "";
            if (KwHouses.IsPart(piece.Prefab))
            {
                if (KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                    Cabins.Add(new KwAnchorCabin(pose, spec));
                return;
            }
            if (piece.Prefab == KwRibs.Made)
                Ribs.Add(pose.Out(Vector3.zero));
            else if (Part(piece, pose, saved, hatches))
                return;
            KwAnchorFixtures.Add(solids, piece, pose, ship);
        }

        // A planking part, a railing or a deck read from its span; false for any other piece.
        private bool Part(KwBpPiece piece, KwAnchorPose pose, string saved, List<(Vector3 Middle, Vector3[] Corners)> hatches)
        {
            string tool = piece.Prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) ? piece.Prefab.Substring(KwToolPieces.PartPrefix.Length) : "";
            if ((tool != "planking" && tool != "deck" && tool != "rail") || !KwSpan.TryParse(saved, out KwSpan span))
                return false;
            if (tool == "planking")
                Hulls.Add(new KwAnchorHull(piece, pose, span));
            else if (tool == "rail")
                Rails.Add(new KwAnchorRail(pose, span, KwShieldRecord.Read(KwBpData.Text(piece.Data, KwShieldRecord.Hash)).Keys));
            else
            {
                KwDef def = span.Def(tool);
                Quaternion back = KwAnchorPose.Back(piece.Rotation);
                Decks.Add(new KwAnchorDeck(pose, def, KwDeckHoles.Of(p => back * (p - piece.Position), def, hatches)));
            }
            return true;
        }

        // Every hatch's middle and its opening's corners, in the pieces' frame (as a launched ship's are, KwShipDeckHoles).
        private static List<(Vector3 Middle, Vector3[] Corners)> Hatches(List<KwBpPiece> pieces)
        {
            List<(Vector3, Vector3[])> hatches = new List<(Vector3, Vector3[])>();
            foreach (KwBpPiece piece in pieces)
            {
                Vector2? opening = KwHatch.Opening(piece.Prefab);
                if (opening == null)
                    continue;
                float hx = opening.Value.x / 2f, hy = opening.Value.y / 2f;
                Vector3[] corners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                    corners[i] = piece.Position + piece.Rotation * KwMesh.ToUnity(new Vector3(i == 1 || i == 2 ? hx : -hx, i >= 2 ? hy : -hy, 0f));
                hatches.Add((piece.Position, corners));
            }
            return hatches;
        }
    }
}
