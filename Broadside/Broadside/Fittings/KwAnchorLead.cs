using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a windlass's anchor is led overboard (<see cref="KwAnchorPlan"/>), in the windlass's kit frame: the fairlead (a
    /// chock on the sheer or a hawse pipe, <see cref="Spot"/>, held out on an arm of <see cref="Reach"/> when the hull below
    /// needs it), the cable's fixed points on deck from the drum to the fairlead (a deck lead among them when it has one),
    /// where the cable leaves the ship (<see cref="Hawse"/>) and where the weighed anchor's ring hangs, its stock along
    /// <see cref="Stock"/> and its shank along <see cref="Up"/>. Or, when no clear place was found, none, with why: the
    /// anchor then stays stowed on the deck.
    /// </summary>
    public sealed class KwAnchorLead
    {
        private KwAnchorLead(string why) => Why = why;

        public KwAnchorLead(KwAnchorSpot spot, List<Vector3> deck, float reach, Vector3 ring, Vector3 stock, Vector3 up)
        {
            (Spot, Deck, Reach, Ring, Stock, Up, Why) = (spot, deck, reach, ring, stock, up, "");
            Hawse = spot.Outer + spot.Outward * reach;
        }

        /// <summary>None found, with why (for the windlass's hover).</summary>
        public static KwAnchorLead Stowed(string why) => new KwAnchorLead(why);

        public bool Found => Spot != null;
        public string Why { get; }
        public KwAnchorSpot? Spot { get; }

        /// <summary>The cable's fixed points on deck: off the drum, a deck lead when it has one, the fairlead's inner lip.</summary>
        public List<Vector3> Deck { get; } = new List<Vector3>();

        /// <summary>A deck lead's place, when the cable turns at one.</summary>
        public Vector3? DeckLead => Deck.Count > 2 ? Deck[1] : (Vector3?)null;

        /// <summary>How far out from the fairlead's outer lip the arm holds the cable (0: none).</summary>
        public float Reach { get; }

        /// <summary>Where the cable leaves the ship: the outer lip, or the arm's end.</summary>
        public Vector3 Hawse { get; }

        public Vector3 Ring { get; }
        public Vector3 Stock { get; }

        /// <summary>The weighed anchor's shank, ring to crown upward: straight up, or leaning when it rests against the hull.</summary>
        public Vector3 Up { get; }

        /// <summary>Where an arm's knee meets the hull under it, when it has one.</summary>
        public Vector3? Knee { get; set; }

        /// <summary>The cable's points on the ship, drum to hawse, the runs on deck sagging a little; then the ring when weighed.</summary>
        public List<Vector3> Cable(bool toRing)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i + 1 < Deck.Count; i++)
            {
                for (int k = 0; k < Sags; k++)
                    points.Add(KwAnchorRoute.Sagged(Deck[i], Deck[i + 1], k / (float)Sags));
            }
            points.Add(Deck[Deck.Count - 1]);
            points.Add(Spot!.Outer);
            if (Reach > 0f)
                points.Add(Hawse);
            if (toRing)
                points.Add(Ring + Up * 0.09f);
            return points;
        }

        // How many points each run on deck is drawn with.
        private const int Sags = 8;

        public override string ToString() => !Found ? "stowed: " + Why
            : $"{(Spot!.Pipe ? "hawse pipe" : Spot.Railed ? "chock on the railing" : "chock on the sheer")} at {F(Spot.At)}, cable {KwAnchorRoute.Length(Deck):0.00} m"
              + $"{(DeckLead.HasValue ? " by a deck lead" : "")}, arm {Reach:0.00} m, ring {F(Ring)}";

        private static string F(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.00}, {1:0.00}, {2:0.00})", v.x, v.y, v.z);
    }
}
