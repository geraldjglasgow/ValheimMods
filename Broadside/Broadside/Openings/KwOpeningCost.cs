using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a part's openings add to its cost on a ship built from the hammer (<see cref="KwBpCost"/>; on the dock they are
    /// free, as every part is): each opening's frame one of its part's tier's hull bulk (two for heavy timber), an
    /// iron-banded frame one of the tier's hull fittings besides; each cover (a lid, shutters) one of the tier's deck bulk;
    /// each door two wood, a double door four (two doors' worth). From the saved text alone, so every peer, the hammer's
    /// requirements and the click agree.
    /// </summary>
    public static class KwOpeningCost
    {
        /// <summary>A part's cost with its openings' added.</summary>
        public static List<(string Item, int Amount)> With(List<(string Item, int Amount)> cost, string saved, KwBiomeTier tier)
        {
            (string hull, string trim) = tier.Materials(KwPart.Hull);
            string deck = tier.Materials(KwPart.Deck).Main;
            foreach (KwOpening o in KwOpeningText.Of(saved))
            {
                KwBpCost.Add(cost, hull, o.Frame == KwOpening.Heavy ? 2 : 1);
                if (o.Frame == KwOpening.Iron)
                    KwBpCost.Add(cost, trim, 1);
                if (o.HasDoor)
                    KwBpCost.Add(cost, "Wood", o.Cover == KwOpening.DoubleDoor ? 4 : 2);
                else if (o.Covered)
                    KwBpCost.Add(cost, deck, 1);
            }
            return cost;
        }
    }
}
