using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands round a ladder the tool would build (<see cref="KwLadderTool"/>), as the game's placement ray sees it.
    /// Its length: straight down from just under its top, in front of the edge it hangs from, to the first thing under it
    /// (the piece it hangs from left out), within <see cref="KwLadder.MaxLength"/>; from an open edge that thing must be a
    /// floor of the ship to climb down to (a deck, a cabin, a hatch or a hold's floor), not the sea, the dock or the hull's
    /// outside. And an open edge must be open there: nothing within <see cref="Carry"/> of its height just beyond it (a deck
    /// or roof carrying on, a wall), and nothing standing on it just behind (a wall, a mast). Local to the player aiming.
    /// </summary>
    public static class KwLadderDrop
    {
        /// <summary>How far under the top the way down is looked along (clear of the deck and a hatch's coaming), and how far into the opening.</summary>
        public const float Below = 0.3f, Inset = 0.065f;

        private const float Carry = 0.35f, Stands = 0.25f, Beyond = 0.25f, Over = 0.5f, Above = 1.5f;
        private static readonly string Deck = KwToolPieces.PartPrefix + "deck";
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>How long the ladder must be to reach what it stands on (0 for nothing it may stand on within reach).</summary>
        public static float Drop(Vector3 top, Vector3 facing, Transform from, bool onFloor)
        {
            if (!Under(top, facing, from, out RaycastHit hit) || (onFloor && !Floor(hit)))
                return 0f;
            return hit.distance + Below;
        }

        /// <summary>The first thing a ladder with its top there would stand on, within its reach (the piece it hangs from left out); false for nothing.</summary>
        public static bool Under(Vector3 top, Vector3 facing, Transform from, out RaycastHit hit) =>
            First(top + facing * Inset + Vector3.down * Below, KwLadder.MaxLength, from, out hit);

        /// <summary>Why an open edge is not open where the ladder would stand; null when it is.</summary>
        public static string? Clear(Vector3 top, Vector3 outward, Transform from)
        {
            if (First(top + outward * Beyond + Vector3.up * Over, Over + Carry, from, out _))
                return "That edge is not open there: something carries on from it";
            if (First(top - outward * Beyond + Vector3.up * Above, Above - Stands, null, out _))
                return "Something stands on the edge there";
            return null;
        }

        // The first thing straight down from a point within a length, a piece's own parts left out.
        private static bool First(Vector3 from, float length, Transform? leave, out RaycastHit first)
        {
            first = default;
            float nearest = float.MaxValue;
            int mask = Player.m_localPlayer != null ? Player.m_localPlayer.m_placeRayMask : Physics.DefaultRaycastLayers;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, Hits, length, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.distance < nearest && (leave == null || !hit.collider.transform.IsChildOf(leave)))
                    (nearest, first) = (hit.distance, hit);
            }
            return nearest < float.MaxValue;
        }

        /// <summary>Whether a hit is a floor of the ship: a deck, a cabin (its roof or floor), a hatch, or a planking part's hold floor.</summary>
        public static bool Floor(RaycastHit hit)
        {
            if (hit.collider.name == "sole")
                return true;
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            if (piece == null)
                return false;
            string prefab = Utils.GetPrefabName(piece.gameObject);
            return prefab == Deck || prefab.StartsWith(KwHatch.Prefix, System.StringComparison.Ordinal) || KwCabinRoofs.Roof(piece, out _, out _);
        }
    }
}
