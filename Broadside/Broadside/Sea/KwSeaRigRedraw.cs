using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rig redrawn after a sea edit, on every peer (a dedicated server too, which has no fittings to draw):
    /// each changed mast's, bowsprit's or cap's own layers made again from the live blueprint (<see cref="KwSeaRigLayers"/>:
    /// its fitting, its platforms' boxes); a mast changed, every bowsprit's jibs too (they are cut to the stay to the
    /// foremast); then the rig's damage given the rig as read now (<see cref="KwRigDamage.Refit"/>: the rig's pieces, their
    /// hit colliders and looks, the sails' push, the marks read at once), or made the first time a ship gets a rig at sea.
    /// The other masts' ropes, tied on to the ship as it was, are tied on again a mast a frame (<see cref="KwSeaRigRetie"/>)
    /// when a mast or a railing changed. The ship's figures (sail push, sails by kind) are the core's
    /// (<see cref="KwSeaRedraw.Common"/>), its weight its hull's alone, which the rig never changes.
    /// </summary>
    public static class KwSeaRigRedraw
    {
        private static readonly string Rail = KwToolPieces.PartPrefix + "rail";

        public static void Redraw(KwSeaShip ship, KwSeaChanges mine)
        {
            HashSet<int> remade = mine.Indices();
            bool masts = mine.Any(p => p.Prefab == KwSailTool.Made);
            KwBodyMass kept = KwBodyMass.Of(ship.Rigid);
            foreach (KwSeaChange change in mine.All)
                KwShipBody.Step("Broadside sea rig layers", () => KwSeaRigLayers.Remake(ship, change));
            if (masts)
                remade.UnionWith(KwSeaRigLayers.Bowsprits(ship, remade));
            Rig(ship, remade);
            kept.Put(ship.Rigid);
            if (masts)
                KwSeaRigRetie.Queue(ship, remade);
        }

        /// <summary>A railing changed (another kind's edit): the masts' ropes and the bowsprits' jib sheets tied on again.</summary>
        public static void After(KwSeaShip ship, KwSeaChanges all)
        {
            if (!all.Any(p => p.Prefab == Rail))
                return;
            HashSet<int> remade = new HashSet<int>(KwSeaRigLayers.Bowsprits(ship, new HashSet<int>()));
            if (remade.Count > 0)
                Rig(ship, remade);
            KwSeaRigRetie.Queue(ship, remade);
        }

        /// <summary>
        /// The rig's damage told of the remade pieces (made the first time a ship gets a rig at sea), and their new looks and
        /// fittings dressed as the ship's (<see cref="KwSeaRigLayers.Dress"/>).
        /// </summary>
        public static void Rig(KwSeaShip ship, ICollection<int> remade)
        {
            KwBodyMass kept = KwBodyMass.Of(ship.Rigid);
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            if (damage != null)
                damage.Refit(ship.Blueprint, remade);
            else
                damage = KwRigDamage.Add(ship.transform, ship.Blueprint);
            kept.Put(ship.Rigid);
            if (damage != null)
                KwSeaRigLayers.Dress(ship, damage.Parts, remade);
        }
    }

    /// <summary>
    /// One rig piece's own layers on a launched ship, made again after a sea edit (<see cref="KwSeaRigRedraw"/>): its
    /// crow's nest's and fighting top's boxes (on every peer, <see cref="KwMastTopColliders"/>) and its fitting where the
    /// ship is drawn: a mast's sails, ropes, pennant and climb (<see cref="KwShipFittings.Sail"/>, tied on to the ship as it
    /// is now), a bowsprit's jibs (<see cref="KwShipJibs"/>). A cap has no fitting: its look is the rig's
    /// (<see cref="KwRigLook"/>). The old ones go at once from the ship's colliders and finding, and from the screen this
    /// frame.
    /// </summary>
    public static class KwSeaRigLayers
    {
        private const string SailFitting = "sail", SpritFitting = "bowsprit";

        /// <summary>One changed entry: its old piece's layers gone, its new piece's made.</summary>
        public static void Remake(KwSeaShip ship, KwSeaChange change)
        {
            if (change.Old != null && KwSeaRig.Is(change.Old.Prefab))
            {
                KwMastTopColliders.Remove(ship.Colliders, change.Index);
                Gone(Fitting(ship.Fittings, change.Old));
            }
            if (change.New != null && KwSeaRig.Is(change.New.Prefab))
                Make(ship, change.New, change.Index);
        }

        private static void Make(KwSeaShip ship, KwBpPiece piece, int index)
        {
            KwMastTopColliders.Add(ship.Colliders, piece, index);
            if (!KwShipBody.Drawn)
                return;
            if (piece.Prefab == KwSailTool.Made)
                KwShipFittings.Sail(ship.Fittings, piece);
            else if (piece.Prefab == KwBowsprit.Made)
                KwShipJibs.Add(ship.Fittings, ship.Blueprint, piece);
        }

        /// <summary>
        /// The remade pieces' new looks and fittings dressed as the ship's were when it was built: the ship's copies of the
        /// piece materials (<see cref="KwShipMaterials"/>), and the looks casting no shadow (the ship's shadow boxes cast
        /// the masts', <see cref="KwShipShadow"/>). Only where drawn.
        /// </summary>
        public static void Dress(KwSeaShip ship, KwRigParts parts, ICollection<int> remade)
        {
            if (!KwShipBody.Drawn)
                return;
            foreach (KwRigPiece piece in parts.Pieces)
            {
                if (!remade.Contains(piece.Index) || piece.Kind == KwRigPiece.RudderKind)
                    continue;
                Transform? look = ship.Visual.Find("rig" + piece.Index), fitting = Fitting(ship.Fittings, piece.Piece);
                if (look != null)
                    Shadowless(look);
                if (fitting != null)
                    KwShipMaterials.Apply(fitting);
            }
        }

        private static void Shadowless(Transform look)
        {
            KwShipMaterials.Apply(look);
            foreach (MeshRenderer renderer in look.GetComponentsInChildren<MeshRenderer>(true))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A mast's sails and ropes made again, tied on to the ship as it is now (its look and hits are the caller's).</summary>
        public static void Retie(KwSeaShip ship, KwBpPiece mast)
        {
            Gone(Fitting(ship.Fittings, mast));
            KwShipFittings.Sail(ship.Fittings, mast);
        }

        /// <summary>
        /// Every bowsprit but the remade ones: its jibs dressed again on their stay to the masts as they are now (only where
        /// drawn; its look and hits made again with them by the caller); their indices.
        /// </summary>
        public static List<int> Bowsprits(KwSeaShip ship, HashSet<int> remade)
        {
            List<int> again = new List<int>();
            for (int i = 0; i < ship.Blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = ship.Blueprint.Pieces[i];
                if (piece.Prefab != KwBowsprit.Made || remade.Contains(i))
                    continue;
                again.Add(i);
                if (!KwShipBody.Drawn)
                    continue;
                Gone(Fitting(ship.Fittings, piece));
                KwShipJibs.Add(ship.Fittings, ship.Blueprint, piece);
            }
            return again;
        }

        /// <summary>A rig piece's fitting under the ship's fittings at its place: a mast's "sail", a bowsprit's "bowsprit"; null for a cap or none.</summary>
        public static Transform? Fitting(Transform fittings, KwBpPiece piece)
        {
            string? name = piece.Prefab == KwSailTool.Made ? SailFitting : piece.Prefab == KwBowsprit.Made ? SpritFitting : null;
            if (name == null)
                return null;
            foreach (Transform child in fittings)
            {
                if (child.name == name && (child.localPosition - piece.Position).sqrMagnitude < 1e-4f)
                    return child;
            }
            return null;
        }

        // A fitting gone: no longer found by its name, off the screen now, destroyed at the frame's end.
        private static void Gone(Transform? fitting)
        {
            if (fitting == null)
                return;
            fitting.name += "_gone";
            fitting.gameObject.SetActive(false);
            Object.Destroy(fitting.gameObject);
        }
    }
}
