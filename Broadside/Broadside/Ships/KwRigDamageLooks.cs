using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rig damage drawn (<see cref="KwRigDamage"/>), on the screens that draw the ship, under its visual
    /// holder: each mast's, bowsprit's and post cap's own look (<see cref="KwRigLook"/>) with its fitting (a mast's sails and
    /// ropes, a bowsprit's jibs), each mast's sails (<see cref="KwSailDamageCloth"/>) and each rudder's blade
    /// (<see cref="KwRigDamageRudder"/>). When the marks change: each look cut again; a piece that has just fallen topples
    /// (<see cref="KwMastFall"/>), one already down when the ship loaded is hidden at once, one put back grows back in place (<see cref="KwRigRise"/>); a
    /// bowsprit's jibs are hidden while their foremast is down.
    /// </summary>
    public sealed class KwRigDamageLooks : MonoBehaviour
    {
        private readonly Dictionary<int, KwRigLook> looks = new Dictionary<int, KwRigLook>();
        private readonly List<KwSailDamageCloth> sails = new List<KwSailDamageCloth>();
        private readonly List<KwRigDamageRudder> rudders = new List<KwRigDamageRudder>();
        private KwRigParts parts = null!;

        public static KwRigDamageLooks? Add(KwRigDamage damage)
        {
            Transform? visual = damage.Frame.Find("visual"), fittings = damage.Frame.Find("fittings");
            if (visual == null)
                return null;
            KwRigDamageLooks made = visual.gameObject.AddComponent<KwRigDamageLooks>();
            made.parts = damage.Parts;
            foreach (KwRigPiece piece in damage.Parts.Pieces)
                KwShipBody.Step("Broadside rig look", () => made.Make(visual, fittings, piece));
            return made;
        }

        private void Make(Transform visual, Transform? fittings, KwRigPiece piece)
        {
            if (piece.Kind == KwRigPiece.RudderKind)
            {
                KwRigDamageRudder? rudder = KwRigDamageRudder.Add(KwRigHitFind.Fitting(fittings, "rudder", piece)?.Find(KwRudder.BladeName), piece);
                if (rudder != null)
                    rudders.Add(rudder);
                return;
            }
            KwRigLook? look = KwRigLook.Make(visual, piece);
            if (look == null)
                return;
            looks[piece.Index] = look;
            look.Fitting = piece.Kind == KwRigPiece.CapKind ? null : KwRigHitFind.Fitting(fittings, piece.IsMast ? "sail" : "bowsprit", piece);
            if (piece.IsMast && look.Fitting != null)
                sails.Add(KwSailDamageCloth.Add(look.Fitting, piece));
        }

        /// <summary>
        /// The ship's rig changed at sea (<see cref="KwRigDamage.Refit"/>): the remade pieces' looks taken away and made again
        /// from the rig as read now, with their fittings (made again already); every other look kept, given its piece as read
        /// now (a bowsprit's foremast may be another) and its fitting found again (a bowsprit's jibs may be new).
        /// </summary>
        public void Refit(KwRigDamage damage, ICollection<int> remade)
        {
            parts = damage.Parts;
            Transform? fittings = damage.Frame.Find("fittings");
            foreach (int index in new List<int>(looks.Keys))
                Again(looks[index], remade.Contains(index), fittings);
            sails.RemoveAll(cloth => cloth == null || remade.Contains(cloth.Index));
            foreach (KwRigPiece piece in parts.Pieces)
            {
                if (remade.Contains(piece.Index) && piece.Kind != KwRigPiece.RudderKind)
                    KwShipBody.Step("Broadside rig look", () => Make(transform, fittings, piece));
            }
        }

        // A look taken away (its piece remade or gone), or kept with its piece and fitting as they are now.
        private void Again(KwRigLook look, bool remade, Transform? fittings)
        {
            int index = look.Piece.Index;
            KwRigPiece? now = parts.Get(index);
            if (remade || now == null)
            {
                looks.Remove(index);
                look.gameObject.name += "_gone";
                look.gameObject.SetActive(false);
                Destroy(look.gameObject);
                return;
            }
            look.Piece = now;
            if (now.Kind != KwRigPiece.CapKind)
                look.Fitting = KwRigHitFind.Fitting(fittings, now.IsMast ? "sail" : "bowsprit", now);
        }

        /// <summary>Everything as the rig's condition leaves it; the pieces that fell just now toppling.</summary>
        public void Show(KwRigCondition condition, List<int> fell)
        {
            foreach (KwRigLook look in looks.Values)
                KwShipBody.Step("Broadside rig look shown", () => Shown(look, condition, fell.Contains(look.Piece.Index)));
            foreach (KwSailDamageCloth cloth in sails)
                KwShipBody.Step("Broadside sail damage shown", () => cloth.Show(condition));
            foreach (KwRigDamageRudder rudder in rudders)
                KwShipBody.Step("Broadside rudder damage shown", () => rudder.Show(condition));
            Jibs(condition);
        }

        // A look cut again; toppling if it fell just now, else down or standing as its marks say.
        private static void Shown(KwRigLook look, KwRigCondition condition, bool fellNow)
        {
            bool changed = look.Show(condition);
            KwRigMark? fall = condition.FallOf(look.Piece.Index);
            if (fellNow && fall.HasValue)
            {
                KwMastFall.Begin(look, fall.Value);
                return;
            }
            if (!changed)
                return;
            // down and hidden until now, put back: it grows back in place (KwRigRise)
            bool back = !fall.HasValue && !look.High.gameObject.activeSelf;
            KwMastFall.Stop(look);
            KwMastFall.Gone(look, fall.HasValue);
            if (back)
                KwRigRise.Begin(look);
        }

        // Each standing bowsprit's jibs shown only while the foremast their stay comes from stands.
        private void Jibs(KwRigCondition condition)
        {
            foreach (KwRigLook look in looks.Values)
            {
                KwRigPiece piece = look.Piece;
                if (piece.Kind != KwRigPiece.SpritKind || look.Fitting == null || condition.Fallen(piece.Index) || look.GetComponent<KwMastFall>() != null)
                    continue;
                look.Fitting.gameObject.SetActive(piece.Foremast < 0 || !condition.Fallen(piece.Foremast));
            }
        }
    }
}
