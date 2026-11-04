using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Dropping a crate off a cart: the game's alternate use on it (Shift+E, or a gamepad's alternate keys with use) asks the
    /// crate's owner (<see cref="KwCargoCart.UnloadRpc"/>) to set it down beside the cart, at the spot the asking game found
    /// (<see cref="Spot"/>): to the right, the left or behind the cart (never in front, where it is pulled), the side nearest
    /// the player first, <see cref="KwCargoCart.Gap"/> clear of the walls, keeping its turn; on the ground, a floor or a dock at most
    /// <see cref="KwCargoCart.MostBelow"/> under the bed, or afloat where that is water (as crates float), never on another cart or a
    /// ship, nor where anything stands in its way there or between. The owner clears the link and writes the spot in one ZDO
    /// write; every screen slides it off the bed to there (<see cref="KwCrateLook"/>). An open or hooked crate stays.
    /// </summary>
    public static class KwCartUnload
    {
        private static readonly Collider[] near = new Collider[16];
        private static readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>The game's alternate use on a crate riding a cart: ask its owner to set it down beside the cart. Whether it was that.</summary>
        public static bool Asked(Container box, Humanoid who, bool hold, bool alt)
        {
            if (hold || !alt || !box.TryGetComponent(out KwCartRider rider) || rider.On == null || !box.TryGetComponent(out KwCrate crate))
                return false;
            if (box.IsInUse() || box.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1)
                who.Message(MessageHud.MessageType.Center, KwCraneText.InUse);
            else if (Spot(crate, rider.On, who.transform.position) is KwCrateTarget beside)
                crate.View!.InvokeRPC(KwCargoCart.UnloadRpc, beside.At, beside.Turn.eulerAngles.y);
            else
                who.Message(MessageHud.MessageType.Center, KwCartText.NoRoom);
            return true;
        }

        /// <summary>On the crate's owner: off its cart and down at that spot, unless it is open, hooked or no longer on a cart.</summary>
        public static void Apply(KwCrate crate, Vector3 at, float yaw)
        {
            ZNetView? view = crate.View;
            if (view == null || !view.IsOwner() || !KwCartRider.Carted(view.GetZDO()) || crate.Box != null && crate.Box.IsInUse() || !crate.Crane.IsNone())
                return;
            KwCartRider.Unload(crate);
            KwCrateRest.Put(crate, Settled(at, Quaternion.Euler(0f, yaw, 0f)), false);
        }

        /// <summary>On the owner: off its cart beside it (a crate whose place another took first), when there is room.</summary>
        public static void Off(KwCrate crate, KwCartBed bed)
        {
            if (!(Spot(crate, bed, crate.transform.position) is KwCrateTarget beside))
                return;
            KwCartRider.Unload(crate);
            KwCrateRest.Put(crate, beside, false);
        }

        /// <summary>Where the crate goes down beside the cart, the side nearest <paramref name="toward"/> first, or null when no side has room.</summary>
        public static KwCrateTarget? Spot(KwCrate crate, KwCartBed bed, Vector3 toward)
        {
            Quaternion turn = Quaternion.Euler(0f, crate.transform.eulerAngles.y, 0f);
            foreach (Vector3 side in Sides(crate, bed, turn, toward))
            {
                Vector3? at = Ground(crate, bed, side, turn);
                if (at.HasValue)
                    return Settled(at.Value, turn);
            }
            return null;
        }

        // Right, left and behind the cart at the floor's height, each far enough out for the crate to clear the wall by Gap, nearest first.
        private static Vector3[] Sides(KwCrate crate, KwCartBed bed, Quaternion turn, Vector3 toward)
        {
            KwCartShape shape = bed.Shape;
            Transform cart = bed.transform;
            Vector3 reach = Reach(Quaternion.Inverse(cart.rotation) * turn, crate.Half);
            float z = Mathf.Clamp(cart.InverseTransformPoint(crate.transform.position).z, shape.MinZ, shape.MaxZ);
            Vector3[] sides =
            {
                cart.TransformPoint(new Vector3(shape.MaxX + KwCargoCart.Gap + reach.x, shape.Floor, z)),
                cart.TransformPoint(new Vector3(shape.MinX - KwCargoCart.Gap - reach.x, shape.Floor, z)),
                cart.TransformPoint(new Vector3(0f, shape.Floor, shape.MinZ - KwCargoCart.Gap - reach.z)),
            };
            System.Array.Sort(sides, (a, b) => (a - toward).sqrMagnitude.CompareTo((b - toward).sqrMagnitude));
            return sides;
        }

        // How far the crate reaches along the cart's x and z from its middle, turned as it is against the cart.
        private static Vector3 Reach(Quaternion relative, Vector3 half)
        {
            Vector3 x = relative * Vector3.right * half.x, z = relative * Vector3.forward * half.z;
            return new Vector3(Mathf.Abs(x.x) + Mathf.Abs(z.x), 0f, Mathf.Abs(x.z) + Mathf.Abs(z.z));
        }

        // The crate's place where it comes down at that point (afloat on water), or null: too far down, onto something that
        // moves (a cart, a ship), or something in its way there or between.
        private static Vector3? Ground(KwCrate crate, KwCartBed bed, Vector3 point, Quaternion turn)
        {
            Vector3 root = point + Vector3.up;
            float free = KwCargoCast.FreeBelow(root + turn * crate.BoxCentre, crate.Half, turn, crate.transform, bed.transform, out Collider? under);
            Vector3 at = root + Vector3.down * Mathf.Max(0f, free);
            float? water = KwCargoCast.Water(at, 0f);
            bool afloat = water.HasValue && water.Value - KwCrateRest.Draft > at.y;
            if (afloat)
                at.y = water!.Value - KwCrateRest.Draft;
            if (root.y - at.y > 1f + KwCargoCart.MostBelow || !afloat && under != null && under.attachedRigidbody != null)
                return null;
            return Crowded(crate, bed, at, turn) || Blocked(crate, bed, at + turn * crate.BoxCentre) ? (Vector3?)null : at;
        }

        private static bool Crowded(KwCrate crate, KwCartBed bed, Vector3 at, Quaternion turn)
        {
            Vector3 centre = at + turn * crate.BoxCentre + Vector3.up * 0.05f;
            int count = Physics.OverlapBoxNonAlloc(centre, crate.Half * 0.95f, near, turn, KwCargo.Solid, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (near[i] != null && !Own(near[i].transform, crate, bed))
                    return true;
            }
            return false;
        }

        // Something solid between the crate on the bed and its spot (a wall the cart stands against).
        private static bool Blocked(KwCrate crate, KwCartBed bed, Vector3 to)
        {
            Vector3 from = crate.transform.TransformPoint(crate.BoxCentre), way = to - from;
            int count = Physics.RaycastNonAlloc(from, way.normalized, hits, way.magnitude, KwCargo.Solid, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != null && !Own(hits[i].collider.transform, crate, bed))
                    return true;
            }
            return false;
        }

        private static bool Own(Transform found, KwCrate crate, KwCartBed bed) => found.IsChildOf(crate.transform) || found.IsChildOf(bed.transform);

        // Its place there, afloat where that is water (its bottom Draft under the calm surface).
        private static KwCrateTarget Settled(Vector3 at, Quaternion turn)
        {
            float? water = KwCargoCast.Water(at, 0f);
            bool afloat = water.HasValue && water.Value - KwCrateRest.Draft > at.y - 0.01f;
            Vector3 put = afloat ? new Vector3(at.x, water!.Value - KwCrateRest.Draft, at.z) : at;
            return new KwCrateTarget(put, turn, afloat, null, null, default);
        }
    }
}
