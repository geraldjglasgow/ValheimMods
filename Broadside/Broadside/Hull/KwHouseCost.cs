using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a cabin costs a ship built from the hammer (<see cref="KwBiomeCost"/>, <see cref="KwBpCost"/>), in its tier's
    /// materials at the planking's and deck's rates (one of the bulk every 2 m², one of the fittings every 4 m²,
    /// <see cref="KwBiomeCost.For"/>): its roof in the tier's deck materials by its area; its own walls and the hull's
    /// raised sides in the tier's hull materials by their area; one wood for each corner post. Worked out from the saved
    /// text alone, so every peer, the hammer's requirements and the click agree. Nothing for a text that does not read.
    /// </summary>
    public static class KwHouseCost
    {
        public static List<(string Item, int Amount)> Of(string tool, string saved, KwBiomeTier tier)
        {
            List<(string Item, int Amount)> cost = new List<(string Item, int Amount)>();
            if (!KwHouses.Owns(tool) || !KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return cost;
            Add(cost, KwBiomeCost.For(KwPart.Deck, tier, KwCabinRoof.Area(spec)));
            Add(cost, KwBiomeCost.For(KwPart.Hull, tier, KwCabinShape.WallArea(spec)));
            int posts = KwCabinPosts.Count(spec);
            if (posts > 0)
                KwBpCost.Add(cost, "Wood", posts);
            return cost;
        }

        private static void Add(List<(string Item, int Amount)> cost, List<(string Item, int Amount)> more)
        {
            foreach ((string item, int amount) in more)
                KwBpCost.Add(cost, item, amount);
        }
    }
}
