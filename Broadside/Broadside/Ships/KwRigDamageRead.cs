using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A blueprint piece read as a rig piece shots can damage (<see cref="KwRigPiece"/>): a mast (its column from 0.1 m
    /// under the deck to its head, as <see cref="KwSailShapes"/> draws it), a bowsprit (its spar from its heel to its tip),
    /// a post cap (an upright as tall as the cap reaches, as thick as its foot) or a rudder; null for any other piece.
    /// </summary>
    internal static class KwRigPieceRead
    {
        private const string Wood = "Wood";

        public static KwRigPiece? Of(KwBlueprint blueprint, int index)
        {
            KwBpPiece piece = KwCapEdit.AsBuilt(blueprint.Pieces[index]);
            string prefab = piece.Prefab;
            if (prefab == KwSailTool.Made)
                return Mast(index, piece);
            if (prefab == KwBowsprit.Made)
                return Sprit(blueprint, index, piece);
            if (KwCaps.Is(prefab))
                return Cap(index, piece);
            return KwRudder.Is(prefab) ? Rudder(blueprint, index, piece) : null;
        }

        private static KwRigPiece? Mast(int index, KwBpPiece piece)
        {
            string saved = KwShipParts.Saved(piece);
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return null;
            float r = KwSailLayout.RadiusOf(mast.Mast);
            KwRigSpar column = new KwRigSpar(KwRigSpar.Column, new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, mast.Mast), r, r * 0.72f);
            KwRigPiece made = new KwRigPiece(index, KwRigPiece.MastKind, piece, column) { Mast = mast, Cost = KwRigCost.Mast(saved), Way = Way(piece) };
            made.Sails.AddRange(KwSailLayout.Of(mast));
            return made;
        }

        private static KwRigPiece? Sprit(KwBlueprint blueprint, int index, KwBpPiece piece)
        {
            if (!KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec))
                return null;
            KwRigSpar spar = new KwRigSpar(KwRigSpar.Column, spec.Along(-KwBowsprit.Heel), spec.Tip, spec.Radius, spec.Radius * 0.7f, 0f, 10);
            KwRigPiece made = new KwRigPiece(index, KwRigPiece.SpritKind, piece, spar) { Cost = KwRigCost.Bowsprit(blueprint, piece), Foremast = Foremast(blueprint, piece) };
            KwJibStay? stay = KwJibStay.InBlueprint(blueprint, piece);
            if (stay == null || spec.Jibs == 0)
                return made;
            float jibs = KwJibPlan.Area(spec, stay);
            (made.Way, made.JibPower, made.JibHalf) = (Way(piece), jibs * KwSailCloth.Drive(spec.Cloth), KwShipSail.Share(stay.Mast, 0, Ship.Speed.Half));
            return made;
        }

        // Which way a piece's sails pull along the ship, as KwSailMix counts them: forward, aft, or not at all across it.
        private static float Way(KwBpPiece piece)
        {
            float facing = Vector3.Dot(piece.Rotation * Vector3.forward, Vector3.forward);
            return Mathf.Abs(facing) < 0.1f ? 0f : Mathf.Sign(facing);
        }

        private static KwRigPiece? Cap(int index, KwBpPiece piece)
        {
            // a cap whose text does not read is still drawn on its own (KwShipFittings.Moves): sized as the standard cap
            bool tool = KwCapSpec.TryParse(KwShipParts.Saved(piece), out KwCapSpec spec);
            if (!tool)
                spec = new KwCapSpec(KwCapSpec.Old(piece.Prefab, out string style) ? style : KwCaps.Styles[0].Id, KwCapForm.PostWidth, KwCapForm.PostDepth, 1f);
            float r = Mathf.Sqrt(spec.Width * spec.Depth / Mathf.PI);
            KwRigSpar upright = new KwRigSpar(KwRigSpar.Column, Vector3.zero, new Vector3(0f, 0f, Height(spec)), r, r * 0.8f);
            List<(string Item, int Amount)> cost = tool ? spec.Cost() : new List<(string Item, int Amount)> { (Wood, 1) };
            return new KwRigPiece(index, KwRigPiece.CapKind, piece, upright) { Cap = spec, Cost = cost };
        }

        /// <summary>How high a post cap reaches over its foot, about: its length and a little for the carving's head.</summary>
        public static float Height(KwCapSpec spec) => 0.2f + spec.Length;

        private static KwRigPiece? Rudder(KwBlueprint blueprint, int index, KwBpPiece piece)
        {
            KwRudderTier? tier = KwRudderTiers.Of(piece.Prefab);
            return tier == null ? null : new KwRigPiece(index, KwRigPiece.RudderKind, piece, default) { Tier = tier, Fit = KwShipParts.Fit(blueprint, piece) };
        }

        // The mast a bowsprit's jibs' stay comes from, as KwJibStay.InBlueprint finds it: its index in the blueprint, or -1.
        private static int Foremast(KwBlueprint blueprint, KwBpPiece sprit)
        {
            List<(Vector3 Foot, KwMastSpec Mast)> masts = new List<(Vector3 Foot, KwMastSpec Mast)>();
            List<int> indices = new List<int>();
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                if (piece.Prefab != KwSailTool.Made || !KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                    continue;
                masts.Add((piece.Position, mast));
                indices.Add(i);
            }
            KwJibStay.Of(sprit.Position, sprit.Rotation, masts, out int found);
            return found >= 0 ? indices[found] : -1;
        }
    }
}
