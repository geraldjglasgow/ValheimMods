using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rig hit colliders (<see cref="KwRigDamageBoxes"/>), on every peer, a dedicated server too, under
    /// the ship's frame ("rig_hits"): each mast's column, yards, booms, gaffs and sails, each bowsprit's spar, each post
    /// cap and each rudder's blade (<see cref="KwRigHitSet"/>), so the game's projectiles and the shot router's trace meet
    /// them. They collide with nothing and the body's mass, centre and inertia are put back as they were after each change
    /// (<see cref="KwBodyMass"/>), so the ship floats and turns as before. The sails' boxes follow the sails set by the
    /// ship's speed; where the ship is drawn, a sail along the ship's boom, gaff and cloth swing with it
    /// (<see cref="KwBoomSwing"/>) and a rudder's drawn blade's own box is tagged. A fallen piece keeps only its stump's.
    /// </summary>
    public sealed class KwRigDamageHits : MonoBehaviour
    {
        private const float Every = 0.25f;
        private readonly List<KwRigHitSet> sets = new List<KwRigHitSet>();
        private Ship? ship;
        private Rigidbody? body;
        private Ship.Speed speed = (Ship.Speed)(-1);
        private float looked;

        public static KwRigDamageHits Add(KwRigDamage damage)
        {
            Transform holder = KwRigDamageBoxes.Holder(damage.Frame, "rig_hits", Vector3.zero, Quaternion.identity);
            KwRigDamageHits hits = holder.gameObject.AddComponent<KwRigDamageHits>();
            (hits.ship, hits.body) = (damage.GetComponent<Ship>(), damage.GetComponent<Rigidbody>());
            Transform? fittings = damage.Frame.Find("fittings"), colliders = damage.Frame.Find("colliders");
            hits.Kept(() =>
            {
                foreach (KwRigPiece piece in damage.Parts.Pieces)
                    hits.sets.Add(KwRigHitSet.Make(holder, piece, fittings, colliders));
            });
            return hits;
        }

        /// <summary>How much of a mast's sail i is set now, by the ship's speed as this peer has it (all of it with no ship).</summary>
        public static float Set(Ship? ship, KwRigPiece mast, int i) =>
            ship != null && mast.Mast != null ? KwShipSail.Share(mast.Mast, i, ship.GetSpeedSetting()) : 1f;

        private void Update()
        {
            foreach (KwRigHitSet set in sets)
                set.Swing();
            if (ship == null || Time.time - looked < Every)
                return;
            looked = Time.time;
            Ship.Speed now = ship.GetSpeedSetting();
            if (now == speed)
                return;
            speed = now;
            Kept(() => sets.ForEach(set => set.Hoist(ship)));
        }

        /// <summary>The colliders as the rig's damage leaves them: a fallen piece's stump alone.</summary>
        public void Show(KwRigCondition condition) => Kept(() => sets.ForEach(set => set.Show(condition)));

        /// <summary>
        /// The ship's rig changed at sea (<see cref="KwRigDamage.Refit"/>): the remade pieces' colliders taken away at once
        /// and made again from the rig as read now (a removed piece's not), set as the ship's sails are now; the rest kept.
        /// </summary>
        public void Refit(KwRigDamage damage, ICollection<int> remade)
        {
            Transform? fittings = damage.Frame.Find("fittings"), colliders = damage.Frame.Find("colliders");
            Kept(() =>
            {
                sets.RemoveAll(set => set.Drop(remade));
                foreach (KwRigPiece piece in damage.Parts.Pieces)
                {
                    if (!remade.Contains(piece.Index))
                        continue;
                    KwRigHitSet set = KwRigHitSet.Make(transform, piece, fittings, colliders);
                    if (ship != null)
                        set.Hoist(ship);
                    sets.Add(set);
                }
            });
        }

        // A change to the colliders with the body's mass, centre and inertia kept as they were.
        private void Kept(Action change)
        {
            KwBodyMass kept = KwBodyMass.Of(body);
            change();
            kept.Put(body);
        }
    }

    /// <summary>
    /// One rig piece's hit colliders (<see cref="KwRigDamageHits"/>), under a frame at its place: a mast's column, each
    /// square sail's yard and sail, each sail along the ship's boom, gaff and sail under a frame turned with its boom; a
    /// bowsprit's spar; a cap's box; a rudder's blade (the drawn blade's own box, tagged, so it turns with it; else a box at rest).
    /// </summary>
    internal sealed class KwRigHitSet
    {
        private readonly List<(Collider Box, int Part)> parts = new List<(Collider Box, int Part)>();
        private readonly Dictionary<int, (Transform Swing, Transform? Boom)> swings = new Dictionary<int, (Transform Swing, Transform? Boom)>();
        private readonly List<(Collider Box, float Up)> tops = new List<(Collider Box, float Up)>();
        private KwRigPiece piece = null!;
        private Transform frame = null!;
        private Collider? main;
        private bool down;

        public static KwRigHitSet Make(Transform holder, KwRigPiece piece, Transform? fittings, Transform? colliders)
        {
            KwRigHitSet set = new KwRigHitSet { piece = piece };
            set.frame = KwRigDamageBoxes.Holder(holder, "piece" + piece.Index, piece.Piece.Position, piece.Piece.Rotation);
            if (piece.IsMast)
            {
                set.Mast(KwRigHitFind.Fitting(fittings, "sail", piece));
                set.tops.AddRange(KwRigHitFind.Tops(colliders, piece));
            }
            else if (piece.Kind == KwRigPiece.SpritKind)
                set.main = KwRigDamageBoxes.Spar(set.frame, piece.Main, KwShipHitRouter.Bowsprit, piece.Index, KwRigSpar.Column);
            else if (piece.Kind == KwRigPiece.CapKind)
                set.main = KwRigDamageBoxes.Box(set.frame, KwRigHitFind.CapMiddle(piece), KwRigHitFind.CapSize(piece), KwShipHitRouter.Cap, piece.Index, -1);
            else
                set.Rudder(KwRigHitFind.Fitting(fittings, "rudder", piece)?.Find(KwRudder.BladeName));
            return set;
        }

        // A mast's column, then each sail's spars and cloth.
        private void Mast(Transform? fitting)
        {
            main = KwRigDamageBoxes.Spar(frame, piece.Main, KwShipHitRouter.Mast, piece.Index, KwRigSpar.Column);
            for (int i = 0; i < piece.Sails.Count; i++)
            {
                KwSailLayout s = piece.Sails[i];
                if (s.Square)
                {
                    parts.Add((KwRigDamageBoxes.Spar(frame, KwRigPiece.Yard(s, i), KwShipHitRouter.Yard, piece.Index, i), i));
                    (Vector3 middle, Vector3 size) = KwRigDamageBoxes.Square(s, 1f);
                    parts.Add((Sail(frame, middle, size, i), i));
                    continue;
                }
                Along(i, s, fitting);
            }
        }

        // A sail along the ship: its boom, gaff and cloth under a frame on the mast's axis that turns as its boom swings.
        private void Along(int i, KwSailLayout s, Transform? fitting)
        {
            Transform swing = KwRigDamageBoxes.Holder(frame, "swing" + i, Vector3.zero, Quaternion.identity);
            swings[i] = (swing, fitting != null ? fitting.Find("boom" + i) : null);
            parts.Add((KwRigDamageBoxes.Spar(swing, KwRigPiece.BoomOf(s, i), KwShipHitRouter.Yard, piece.Index, KwRigSpar.Boom + i), KwRigSpar.Boom + i));
            if (s.Spec.Shape == 2)
                parts.Add((KwRigDamageBoxes.Spar(swing, KwRigPiece.GaffOf(s, i, 1f), KwShipHitRouter.Yard, piece.Index, KwRigSpar.Gaff + i), KwRigSpar.Gaff + i));
            (Vector3 middle, Vector3 size) = KwRigDamageBoxes.Along(s, 1f);
            parts.Add((Sail(swing, middle, size, i), i));
        }

        // A sail's box, a shot passing through it holing the cloth (KwSailDamagePierce).
        private BoxCollider Sail(Transform at, Vector3 middle, Vector3 size, int i)
        {
            BoxCollider box = KwRigDamageBoxes.Box(at, middle, size, KwShipHitRouter.Sail, piece.Index, i);
            KwSailDamagePierce pierce = box.gameObject.AddComponent<KwSailDamagePierce>();
            (pierce.Piece, pierce.Sail, pierce.Layout) = (piece.Index, i, piece.Sails[i]);
            return box;
        }

        // A rudder's blade: the drawn blade's own box tagged (one box, so a shot meets the blade once), or a box of the
        // rig's where the blade hangs at rest (a dedicated server draws none).
        private void Rudder(Transform? blade)
        {
            if (blade != null && blade.TryGetComponent(out BoxCollider own))
            {
                KwShipHitRouter.Tag(blade.gameObject, KwShipHitRouter.Rudder, piece.Index);
                main = own;
                return;
            }
            (Vector3 middle, Vector3 size) = KwRigBlade.Box(piece);
            Transform at = KwRigDamageBoxes.Holder(frame, "blade", KwMesh.ToUnity(piece.Tier!.Pivot(piece.Fit)), Quaternion.identity);
            main = KwRigDamageBoxes.Box(at, middle, size, KwShipHitRouter.Rudder, piece.Index, -1);
        }

        /// <summary>Each sail along the ship's frame turned as its drawn boom is (only where the ship is drawn).</summary>
        public void Swing()
        {
            foreach ((Transform swing, Transform? boom) in swings.Values)
            {
                if (boom != null && swing.localRotation != boom.localRotation)
                    swing.localRotation = boom.localRotation;
            }
        }

        /// <summary>The sails' boxes and gaffs as set at the ship's speed.</summary>
        public void Hoist(Ship ship)
        {
            if (!piece.IsMast || down)
                return;
            foreach ((Collider box, int part) in parts)
            {
                int i = part % 100;
                float f = KwRigDamageHits.Set(ship, piece, i);
                KwSailLayout s = piece.Sails[i];
                if (box is BoxCollider cloth)
                    Placed(cloth, s.Square ? KwRigDamageBoxes.Square(s, f) : KwRigDamageBoxes.Along(s, f));
                else if (part >= KwRigSpar.Gaff && box is CapsuleCollider gaff)
                    KwRigDamageBoxes.Size(gaff, KwRigPiece.GaffOf(s, i, Mathf.Max(f, 0.05f)));
            }
        }

        private static void Placed(BoxCollider box, (Vector3 Middle, Vector3 Size) at) => KwRigDamageBoxes.Place(box, at.Middle, at.Size);

        /// <summary>Its colliders taken away at once when its piece is one of these (true); else kept (false).</summary>
        public bool Drop(ICollection<int> remade)
        {
            if (!remade.Contains(piece.Index))
                return false;
            if (frame != null)
                UnityEngine.Object.DestroyImmediate(frame.gameObject);
            return true;
        }

        /// <summary>
        /// A fallen piece keeps its stump's collider and those of the sails, yards, booms and gaffs it still carries (a mast's
        /// sails hung below the break, <see cref="KwRigCondition.SailDown"/>); one put back has all of them again.
        /// </summary>
        public void Show(KwRigCondition condition)
        {
            KwRigMark? fall = condition.FallOf(piece.Index);
            down = fall.HasValue;
            foreach ((Collider box, int part) in parts)
                box.enabled = !down || !condition.SailDown(piece.Index, part % KwRigSpar.Boom);
            foreach ((Collider box, float up) in tops)
                box.enabled = !down || up < fall!.Value.A;
            if (main is CapsuleCollider spar)
                KwRigDamageBoxes.Size(spar, down ? KwRigHitFind.Stump(piece.Main, fall!.Value.A) : piece.Main);
            else if (main is BoxCollider box && piece.Kind == KwRigPiece.CapKind)
                KwRigDamageBoxes.Place(box, down ? new Vector3(0f, 0f, 0.12f) : KwRigHitFind.CapMiddle(piece), down ? new Vector3(0.3f, 0.3f, 0.24f) : KwRigHitFind.CapSize(piece));
        }
    }
}
