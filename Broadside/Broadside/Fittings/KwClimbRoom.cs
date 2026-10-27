using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Whether the player fits standing at a place a climb puts them (a ladder's top, a boarding ladder's deck): their own
    /// capsule there touches nothing solid. A climb that lands the player overlapping a railing, a wall or the hull's boxes
    /// has the physics push them out again, off a roof or over the side (the user, 2026-10-26: "it's still doing that
    /// thing where it's like kicking me back down"). Pure physics queries; local to the climbing player.
    /// </summary>
    public static class KwClimbRoom
    {
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
