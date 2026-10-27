using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship round a rigged piece (a mast, a bowsprit's jibs), read from its pieces as blueprint pieces in any one frame
    /// (a launched ship's blueprint, the dock's pieces read the same way, <see cref="KwRigSource"/>), all in that piece's kit
    /// frame (x along it, y to its left, z up from its foot): the places its ropes may tie on (<see cref="Edges"/>: the top of
    /// the hull's sides, the outer edges of cabins' roofs, <see cref="KwRigEdges"/>), the cabin whose roof it stands on
    /// (<see cref="Base"/>), the decks' and roofs' tops, and what a rope must never pass through (<see cref="Solids"/>: the
    /// decks, the hull's sides, cabins, railings, masts, sails and yards). Plain maths over the pieces' saved shapes (the hull
    /// as the anchor reads it, <see cref="KwAnchorShip"/>), never the physics, so every peer ties the same ropes.
    /// </summary>
    public sealed class KwRigShip
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        private bool own;

        public KwRigShip(List<KwBpPiece> pieces, KwBpPiece self)
        {
            Hull = new KwAnchorShip(pieces, self);
            Frame = Hull.Hulls.Count > 0 ? Hull.Hulls[0].Pose : new KwAnchorPose(Vector3.zero, Quaternion.identity);
            Tier = Hull.Hulls.Count > 0 ? KwBiomeTiers.Of(Hull.Hulls[0].Piece.Data) : KwBiomeTiers.Meadows;
            foreach (KwBpPiece piece in pieces)
            {
                if (!ReferenceEquals(piece, self))
                    Take(piece, KwAnchorPose.Of(self, piece));
            }
            Base = Cabins.Find(c => c.Carries(Vector3.zero));
            Solids.AddRange(Hull.Hulls);
            Solids.AddRange(Hull.Rails);
            Solids.AddRange(Hull.Decks);
            Solids.AddRange(Cabins);
            Solids.AddRange(RoofRails);
            Edges = KwRigEdges.Of(this);
        }

        /// <summary>The hull's planking, railings and decks as the anchor's cable sees them.</summary>
        public KwAnchorShip Hull { get; }

        /// <summary>The first planking part's frame: the ship's own axes, x along it and y across from its middle line.</summary>
        public KwAnchorPose Frame { get; }

        /// <summary>The hull's tier: a fitting on a deck wears its wood and metal.</summary>
        public KwBiomeTier Tier { get; }

        public List<KwRigCabin> Cabins { get; } = new List<KwRigCabin>();
        public List<KwRigRoofRail> RoofRails { get; } = new List<KwRigRoofRail>();

        /// <summary>The cabin whose roof the piece stands on; null on a deck.</summary>
        public KwRigCabin? Base { get; }

        /// <summary>Every place a rope may tie on, every <see cref="KwRigEdges.Step"/> along each side of the hull and of each roof.</summary>
        public List<KwRigEdge> Edges { get; }

        /// <summary>What a rope must keep out of.</summary>
        public List<KwAnchorSolid> Solids { get; } = new List<KwAnchorSolid>();

        /// <summary>The piece's own sails and yards among the solids (once): a mast's, which its own ropes must miss too.</summary>
        public void Own(List<KwSailLayout> sails)
        {
            if (own)
                return;
            own = true;
            KwRigSails.Add(Solids, new KwAnchorPose(Vector3.zero, Quaternion.identity), sails);
        }

        /// <summary>Where a rope aims along the ship (the ship's x) and the side it is on (+1 the ship's left): the aim's side
        /// of the middle line, or, for an aim near it, of the rope's top.</summary>
        public (float Along, float Side) Place(Vector3 from, Vector3 aim)
        {
            Vector3 a = Frame.Local(aim), f = Frame.Local(from);
            float across = Mathf.Abs(a.y) > 0.3f ? a.y : a.y - f.y;
            return (a.x, across >= 0f ? 1f : -1f);
        }

        /// <summary>The highest deck or roof top under a point (kit x, y) that is lower than a height; null with none.</summary>
        public float? Top(Vector3 p, float under)
        {
            float? best = null;
            foreach (KwAnchorDeck deck in Hull.Decks)
                best = Higher(best, deck.Top(p.x, p.y), under);
            foreach (KwRigCabin cabin in Cabins)
                best = Higher(best, cabin.RoofTop(p), under);
            return best;
        }

        private static float? Higher(float? best, float? top, float under) =>
            top.HasValue && top.Value < under && (best == null || top.Value > best.Value) ? top : best;

        /// <summary>The solids whose balls come within a reach of the run from a to b.</summary>
        public List<KwAnchorSolid> Near(Vector3 a, Vector3 b, float reach)
        {
            List<KwAnchorSolid> near = new List<KwAnchorSolid>();
            foreach (KwAnchorSolid solid in Solids)
            {
                if (KwAnchorRod.Near(solid.Middle, a, b) < solid.Radius + reach)
                    near.Add(solid);
            }
            return near;
        }

        // One piece besides the hull's: a cabin, a roof railing, or another mast with its sails.
        private void Take(KwBpPiece piece, KwAnchorPose pose)
        {
            string saved = KwBpData.Text(piece.Data, SpanHash) ?? "";
            if (KwHouses.IsPart(piece.Prefab) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                Cabins.Add(new KwRigCabin(pose, spec, KwBiomeTiers.Of(piece.Data)));
            else if (piece.Prefab == KwToolPieces.PartPrefix + "rail" && KwRoofRail.TryParse(saved, out KwRoofRail rail))
                RoofRails.Add(new KwRigRoofRail(pose, rail));
            else if (piece.Prefab == KwSailTool.Made && KwMastSpec.TryParse(saved, out KwMastSpec mast))
                Mast(pose, mast);
        }

        // Another mast: its spar as a column, its sails and yards.
        private void Mast(KwAnchorPose pose, KwMastSpec mast)
        {
            Solids.Add(new KwAnchorColumn("a mast", pose.Out(Vector3.zero), mast.Mast, KwSailLayout.RadiusOf(mast.Mast) + 0.03f));
            KwRigSails.Add(Solids, pose, KwSailLayout.Of(mast));
        }
    }
}
