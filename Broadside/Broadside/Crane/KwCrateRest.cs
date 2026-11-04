using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a crate let go comes to rest, and its hand-off with the launched ships and carts it rides, on the crate's owner.
    /// It comes down straight, upright and keeping its turn, onto the first solid thing under its box (the ground, a floor, a
    /// dock, a deck, another piece; the ground's height when nothing is found), or afloat where that would be under water,
    /// its bottom <see cref="Draft"/> under the calm surface. Set down on a launched Broadside ship (its hull, deck or cabins,
    /// or a piece riding it) that has its own id, it rides that ship from there (<see cref="Board"/>), as a crate built on its
    /// deck does: the rider's own record in the crate's ZDO, read on every peer and by a player joining later. Let go over
    /// one of the game's own ships (or a Broadside ship before it has its id), which carry no crates, it goes down beside it,
    /// clear of its hull (<see cref="Beside"/>): set on its deck it stood fixed in the world inside the moving hull and held
    /// the ship dead (a player's report, 2026-11-04). Let go over the
    /// game's cart (its bed, its load space, a crate on it), it takes a place on the bed and rides the cart
    /// (<see cref="KwCartLoad.Onto"/>), or goes down beside it when the bed has no room. Let go from a crane from more than
    /// <see cref="KwCrateBreak.Height"/> over where it lands, it breaks there instead (<see cref="KwCrateBreak"/>). Hooked
    /// while it rides, it leaves the ship or cart first (<see cref="Unboard"/>): the record cleared, their child no more, an
    /// ordinary hanging crate.
    /// </summary>
    public static class KwCrateRest
    {
        /// <summary>How deep a floating crate sits in the water (m).</summary>
        public const float Draft = 0.35f;

        // How far a crate let go over a ship that carries no crates goes down clear of its hull's bounds (m).
        private const float Clear = 0.3f;

        /// <summary>
        /// Straight under the crate as it is drawn now (the crane it hangs from, or <paramref name="pass"/>, passed over): where
        /// it comes down, whether afloat, and the launched ship or cart that is under it.
        /// </summary>
        public static (Vector3 At, bool Afloat, KwShipRiders? Ship, KwCartBed? Cart) Below(KwCrate crate, Transform? pass = null)
        {
            (Vector3 at, bool afloat, Collider? under) = Down(crate, crate.transform.position, pass);
            Rigidbody? body = under != null ? under.attachedRigidbody : null;
            KwShipRiders? ship = ShipOf(body);
            if (ship == null && body != null && body.GetComponent<Ship>() != null)
            {
                (at, afloat, under) = Down(crate, Beside(crate, body), body.transform);
                body = under != null ? under.attachedRigidbody : null;
            }
            if (afloat)
                return (at, true, null, null);
            return (at, false, ship, body != null ? body.GetComponentInParent<KwCartBed>() : null);
        }

        // Straight down from a place under the crate's box (the crane it hangs from, or pass, passed over): where it comes
        // down, whether afloat, and what it would come down on.
        private static (Vector3 At, bool Afloat, Collider? Under) Down(KwCrate crate, Vector3 from, Transform? pass)
        {
            Quaternion upright = Upright(crate);
            KwCrane? crane = pass == null ? crate.HangingFrom() : null;
            float free = KwCargoCast.FreeBelow(from + upright * crate.BoxCentre, crate.Half, upright, crate.transform, pass != null ? pass : crane != null ? crane.transform : null, out Collider? under);
            Vector3 at = from + Vector3.down * Mathf.Max(0f, free);
            if (free >= KwCargoCast.Far - 1f && ZoneSystem.instance != null)
                at.y = ZoneSystem.instance.GetGroundHeight(from);
            float? water = KwCargoCast.Water(at, 0f);
            if (water.HasValue && water.Value - Draft > at.y)
                return (new Vector3(at.x, water.Value - Draft, at.z), true, null);
            return (at, false, under);
        }

        // Over a ship that carries no crates: out from its hull's bounds on the side the crate hangs, by the box's reach
        // and Clear more, at the crate's height.
        private static Vector3 Beside(KwCrate crate, Rigidbody body)
        {
            Bounds hull = new Bounds(body.worldCenterOfMass, Vector3.zero);
            foreach (Collider part in body.GetComponentsInChildren<Collider>())
            {
                if (!part.isTrigger)
                    hull.Encapsulate(part.bounds);
            }
            Vector3 from = crate.transform.position, way = from - hull.center;
            way.y = 0f;
            way = way.sqrMagnitude > 1e-4f ? way.normalized : body.transform.right;
            float reach = Mathf.Abs(way.x) * hull.extents.x + Mathf.Abs(way.z) * hull.extents.z + new Vector2(crate.Half.x, crate.Half.z).magnitude + Clear;
            Vector3 clear = hull.center + way * reach;
            return new Vector3(clear.x, from.y, clear.z);
        }

        /// <summary>Where the crate comes to rest from where it is drawn now: on what is under it, a launched ship, a cart's bed or beside it.</summary>
        public static KwCrateTarget Find(KwCrate crate)
        {
            (Vector3 at, bool afloat, KwShipRiders? ship, KwCartBed? cart) = Below(crate);
            if (cart != null && cart.Zdo != null)
                return KwCartLoad.Onto(crate, cart);
            return new KwCrateTarget(at, Upright(crate), afloat, ship, null, default);
        }

        /// <summary>Straight down past <paramref name="pass"/> (a cart with no room and none beside it): on what is under it, or afloat.</summary>
        public static KwCrateTarget Plain(KwCrate crate, Transform pass)
        {
            (Vector3 at, bool afloat, KwShipRiders? ship, _) = Below(crate, pass);
            return new KwCrateTarget(at, Upright(crate), afloat, ship, null, default);
        }

        /// <summary>
        /// On the owner: the crate put where it comes to rest, its place and what it rides written in one ZDO write; let go from a
        /// crane (<paramref name="fromCrane"/>) from too high, it falls there and breaks as it lands instead.
        /// </summary>
        public static void Put(KwCrate crate, KwCrateTarget target, bool fromCrane)
        {
            ZDO zdo = crate.View!.GetZDO();
            zdo.Set(KwCargo.AfloatKey, target.Afloat);
            if (fromCrane && !target.Afloat && KwCrateBreak.Begin(crate, target.At, target.Turn))
                return;
            if (target.Cart != null)
                KwCartLoad.Board(crate, target.Cart, target.Place);
            else if (target.Ship != null)
                Board(crate, target.Ship, target.At, target.Turn);
            else
            {
                zdo.SetPosition(target.At);
                zdo.SetRotation(target.Turn);
            }
        }

        private static Quaternion Upright(KwCrate crate) => Quaternion.Euler(0f, crate.transform.eulerAngles.y, 0f);

        // The launched Broadside ship a body is (its own colliders or a rider's, all in its body), once it has its id.
        private static KwShipRiders? ShipOf(Rigidbody? body)
        {
            KwShipRiders? ship = body != null ? body.GetComponent<KwShipRiders>() : null;
            return ship != null && ship.Zdo != null && ship.Id != 0L ? ship : null;
        }

        /// <summary>
        /// On the owner: a crate set down on a launched ship rides it, at that place on the deck as the ship is drawn (the hit
        /// carried from the ship's physics pose, <see cref="KwShipRiders.Shown"/>), turned with the deck but keeping its
        /// heading: its place in the world and the rider's record written together (<see cref="KwShipRider.Place"/>).
        /// </summary>
        public static void Board(KwCrate crate, KwShipRiders ship, Vector3 at, Quaternion upright)
        {
            Transform drawn = ship.transform;
            Vector3 shown = ship.Shown(at, Vector3.up).At;
            Quaternion onDeck = drawn.rotation * Quaternion.Euler(0f, upright.eulerAngles.y - drawn.eulerAngles.y, 0f);
            crate.transform.SetPositionAndRotation(shown, onDeck);
            ZDO zdo = crate.View!.GetZDO();
            zdo.SetPosition(shown);
            zdo.SetRotation(onDeck);
            KwShipRider.Place(crate.gameObject, ship);
        }

        /// <summary>On the owner, as it is hooked: off the ship or cart it rides (or waits for, parked), an ordinary piece again.</summary>
        public static void Unboard(KwCrate crate)
        {
            if (crate.TryGetComponent(out KwShipRider rider) && rider.Ship != 0L)
                rider.Release();
            KwCartRider.Unload(crate);
        }
    }
}
