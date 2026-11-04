using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock ladder's solid part, made on every peer (a dedicated server too: the ladder's owner checks its support from its
    /// colliders): one box round its rails and rungs, just off the deck's edge from its foot to a little over the deck's top,
    /// on the piece's layer, so the game's hover and E find the ladder (<see cref="KwLadderClimb"/>) from the deck and from
    /// the water, and ships and swimmers bump it. Nothing over the deck itself (its hooks are only drawn), so nobody trips
    /// stepping off it. Made once, when the ladder builds.
    /// </summary>
    public static class KwPierLadderBody
    {
        private const float Over = 0.1f, Wide = 0.62f, Deep = 0.11f;

        public static void Make(Transform piece, float length)
        {
            GameObject part = new GameObject("bs_pier_ladder_climb") { layer = piece.gameObject.layer };
            part.transform.SetParent(piece, false);
            BoxCollider box = part.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, (Over - length) * 0.5f, KwPierLadderShape.Out);
            box.size = new Vector3(Wide, length + Over, Deep);
        }
    }
}
