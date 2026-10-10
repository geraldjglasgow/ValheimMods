using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Studio
{
    /// <summary>
    /// The biome each item is meant for, worked out from the game's data: raw materials from <see cref="TierSeeds"/>,
    /// creature drops from where the creature spawns, and anything made from the highest tier among what it takes
    /// (ingredients, the station it is made at, a smelter's fuel). An item made more than one way takes the earliest.
    /// An estimate: an item whose materials the studio cannot place has no tier.
    /// </summary>
    internal static class ItemTiers
    {
        private const int Passes = 32;

        /// <summary>Every tier the game's data gives, by prefab name.</summary>
        internal static Dictionary<string, Tier> Compute()
        {
            var tiers = new Dictionary<string, Tier>(TierSources.Drops(), StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Tier> seed in TierSeeds.Materials) tiers[seed.Key] = seed.Value;
            List<TierSource> sources = TierSources.All().Where(s => !TierSeeds.Materials.ContainsKey(s.Product)).ToList();
            Settle(sources, tiers, true);
            Settle(sources, tiers, false);
            return tiers;
        }

        private static void Settle(List<TierSource> sources, Dictionary<string, Tier> tiers, bool strict)
        {
            for (int pass = 0; pass < Passes; pass++)
            {
                bool changed = false;
                foreach (TierSource source in sources) changed |= Apply(source, tiers, strict);
                if (!changed) return;
            }
        }

        // Strict: only from inputs that are all placed, and it may lower a tier another way gave. Otherwise from the
        // inputs that are placed, for products nothing placed yet (a mod's item made partly from the mod's own things).
        private static bool Apply(TierSource source, Dictionary<string, Tier> tiers, bool strict)
        {
            bool placed = tiers.TryGetValue(source.Product, out Tier known);
            if (!strict && placed) return false;
            Tier? made = Highest(source.Inputs, tiers, strict);
            if (made == null || (placed && made.Value.Rank >= known.Rank)) return false;
            tiers[source.Product] = made.Value;
            return true;
        }

        private static Tier? Highest(List<string> inputs, Dictionary<string, Tier> tiers, bool strict)
        {
            Tier? highest = null;
            foreach (string input in inputs)
            {
                if (!tiers.TryGetValue(input, out Tier tier))
                {
                    if (strict) return null;
                    continue;
                }
                if (highest == null || tier.Rank > highest.Value.Rank) highest = tier;
            }
            return highest;
        }
    }
}
