using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A crate a cargo crane lifts: the cargo crate (<see cref="KwCargo.CratePrefab"/>) and, since 2026-11-02, every storage
    /// crate (<see cref="KwCrates"/>; the user's call: the crane lifts every Broadside storage crate), each the game's chest
    /// (its items in its own ZDO, opened as any chest); no other chest. It hangs from a crane while both name each other,
    /// the crane in the crate's ZDO (<see cref="KwCargo.CraneKey"/>, written by the crate's owner) and the crate in the
    /// crane's (written by the crane's); every screen then draws it under the crane's hook in a rope sling
    /// (<see cref="KwCrateLook"/>) and the crate's owner keeps its ZDO's place up with it. Its owner agrees to a hook
    /// (<see cref="KwCargo.HookRpc"/>) unless the crate is open or on another crane, taking it off the launched ship it
    /// rides first (<see cref="KwCrateRest.Unboard"/>), and sets it down under where it hangs (<see cref="Rest"/>: on what is
    /// under it, aboard the launched ship whose deck that is, or afloat) when told to let go, when the crane no longer names
    /// it, or when the crane is gone; and a free crate with nothing left under it settles again (never one riding a ship or
    /// a cart: its rider keeps it). Let go over the game's cart it rides the cart (<see cref="KwCartLoad"/>); let go from
    /// more than <see cref="KwCrateBreak.Height"/> up it breaks where it lands (<see cref="KwCrateBreak"/>), its items
    /// dropped whole before it goes. Its box and lift point come from its model (<see cref="Measure"/>). Nothing is ever
    /// taken out of or put into it here but by the break, so nothing can be lost or doubled.
    /// </summary>
    public sealed class KwCrate : MonoBehaviour
    {
        public const string Label = "Cargo crate";
        public const string LiftName = "lift", BodyBox = "col_box_body";
        private const float CheckEvery = 0.5f, WriteEvery = 0.2f, SettleEvery = 2f, Loose = 0.05f;
        private const int Patience = 6;
        private static readonly List<KwCrate> all = new List<KwCrate>();

        private ZNetView? view;
        private Container? container;
        private Vector3 liftLocal = new Vector3(0f, 1.6f, 0f), boxCentre = new Vector3(0f, 0.45f, 0f), half = new Vector3(0.7f, 0.45f, 0.45f);
        private float nextCheck, nextWrite, nextSettle;
        private int misses;
        private bool hanging;

        public ZDOID Id => view != null && view.IsValid() ? view.GetZDO().m_uid : ZDOID.None;
        public ZDOID Crane => view != null && view.IsValid() ? view.GetZDO().GetZDOID(KwCargo.CraneKey) : ZDOID.None;
        public bool Afloat => view != null && view.IsValid() && view.GetZDO().GetBool(KwCargo.AfloatKey);
        public float HangYaw => view != null && view.IsValid() ? view.GetZDO().GetFloat(KwCargo.HangYawKey) : 0f;
        public ZNetView? View => view;
        public Container? Box => container;

        /// <summary>Where a crane's hook holds it (the sling's meeting point), in the crate's own frame and in the world.</summary>
        public Vector3 LiftLocal => liftLocal;
        public Vector3 LiftPoint => transform.TransformPoint(liftLocal);

        /// <summary>The crate's own box: centre (own frame) and half sizes.</summary>
        public Vector3 BoxCentre => boxCentre;
        public Vector3 Half => half;

        /// <summary>Whether it rides a launched ship or a cart (or waits for it, parked): its rider places it, it neither settles nor is drawn free.</summary>
        public bool Riding => TryGetComponent(out KwShipRider rider) && rider.Ship != 0L || KwCartRider.Carted(view != null ? view.GetZDO() : null);

        /// <summary>A loaded crate by its ZDO, or null.</summary>
        public static KwCrate? Find(ZDOID id)
        {
            if (id.IsNone() || ZNetScene.instance == null)
                return null;
            GameObject? found = ZNetScene.instance.FindInstance(id);
            return found != null ? found.GetComponent<KwCrate>() : null;
        }

        /// <summary>
        /// The free crate whose top the hook rests on: the hook anywhere over its top (its footprint and <paramref name="margin"/>
        /// round it), the top from <paramref name="below"/> under the hook to <paramref name="above"/> over it; of several, the
        /// one whose top is nearest the hook's height, then nearest its middle.
        /// </summary>
        public static KwCrate? Under(Vector3 hook, float margin, float below, float above)
        {
            KwCrate? best = null;
            float bestScore = float.MaxValue;
            foreach (KwCrate crate in all)
            {
                float score = crate.Over(hook, margin, below, above);
                if (score < bestScore)
                    (best, bestScore) = (crate, score);
            }
            return best;
        }

        // How far the hook is from resting on this free crate's top (its height off the top, and a quarter of its way off
        // the middle), in the crate's own frame; float.MaxValue when it is not over the top or the crate hangs already.
        private float Over(Vector3 hook, float margin, float below, float above)
        {
            if (view == null || !view.IsValid() || !Crane.IsNone())
                return float.MaxValue;
            Vector3 at = transform.InverseTransformPoint(hook) - boxCentre;
            float rise = at.y - half.y;
            if (Mathf.Abs(at.x) > half.x + margin || Mathf.Abs(at.z) > half.z + margin || rise < -below || rise > above)
                return float.MaxValue;
            return Mathf.Abs(rise) + 0.25f * new Vector2(at.x, at.z).magnitude;
        }

        private void Start() => Guard.Run("Broadside cargo crate", static c => c.Init(), this);

        private void Init()
        {
            view = GetComponent<ZNetView>();
            container = GetComponent<Container>();
            if (view == null || view.GetZDO() == null)
            {
                enabled = false;
                return;
            }
            Measure();
            view.Register<ZDOID>(KwCargo.HookRpc, (sender, crane) => Guard.Run("Broadside crate hook", () => Hook(sender, crane)));
            view.Register<ZDOID>(KwCargo.ReleaseRpc, (sender, crane) => Guard.Run("Broadside crate release", () => Release(crane)));
            view.Register<Vector3, float>(KwCargoCart.UnloadRpc, (sender, at, yaw) => Guard.Run("Broadside crate unload", () => KwCartUnload.Apply(this, at, yaw)));
            all.Add(this);
        }

        // The body's box from the model (its col_box_body) and the lift point (the model's own, the cargo crate's; else over
        // the middle of the box's top, a storage crate's, KwCrateSling.LiftOver), in the crate's own frame.
        private void Measure()
        {
            Transform? body = BundlePrefabs.GameMaterials.Find(transform, BodyBox);
            BoxCollider? box = body != null ? body.GetComponent<BoxCollider>() : null;
            if (box != null)
            {
                boxCentre = transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
                half = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
            }
            Transform? lift = BundlePrefabs.GameMaterials.Find(transform, LiftName);
            liftLocal = lift != null ? transform.InverseTransformPoint(lift.position) : KwCrateSling.LiftOver(boxCentre, half);
        }

        private void OnDestroy() => all.Remove(this);

        /// <summary>The crane it hangs from on this screen (both name each other, the crane loaded here), or null.</summary>
        public KwCrane? HangingFrom()
        {
            GameObject? found = Crane.IsNone() || ZNetScene.instance == null ? null : ZNetScene.instance.FindInstance(Crane);
            KwCrane? crane = found != null ? found.GetComponent<KwCrane>() : null;
            return crane != null && crane.Crate == Id && crane.Rig != null && crane.Rig.Ready ? crane : null;
        }

        public bool HangsFrom(KwCrane crane) => Crane == crane.Id && crane.Crate == Id;

        /// <summary>Asks the crate's owner to hang it from this crane.</summary>
        public void AskHook(ZDOID crane) => view!.InvokeRPC(KwCargo.HookRpc, crane);

        /// <summary>The free drop under the crate as it is drawn now.</summary>
        public float FreeBelow()
        {
            KwCrane? crane = HangingFrom();
            return KwCargoCast.FreeBelow(transform.TransformPoint(boxCentre), half, transform.rotation, transform, crane != null ? crane.transform : null);
        }

        // On the owner: hang from the crane unless open, falling to break, or on another crane still holding it; answer the
        // crane's owner.
        private void Hook(long sender, ZDOID crane)
        {
            if (!view!.IsOwner())
                return;
            ZDOID now = Crane;
            bool ok = (container == null || !container.IsInUse()) && !KwCrateBreak.Pending(this) && (now.IsNone() || now == crane || !Linked(now));
            if (ok)
                Take(crane);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, crane, KwCargo.HookedRpc, Id, ok);
        }

        // On the owner, agreed: off the ship it rides first (the rider's record cleared in this same ZDO write, so every
        // peer and a player joining later sees it hang, never ride and hang at once), then this crane's, turned against
        // its turret as it stands.
        private void Take(ZDOID crane)
        {
            KwCrateRest.Unboard(this);
            ZDO zdo = view!.GetZDO();
            zdo.Set(KwCargo.CraneKey, crane);
            zdo.Set(KwCargo.HangYawKey, transform.eulerAngles.y - TurretYaw(crane));
            zdo.Set(KwCargo.AfloatKey, false);
            (hanging, misses) = (true, 0);
        }

        // The crane's turret turn in the world: from its model where it is loaded, else from its ZDO.
        private static float TurretYaw(ZDOID crane)
        {
            GameObject? found = ZNetScene.instance.FindInstance(crane);
            KwCraneRig? rig = found != null ? found.GetComponent<KwCraneRig>() : null;
            if (rig != null && rig.Ready)
                return rig.Turret.eulerAngles.y;
            ZDO? zdo = ZDOMan.instance.GetZDO(crane);
            return zdo != null ? zdo.GetRotation().eulerAngles.y + zdo.GetFloat(KwCargo.TurnKey) : 0f;
        }

        private void Release(ZDOID crane)
        {
            if (view!.IsOwner() && Crane == crane)
                Rest(true);
        }

        // Whether that crane's ZDO still names this crate.
        private bool Linked(ZDOID crane)
        {
            ZDO? zdo = ZDOMan.instance.GetZDO(crane);
            return zdo != null && zdo.GetZDOID(KwCargo.CrateKey) == Id;
        }

        private void Update()
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            float now = Time.time;
            if (now >= nextCheck)
            {
                nextCheck = now + CheckEvery;
                Guard.Run("Broadside crate check", Check);
            }
            if (hanging && now >= nextWrite)
            {
                nextWrite = now + WriteEvery;
                Follow();
            }
        }

        // On the owner: let go when the crane has not named this crate for a few checks running (it let go, it is gone,
        // or it was never told); a free crate with nothing under it settles again; one falling to break whose break this
        // game has not armed (its owner changed mid-fall) breaks here.
        private void Check()
        {
            ZDOID crane = Crane;
            hanging = !crane.IsNone();
            KwCrateBreak.Keep(this);
            if (!hanging)
            {
                Settle();
                return;
            }
            misses = Linked(crane) ? 0 : misses + 1;
            if (misses >= Patience)
                Rest(true);
        }

        /// <summary>Settled at its next check (a crate just let go by a cart that went).</summary>
        public void SettleSoon() => nextSettle = 0f;

        // Not while it rides a ship or a cart (the rider keeps its place, aboard or parked), floats, falls to break, or
        // stands where the land and pieces round it are not all loaded yet (it would fall through them).
        private void Settle()
        {
            if (Time.time < nextSettle || Afloat || Riding || KwCrateBreak.Pending(this))
                return;
            nextSettle = Time.time + SettleEvery;
            ZDO zdo = view!.GetZDO();
            Vector3 centre = zdo.GetPosition() + zdo.GetRotation() * boxCentre;
            if (ZNetScene.instance.IsAreaReady(centre) && KwCargoCast.FreeBelow(centre, half, zdo.GetRotation(), transform, null) > Loose)
                Rest();
        }

        // On the owner while it hangs: its ZDO's place follows it, so it loads where it is and comes down from there.
        private void Follow()
        {
            ZDO zdo = view!.GetZDO();
            if ((zdo.GetPosition() - transform.position).sqrMagnitude < 0.0004f && Quaternion.Angle(zdo.GetRotation(), transform.rotation) < 1f)
                return;
            zdo.SetPosition(transform.position);
            zdo.SetRotation(transform.rotation);
        }

        /// <summary>
        /// On the owner: set down straight under where it is now, upright, and free of any crane: on what is under it, aboard
        /// the launched ship whose deck that is (it rides it, as a crate built there does, <see cref="KwCrateRest.Board"/>), on
        /// a cart's bed or beside the cart, or afloat (<see cref="KwCrateRest.Find"/>, worked out while it still hangs, so the
        /// crane's own parts are passed over). Let go from a crane (<paramref name="fromCrane"/>) from too high, it breaks
        /// where it lands (<see cref="KwCrateBreak"/>).
        /// </summary>
        public void Rest(bool fromCrane = false)
        {
            KwCrateTarget target = KwCrateRest.Find(this);
            view!.GetZDO().Set(KwCargo.CraneKey, ZDOID.None);
            (hanging, misses) = (false, 0);
            KwCrateRest.Put(this, target, fromCrane);
        }
    }
}
