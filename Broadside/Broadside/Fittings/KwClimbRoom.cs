using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Whether the player fits standing at a place a climb puts them (a ladder's top, a boarding ladder's deck): their own
    /// capsule there touches nothing solid. A climb that lands the player overlapping a railing, a wall or the hull's boxes
    /// has the physics push them out again, off a roof or over the side (the user, 2026-10-26: "it's still doing that
    /// thing where it's like kicking me back down"). Pure physics queries; local to the climbing player. Also where a
    /// climb puts the player down (<see cref="Place"/>).
    /// </summary>
    public static class KwClimbRoom
    {
        /// <summary>
        /// The player put standing there by a climb. No fall is counted from where they were, and the game's hold on a ship
        /// is let go: a player standing still on a ship is kept at the spot they stood on it (<c>Character.ApplyGroundForce</c>,
        /// pulled back to it from up to 4 m, or set straight onto it when another peer owns the ship), and that spot is taken
        /// again only on another ship's body. A climb from a deck to a roof of the same ship kept the spot at the ladder's
        /// foot, and the game dragged the player back over the edge to it. Let go, the spot is taken where they land.
        /// </summary>
        public static void Place(Player player, Vector3 feet)
        {
            player.transform.position = feet;
            player.m_maxAirAltitude = feet.y;
            player.m_lastAttachBody = null;
            Physics.SyncTransforms();
        }

        /// <summary>Whether the player's capsule standing with its feet there touches nothing solid (their place-ray layers).</summary>
        public static bool Fits(Player player, Vector3 feet)
        {
            CapsuleCollider body = player.m_collider;
            float r = body.radius - 0.02f, h = Mathf.Max(body.height, 2f * body.radius);
            Vector3 low = feet + Vector3.up * (r + 0.05f), high = feet + Vector3.up * (h - r);
            return !Physics.CheckCapsule(low, high, r, player.m_placeRayMask, QueryTriggerInteraction.Ignore);
        }
    }
}
