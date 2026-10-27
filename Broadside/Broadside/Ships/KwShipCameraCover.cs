using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The spaces inside a launched ship a camera must not rest in, found by what is straight over a point: it is covered
    /// when the first thing over it is one of the ship's own colliders seen from below, facing down within 45° (a deck, the
    /// hold's floor, a shut hatch, a cabin's roof), and not a mast top (<see cref="KwMastTopColliders"/>' boxes, named
    /// "top", with open air under them). The hull's sides lean far less than that, so a camera by the hull's outside is never
    /// covered. A camera is raised out of such a space only while the ship stands between it and the player's eyes (it is
    /// shut in: a hold, a cabin, between decks), up through each cover over it to stand over it, as the game's camera stands
    /// over the sea; under a raised deck open toward the player, or under an open hatch, it sees him and stays. Physics rays
    /// against the ship's colliders where they are on this peer; for <see cref="KwShipCamera"/>. Local only.
    /// </summary>
    public static class KwShipCameraCover
    {
        private const float Reach = 30f, Slab = 0.1f, Facing = 0.7f, Drop = 1.5f, Above = 0.1f, Probe = 0.9f;
        private const int Most = 8, Round = 3;
        private const string MastTop = "top";
        private static int vehicle, mask;

        /// <summary>The vehicle layer, which the ship's boxes are on, as a mask.</summary>
        public static int Vehicle => vehicle != 0 ? vehicle : vehicle = 1 << LayerMask.NameToLayer("vehicle");

        /// <summary>The layers the ship's colliders are on: its boxes (vehicle) and its fittings (piece).</summary>
        public static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("vehicle", "piece");

        /// <summary>Whether one of the ship's covers is over a point (it is in a hold, between decks or in a cabin).</summary>
        public static bool Covered(KwShipBody ship, Vector3 point) => Top(ship, point, out _);

        /// <summary>
        /// Whether a player's eyes are inside the ship: under a cover, or under an opening in one (a hatch or a ladder's way
        /// down) with covers <see cref="Probe"/> m round them on <see cref="Round"/> sides of four.
        /// </summary>
        public static bool Within(KwShipBody ship, Vector3 eyes)
        {
            if (Covered(ship, eyes))
                return true;
            Vector3 across = ship.transform.right * Probe, along = ship.transform.forward * Probe;
            int round = Count(ship, eyes + across) + Count(ship, eyes - across) + Count(ship, eyes + along) + Count(ship, eyes - along);
            return round >= Round;
        }

        private static int Count(KwShipBody ship, Vector3 point) => Covered(ship, point) ? 1 : 0;

        /// <summary>
        /// A point raised straight up through each cover over it, to <paramref name="rise"/> over its top, while the ship
        /// hides the eyes from it; the height it was raised to, or minus infinity when it stays.
        /// </summary>
        public static float Lift(KwShipBody ship, ref Vector3 point, Vector3 eyes, float rise)
        {
            float lifted = float.NegativeInfinity;
            for (int i = 0; i < Most && Top(ship, point, out float top) && Hidden(ship, point, eyes); i++)
                point.y = lifted = top + rise;
            return lifted;
        }

        /// <summary>Whether one of the ship's colliders stands between a point and the eyes.</summary>
        public static bool Hidden(KwShipBody ship, Vector3 point, Vector3 eyes) =>
            Physics.Linecast(point, eyes, out RaycastHit hit, Mask, QueryTriggerInteraction.Ignore) && Ours(ship, hit);

        /// <summary>A little over the ship's floor close under a point (a deck, a roof, a mast top), or minus infinity.</summary>
        public static float Ground(KwShipBody ship, Vector3 point)
        {
            if (!Physics.Raycast(point, Vector3.down, out RaycastHit hit, Drop, Mask, QueryTriggerInteraction.Ignore) || !Ours(ship, hit))
                return float.NegativeInfinity;
            return Vector3.Dot(hit.normal, ship.transform.up) >= Facing ? hit.point.y + Above : float.NegativeInfinity;
        }

        // The top of the cover over a point: the first thing straight over it, one of the ship's colliders seen from below.
        private static bool Top(KwShipBody ship, Vector3 point, out float top)
        {
            top = 0f;
            if (!Physics.Raycast(point, Vector3.up, out RaycastHit hit, Reach, Mask, QueryTriggerInteraction.Ignore) || !Ours(ship, hit))
                return false;
            top = hit.point.y + Slab;
            return hit.collider.name != MastTop && Vector3.Dot(hit.normal, -ship.transform.up) >= Facing;
        }

        private static bool Ours(KwShipBody ship, RaycastHit hit) => hit.collider.transform.IsChildOf(ship.transform);
    }
}
