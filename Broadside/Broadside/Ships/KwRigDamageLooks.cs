using System.Collections.Generic;
using System.Diagnostics;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rig damage drawn (<see cref="KwRigDamage"/>), on the screens that draw the ship, under its visual
    /// holder: each mast's, bowsprit's and post cap's own look (<see cref="KwRigLook"/>) with its fitting (a mast's sails and
    /// ropes, a bowsprit's jibs), each mast's sails (<see cref="KwSailDamageCloth"/>) and each rudder's blade
    /// (<see cref="KwRigDamageRudder"/>). When the marks change: each look cut again; a piece that has just fallen topples
    /// (<see cref="KwMastFall"/>), one already down when the ship loaded is hidden at once, one put back grows back in place (<see cref="KwRigRise"/>); a
    /// bowsprit's jibs are hidden while their foremast is down. The looks, the masts' sails (a sail at a time) and the
    /// rudders are shown in turn while the frame's share lasts (<see cref="KwFrameShare.Looks"/>), each as the marks are
    /// when its turn comes, the jibs once all are: a ship loaded with its rig damage (every peer that loads it draws it all)
    /// takes a few frames, not one long one.
    /// </summary>
    public sealed class KwRigDamageLooks : MonoBehaviour
    {
        private readonly Dictionary<int, KwRigLook> looks = new Dictionary<int, KwRigLook>();
        private readonly List<KwSailDamageCloth> sails = new List<KwSailDamageCloth>();
        private readonly List<KwRigDamageRudder> rudders = new List<KwRigDamageRudder>();
        private readonly List<(Component Item, bool Fell)> waiting = new List<(Component Item, bool Fell)>();
        private KwRigParts parts = null!;
        private KwRigCondition? condition;
        private bool jibs;

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
        /// now (a bowsprit's foremast may be another) and its fitting found again (a bowsprit's jibs may be new). A look or
        /// sails taken away wait no more to be shown (their objects go at the frame's end).
        /// </summary>
        public void Refit(KwRigDamage damage, ICollection<int> remade)
        {
            parts = damage.Parts;
            Transform? fittings = damage.Frame.Find("fittings");
            foreach (int index in new List<int>(looks.Keys))
                Again(looks[index], remade.Contains(index), fittings);
            sails.RemoveAll(cloth => cloth == null || remade.Contains(cloth.Index));
            waiting.RemoveAll(w => w.Item is KwRigLook look ? !looks.ContainsValue(look) : w.Item is KwSailDamageCloth cloth && !sails.Contains(cloth));
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

        /// <summary>
        /// Everything to be shown as the rig's condition leaves it, in turn from this frame on (see the class); the pieces
        /// that fell just now toppling.
        /// </summary>
        public void Show(KwRigCondition condition, List<int> fell)
        {
            this.condition = condition;
            foreach (KwRigLook look in looks.Values)
                Wait(look, fell.Contains(look.Piece.Index));
            foreach (KwSailDamageCloth cloth in sails)
                Wait(cloth, false);
            foreach (KwRigDamageRudder rudder in rudders)
                Wait(rudder, false);
            jibs = true;
        }

        // An item to show in its turn, once: one waiting already keeps its place and a fall it is to show.
        private void Wait(Component item, bool fell)
        {
            for (int i = 0; i < waiting.Count; i++)
            {
                if (waiting[i].Item == item)
                {
                    waiting[i] = (item, waiting[i].Fell || fell);
                    return;
                }
            }
            waiting.Add((item, fell));
        }

        // Each waiting item a step in turn while the frame's share lasts, the jibs once none waits.
        private void Update()
        {
            if (condition == null || (waiting.Count == 0 && !jibs))
                return;
            for (int i = 0; i < waiting.Count && KwFrameShare.Looks.Open(); i++)
            {
                if (Next(waiting[i], condition))
                    waiting.RemoveAt(i--);
            }
            if (waiting.Count == 0 && jibs)
            {
                jibs = false;
                Jibs(condition);
            }
        }

        // A waiting item's step as the condition now leaves it, its time counted against the frame's share: true once it is
        // all shown (a mast's sails take a step a sail, more while a sail's holes are cut) or gone.
        private static bool Next((Component Item, bool Fell) waits, KwRigCondition now)
        {
            long start = Stopwatch.GetTimestamp();
            bool done = waits.Item == null || Showing(waits.Item, waits.Fell, now);
            KwFrameShare.Looks.Spend(Stopwatch.GetTimestamp() - start);
            return done;
        }

        // One item's step (a throw reported by the guard and the item let go): true once it is all shown.
        private static bool Showing(Component item, bool fell, KwRigCondition now)
        {
            try
            {
                return Guard.Run("Broadside rig damage shown", static (w, c) => Step(w.Item, w.Fell, c), (Item: item, Fell: fell), now);
            }
            catch (System.Exception)
            {
                return true;
            }
        }

        private static bool Step(Component item, bool fell, KwRigCondition now)
        {
            switch (item)
            {
                case KwRigLook look:
                    Shown(look, now, fell);
                    return true;
                case KwSailDamageCloth cloth:
                    return cloth.Step(now);
                case KwRigDamageRudder rudder:
                    rudder.Show(now);
                    return true;
                default:
                    return true;
            }
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
            KwMastFall.Gone(look, fall);
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
