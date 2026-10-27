using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where an anchor windlass's anchor goes overboard, worked out from the ship's pieces alone (<see cref="KwAnchorShip"/>),
    /// the same on every peer: of every place its cable could leave the ship within <see cref="KwAnchorSpots.Reach"/>
    /// (<see cref="KwAnchorSpots"/>), the one with the shortest cable that is clear all the way: on deck from the drum to the
    /// fairlead (<see cref="KwAnchorRoute"/>), and overboard down to the weighed anchor hanging clear of the hull
    /// (<see cref="KwAnchorHang"/>); a chock over the sheer before a hawse pipe (taken only for a windlass in a cabin, or
    /// with no chock clear), a turn at a deck lead counted as <see cref="TurnCost"/> of cable and an arm out from the hull
    /// as cable twice over, so either is fitted only when it must be or is clearly shorter, and a little more for a place
    /// off to the side of the drum (<see cref="Aside"/>). So a windlass facing the side leads its cable straight out over
    /// the side before it. Without one, why (what stood in the way of the nearest).
    /// </summary>
    public static class KwAnchorPlan
    {
        /// <summary>What a turn at a deck lead counts for as cable (the user's: an eye and a long way round where the side
        /// straight before the windlass would do "seems inefficient"); what each metre a fairlead stands to the side of the
        /// drum's middle counts for, so of two places about as near the one straight out from the drum wins.</summary>
        public const float TurnCost = 0.6f, Aside = 0.3f;

        // What an arm's reach counts for as cable; how far out an arm is before a knee holds it; how many places are tried at most.
        private const float ArmCost = 2f, KneeFrom = 0.08f;
        private const int Tries = 200;

        /// <summary>The lead for a windlass among a ship's pieces (blueprint pieces in any one frame, the windlass among them).</summary>
        public static KwAnchorLead For(List<KwBpPiece> pieces, KwBpPiece windlass)
        {
            KwAnchorShip ship = new KwAnchorShip(pieces, windlass);
            if (ship.Hulls.Count == 0)
                return KwAnchorLead.Stowed("there is no hull round it");
            string? why = null;
            KwAnchorLead? lead = ship.Housed ? null : Best(ship, KwAnchorSpots.Chocks(ship), ref why);
            lead ??= Best(ship, KwAnchorSpots.Pipes(ship), ref why);
            return lead ?? KwAnchorLead.Stowed(why ?? $"no side of the ship is within {KwAnchorSpots.Reach:0} m");
        }

        // The clear place with the shortest cable, nearest first; a place is passed by once even its straight way is longer.
        private static KwAnchorLead? Best(KwAnchorShip ship, List<KwAnchorSpot> spots, ref string? why)
        {
            KwAnchorLead? best = null;
            float cost = float.MaxValue;
            for (int i = 0; i < spots.Count && i < Tries && spots[i].Least < cost; i++)
            {
                KwAnchorLead? lead = Try(ship, spots[i], ref why);
                float length = lead != null ? Cost(lead) : float.MaxValue;
                if (length < cost)
                    (best, cost) = (lead, length);
            }
            return best;
        }

        /// <summary>What a lead counts for: its cable on deck, each turn at a deck lead, its fairlead's place to the side of
        /// the drum, an arm's reach and its place's penalty.</summary>
        public static float Cost(KwAnchorLead lead) => KwAnchorRoute.Length(lead.Deck) + (lead.Deck.Count - 2) * TurnCost
            + Mathf.Abs(lead.Spot!.Inner.y) * Aside + lead.Reach * ArmCost + lead.Spot.Penalty;

        // One place: the cable to it on deck, then the anchor hung from it; null with why the first time one fails.
        private static KwAnchorLead? Try(KwAnchorShip ship, KwAnchorSpot spot, ref string? why)
        {
            List<Vector3>? deck = KwAnchorRoute.Find(ship, spot.Inner, out string? blocked);
            if (deck == null)
            {
                why ??= Words(blocked, false);
                return null;
            }
            (float Reach, Vector3 Ring, Vector3 Stock, Vector3 Up)? hang = KwAnchorHang.Find(ship, spot, out string? hit);
            if (hang == null)
            {
                why ??= Words(hit, true);
                return null;
            }
            KwAnchorLead lead = new KwAnchorLead(spot, deck, hang.Value.Reach, hang.Value.Ring, hang.Value.Stock, hang.Value.Up);
            if (lead.Reach > KneeFrom)
                lead.Knee = Knee(ship, lead);
            return lead;
        }

        // Where a knee from under the arm's end meets the hull, slanting down and in to it (null when it meets nothing).
        private static Vector3? Knee(KwAnchorShip ship, KwAnchorLead lead)
        {
            KwAnchorSpot spot = lead.Spot!;
            Vector3 from = lead.Hawse - new Vector3(0f, 0f, 0.1f), to = spot.At - spot.Outward * 0.1f - new Vector3(0f, 0f, 0.6f);
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                Vector3 p = Vector3.Lerp(from, to, t);
                if (ship.Blocker(p, 0f, true) != null)
                    return p;
            }
            return null;
        }

        // Why the nearest place would not do, in words for the hover.
        private static string Words(string? what, bool overboard) => overboard
            ? $"{what ?? "the hull"} would meet the anchor hung over the side"
            : $"{what ?? "something"} is in the way of its cable to the nearest side";
    }
}
