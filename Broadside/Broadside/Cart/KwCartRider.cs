using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A crate riding a cart, on every peer (added as soon as its ZDO names a cart, <see cref="Agree"/>, from
    /// <see cref="KwCrateLook"/>, so a player joining later or a world loaded again has it too). Its ZDO keeps the link: the
    /// cart's own id, its cells and its turn on the bed (<see cref="KwCart"/>). Wherever the cart is in this peer's scene, the
    /// crate is its child at that place (<see cref="KwCartBed.Take"/>), eased there from where it was drawn (a crate set down
    /// from the hook drops onto the bed, falling), so it follows the cart exactly as the cart's owner pulls it and every other
    /// screen draws it. With its cart not loaded here it stays where it is (parked) and boards when the cart comes. Its owner
    /// keeps its ZDO's place with the cart, so it loads with the cart's area and where it is: at once when the cart stands
    /// still, every <see cref="Moving"/> metres while it is pulled (each write sends the crate's whole ZDO, items and all). Its
    /// owner lets it go when the cart is gone for good (destroyed, or nowhere near for <see cref="KwCargoCart.Patience"/> seconds
    /// with the area loaded): the link cleared, the crate settles where it is. Two crates set down on one place at the same
    /// moment by two games: the later one (by ZDO id) goes down beside the cart.
    /// </summary>
    public sealed class KwCartRider : MonoBehaviour
    {
        private const float Look = 0.5f, Keep = 1f, Moving = 8f, Hair = 0.02f, Degrees = 1f, Still = 0.05f, Snap = 3f;
        private ZNetView? view;
        private KwCrate? crate;
        private KwCartBed? on;
        private Vector3 local, from, last;
        private Quaternion turn = Quaternion.identity, fromTurn = Quaternion.identity;
        private float next, missing, began = -1f, span;
        private bool gone;

        /// <summary>The cart's own id it rides, its cells and its turn on the bed, as last read from its ZDO.</summary>
        public long Cart { get; private set; }
        public int Mask { get; private set; }
        public float Yaw { get; private set; }

        /// <summary>The cart it is a child of now, or null while parked.</summary>
        public KwCartBed? On => on;
        public Container? Box => crate != null ? crate.Box : null;

        /// <summary>Whether a crate's ZDO says it rides a cart.</summary>
        public static bool Carted(ZDO? zdo) => zdo != null && zdo.GetLong(KwCargoCart.CartHash, 0L) != 0L;

        /// <summary>
        /// The link and the rider agree on this peer: a crate whose ZDO names a cart rides it here; one whose ZDO no longer does
        /// is let go here now. Whether it rides.
        /// </summary>
        public static bool Agree(GameObject crate, ZDO zdo)
        {
            bool record = Carted(zdo);
            KwCartRider? rider = crate.TryGetComponent(out KwCartRider found) ? found : null;
            if (record && rider == null)
                crate.AddComponent<KwCartRider>();
            else if (!record && rider != null)
                rider.Release(false);
            return record;
        }

        /// <summary>On the crate's owner, as it is hooked or unloaded: off its cart, a free crate where it is.</summary>
        public static void Unload(KwCrate crate)
        {
            if (crate.TryGetComponent(out KwCartRider rider))
                rider.Release(false);
            else if (crate.View != null && crate.View.GetZDO() is ZDO zdo && zdo.IsOwner() && Carted(zdo))
                zdo.Set(KwCargoCart.CartHash, 0L);
        }

        private void Awake() => Guard.Run("Broadside cart rider", static r => r.Begin(), this);

        private void Begin()
        {
            (view, crate) = (GetComponent<ZNetView>(), GetComponent<KwCrate>());
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || crate == null || !Read(zdo))
            {
                Destroy(this);
                return;
            }
            missing = Time.time;
            Board();
        }

        private bool Read(ZDO zdo)
        {
            Cart = zdo.GetLong(KwCargoCart.CartHash, 0L);
            Mask = zdo.GetInt(KwCargoCart.SlotHash);
            Yaw = zdo.GetFloat(KwCargoCart.YawHash);
            return Cart != 0L;
        }

        private bool Changed(ZDO zdo) =>
            zdo.GetLong(KwCargoCart.CartHash, 0L) != Cart || zdo.GetInt(KwCargoCart.SlotHash) != Mask || !Mathf.Approximately(zdo.GetFloat(KwCargoCart.YawHash), Yaw);

        private void OnDestroy()
        {
            if (on != null)
                on.Forget(this);
        }

        private void LateUpdate()
        {
            if (began >= 0f)
                Ease();
            if (Time.time < next)
                return;
            next = Time.time + (on != null ? Keep : Look);
            Guard.Run("Broadside cart rider", static r => r.Tick(), this);
        }

        private void Tick()
        {
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null)
                return;
            if (Changed(zdo))
            {
                Leave(false);
                if (!Read(zdo))
                {
                    Release(false);
                    return;
                }
            }
            if (on == null || on.Zdo == null)
                Board();
            if (zdo.IsOwner())
                Upkeep(zdo);
        }

        // Onto its cart when the cart is in this peer's scene, eased from where it is drawn to its place.
        private void Board()
        {
            KwCartBed? bed = KwCartBed.Find(Cart);
            if (bed == null || crate == null)
                return;
            (local, turn) = KwCartLoad.Pose(bed.Shape, new KwCartPlace(Mask, Yaw), crate.BoxCentre, crate.Half);
            (on, missing, gone) = (bed, Time.time, false);
            bed.Take(this);
            Settle();
        }

        // A short ease onto the bed (a fall when it comes from over it), or a jump when it was drawn far off (a parked crate).
        private void Settle()
        {
            Vector3 now = transform.localPosition;
            if ((now - local).sqrMagnitude > Snap * Snap)
            {
                transform.SetLocalPositionAndRotation(local, turn);
                began = -1f;
                return;
            }
            float drop = Mathf.Max(0f, now.y - local.y);
            (from, fromTurn, began, span) = (now, transform.localRotation, Time.time, Mathf.Clamp(Mathf.Sqrt(2f * drop / 9.81f), 0.12f, 0.6f));
        }

        private void Ease()
        {
            if (on == null || transform.parent != on.transform)
            {
                began = -1f;
                return;
            }
            float t = Mathf.Clamp01((Time.time - began) / span), side = Mathf.SmoothStep(0f, 1f, t);
            if (t >= 1f)
                began = -1f;
            Vector3 at = new Vector3(Mathf.Lerp(from.x, local.x, side), Mathf.Lerp(from.y, local.y, t * t), Mathf.Lerp(from.z, local.z, side));
            transform.SetLocalPositionAndRotation(at, Quaternion.Slerp(fromTurn, turn, side));
        }

        // On the owner: the ZDO's place kept with the cart; a cart gone for good lets it go; a place taken twice is settled.
        private void Upkeep(ZDO zdo)
        {
            if (on == null)
            {
                Parked();
                return;
            }
            missing = Time.time;
            Vector3 now = transform.position;
            bool still = (now - last).sqrMagnitude < Still * Still;
            last = now;
            float off = (zdo.GetPosition() - now).magnitude;
            if (still ? off > Hair || Quaternion.Angle(zdo.GetRotation(), transform.rotation) > Degrees : off > Moving)
            {
                zdo.SetPosition(now);
                zdo.SetRotation(transform.rotation);
            }
            if (began < 0f)
                Clash(zdo);
        }

        // Another crate on this cart holds one of its cells and was there first (the lower ZDO id): this one goes beside the cart.
        private void Clash(ZDO zdo)
        {
            foreach (KwCartRider other in on!.Riders)
            {
                ZDO? theirs = other != null && other != this && (other.Mask & Mask) != 0 && other.view != null ? other.view.GetZDO() : null;
                if (theirs != null && First(theirs.m_uid, zdo.m_uid))
                {
                    KwCartUnload.Off(crate!, on);
                    return;
                }
            }
        }

        private static bool First(ZDOID a, ZDOID b) => a.UserID != b.UserID ? a.UserID < b.UserID : a.ID < b.ID;

        // On the owner, its cart not in this game's scene: let go when the cart was destroyed, or when no ZDO of it has been
        // near for Patience seconds with the area round the crate loaded.
        private void Parked()
        {
            if (gone || KwCartBed.Gone(Cart))
                Release(true);
            else if (KwCartFind.Near(Cart, transform.position) != null)
                missing = Time.time;
            else if (Time.time - missing > KwCargoCart.Patience && AreaReady())
                Release(true);
        }

        private bool AreaReady() => ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(transform.position);

        /// <summary>Let go where it is (parked) as its cart's object goes; when the cart was destroyed, its owner makes it a free crate.</summary>
        public void Leave(bool destroyed)
        {
            KwCartBed? was = on;
            (on, missing, began) = (null, Time.time, -1f);
            if (was != null)
                was.Drop(this);
            gone |= destroyed;
            if (destroyed && view != null && view.GetZDO() is ZDO zdo && zdo.IsOwner())
                Release(true);
        }

        /// <summary>A free crate again: the link cleared (on its owner), off the cart where it is; settling at once when asked.</summary>
        public void Release(bool settle)
        {
            if (view != null && view.GetZDO() is ZDO zdo && zdo.IsOwner() && Carted(zdo))
                zdo.Set(KwCargoCart.CartHash, 0L);
            if (on != null)
                Leave(false);
            Cart = 0L;
            if (settle && crate != null)
                crate.SettleSoon();
            Destroy(this);
        }
    }
}
