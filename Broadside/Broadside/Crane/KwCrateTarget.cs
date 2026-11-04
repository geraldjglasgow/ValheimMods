using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a crate let go comes to rest (<see cref="KwCrateRest.Find"/>): its place and turn in the world, whether it floats
    /// there, and what it then rides: a launched ship (<see cref="Ship"/>, the rider's record) or a cart (<see cref="Cart"/>
    /// at <see cref="Place"/>, the cart's link); neither for an ordinary resting crate.
    /// </summary>
    public readonly struct KwCrateTarget
    {
        public readonly Vector3 At;
        public readonly Quaternion Turn;
        public readonly bool Afloat;
        public readonly KwShipRiders? Ship;
        public readonly KwCartBed? Cart;
        public readonly KwCartPlace Place;

        public KwCrateTarget(Vector3 at, Quaternion turn, bool afloat, KwShipRiders? ship, KwCartBed? cart, KwCartPlace place) =>
            (At, Turn, Afloat, Ship, Cart, Place) = (at, turn, afloat, ship, cart, place);
    }
}
