using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cargo crate in the world: the game's chest (its items in its own ZDO, opened as any chest) that only a cargo crane
    /// lifts. It hangs from a crane while both name each other, the crane in the crate's ZDO
    /// (<see cref="KwCargo.CraneKey"/>, written by the crate's owner) and the crate in the crane's (written by the crane's);
    /// every screen then draws it under the crane's hook (<see cref="KwCrateLook"/>) and the crate's owner keeps its ZDO's
    /// place up with it. Its owner agrees to a hook (<see cref="KwCargo.HookRpc"/>) unless the crate is open or on another
    /// crane, and sets it down under where it hangs (<see cref="Rest"/>: on what is under it, or afloat) when told to let go,
    /// when the crane no longer names it, or when the crane is gone; and a free crate with nothing left under it settles
    /// again. Nothing is ever taken out of or put into it here, so nothing can be lost or doubled.
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

        /// <summary>A loaded crate by its ZDO, or null.</summary>
        public static KwCrate? Find(ZDOID id)
        {
            if (id.IsNone() || ZNetScene.instance == null)
                return null;
            GameObject? found = ZNetScene.instance.FindInstance(id);
            return found != null ? found.GetComponent<KwCrate>() : null;
        }

        /// <summary>The free crate whose top the hook rests on: within <paramref name="reach"/> across, its top from <paramref name="below"/> under the hook to <paramref name="above"/> over it.</summary>
        public static KwCrate? Under(Vector3 hook, float reach, float below, float above)
        {
            KwCrate? best = null;
            float bestSq = reach * reach;
            foreach (KwCrate crate in all)
            {
                Vector3 top = crate.transform.TransformPoint(crate.boxCentre + Vector3.up * crate.half.y);
                float rise = hook.y - top.y, sq = new Vector2(hook.x - top.x, hook.z - top.z).sqrMagnitude;
                if (crate.Crane.IsNone() && rise >= -below && rise <= above && sq < bestSq)
                    (best, bestSq) = (crate, sq);
            }
            return best;
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
            all.Add(this);
        }

        // The lift point and the body's box from the model (its col_box_body), in the crate's own frame.
        private void Measure()
        {
            Transform? lift = BundlePrefabs.GameMaterials.Find(transform, LiftName);
            if (lift != null)
                liftLocal = transform.InverseTransformPoint(lift.position);
            Transform? body = BundlePrefabs.GameMaterials.Find(transform, BodyBox);
            BoxCollider? box = body != null ? body.GetComponent<BoxCollider>() : null;
            if (box == null)
                return;
            boxCentre = transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
            half = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
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

        // On the owner: hang from the crane unless open or on another crane still holding it; answer the crane's owner.
        private void Hook(long sender, ZDOID crane)
        {
            if (!view!.IsOwner())
                return;
            ZDOID now = Crane;
            bool ok = (container == null || !container.IsInUse()) && (now.IsNone() || now == crane || !Linked(now));
            if (ok)
            {
                ZDO zdo = view.GetZDO();
                zdo.Set(KwCargo.CraneKey, crane);
                zdo.Set(KwCargo.HangYawKey, transform.eulerAngles.y - TurretYaw(crane));
                zdo.Set(KwCargo.AfloatKey, false);
                misses = 0;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, crane, KwCargo.HookedRpc, Id, ok);
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
                Rest();
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
            if (now >= nextWrite && !Crane.IsNone())
            {
                nextWrite = now + WriteEvery;
                Follow();
            }
        }

        // On the owner: let go when the crane has not named this crate for a few checks running (it let go, it is gone,
        // or it was never told); a free crate with nothing under it settles again.
        private void Check()
        {
            ZDOID crane = Crane;
            if (crane.IsNone())
            {
                Settle();
                return;
            }
            misses = Linked(crane) ? 0 : misses + 1;
            if (misses >= Patience)
                Rest();
        }

        private void Settle()
        {
            if (Time.time < nextSettle || Afloat)
                return;
            nextSettle = Time.time + SettleEvery;
            ZDO zdo = view!.GetZDO();
            Vector3 centre = zdo.GetPosition() + zdo.GetRotation() * boxCentre;
            if (KwCargoCast.FreeBelow(centre, half, zdo.GetRotation(), transform, null) > Loose)
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

        /// <summary>On the owner: set down straight under where it is now (or afloat), upright, and free of any crane.</summary>
        public void Rest()
        {
            (Vector3 at, bool afloat) = KwCrateRest.Below(this);
            ZDO zdo = view!.GetZDO();
            zdo.SetPosition(at);
            zdo.SetRotation(Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            zdo.Set(KwCargo.AfloatKey, afloat);
            zdo.Set(KwCargo.CraneKey, ZDOID.None);
            misses = 0;
        }
    }
}
