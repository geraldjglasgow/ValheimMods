using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the parts no biome tier dresses cost a ship built from the hammer (<see cref="KwBpCost"/>): a ladder one wood
    /// for every metre of its height, at least one; a mast one wood for every metre of its height, at least two, its ropes
    /// and rigging included; and each sail on the mast, in the item its cloth is made of (<see cref="KwSailCloth.Item"/>),
    /// one for every 2 m² of its area, at least one a sail, and what else it carries (a crow's nest, a fighting top, a
    /// pennant); a bowsprit by its length and its jibs by their area (<see cref="Bowsprit"/>). Worked out from the part's
    /// saved text (a mast's sails as they are placed to fit it, <see cref="KwMastSpec.TryParse"/>; a bowsprit's jibs as
    /// they are cut to the blueprint's foremast), so every peer, the hammer's requirements and the click agree. Nothing for
    /// a part whose saved text does not read.
    /// </summary>
    public static class KwRigCost
    {
        private const string Wood = "Wood";
        private const int LeastMast = 2, NestWood = 6, TopWood = 8;
        private const float SailArea = 2f;

        /// <summary>A ladder's cost, from its saved text ("ladder|length").</summary>
        public static List<(string Item, int Amount)> Ladder(string saved)
        {
            List<(string Item, int Amount)> cost = new List<(string Item, int Amount)>();
            if (KwLadder.TryLength(saved, out float length))
                cost.Add((Wood, Amount(length, 1)));
            return cost;
        }

        /// <summary>A mast's cost and its sails', from its saved text ("mast|height|sail|sail|...").</summary>
        public static List<(string Item, int Amount)> Mast(string saved)
        {
            List<(string Item, int Amount)> cost = new List<(string Item, int Amount)>();
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return cost;
            cost.Add((Wood, Amount(mast.Mast, LeastMast)));
            foreach (KwSailSpec sail in mast.Sails)
                KwBpCost.Add(cost, KwSailCloth.Item(sail.Cloth), Amount(sail.Area / SailArea, 1));
            Tops(cost, mast);
            return cost;
        }

        // What the mast carries besides its sails: a crow's nest six wood, a fighting top eight (its climbing pegs are the
        // mast's), a pennant one of its cloth's item (the sails' cloth, or linen thread dyed).
        private static void Tops(List<(string Item, int Amount)> cost, KwMastSpec mast)
        {
            KwMastTops tops = mast.Tops;
            KwBpCost.Add(cost, Wood, (tops.Nest ? NestWood : 0) + (tops.HasTop ? TopWood : 0));
            if (tops.HasPennant)
                KwBpCost.Add(cost, KwSailCloth.Item(tops.Pennant == KwPennantLook.Cloth && mast.Sails.Count > 0 ? mast.Sails[0].Cloth : KwSailCloth.Plain), 1);
        }

        /// <summary>
        /// A bowsprit's cost and its jibs': one wood for every metre of its length, at least two, and one for each jib's club;
        /// each jib in its cloth's item, one for every 2 m² of it, at least one. Its jibs are cut to the stay to the
        /// blueprint's foremast (<see cref="KwJibStay.InBlueprint"/>), as the launched ship sets them; with no foremast they
        /// are not set, and cost nothing.
        /// </summary>
        public static List<(string Item, int Amount)> Bowsprit(KwBlueprint blueprint, KwBpPiece piece)
        {
            List<(string Item, int Amount)> cost = new List<(string Item, int Amount)>();
            if (!KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec))
                return cost;
            KwJibStay? stay = KwJibStay.InBlueprint(blueprint, piece);
            List<KwJibCut> jibs = stay != null ? KwJibPlan.Of(spec, stay) : new List<KwJibCut>();
            cost.Add((Wood, Amount(spec.Length, LeastMast) + jibs.Count));
            foreach (KwJibCut jib in jibs)
                KwBpCost.Add(cost, KwSailCloth.Item(spec.Cloth), Amount(jib.Area / SailArea, 1));
            return cost;
        }

        // A whole number for a share, rounded up (a hair over a whole one not counted), at least the least.
        private static int Amount(float share, int least) => Mathf.Max(least, Mathf.CeilToInt(share - 0.001f));
    }
}
