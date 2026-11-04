using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the trawl catches, by the biome of the water the net is in (<see cref="KwTrawlSea.Biome"/>): the game's own
    /// fish items (their prefab names, read in the game 2026-11-03 with the game's spawn lists: Fish1 perch and Fish2 pike in
    /// the Meadows, Black Forest and Plains, Fish5 trollfish in the Black Forest, Fish6 giant herring in the Swamp, Fish7
    /// grouper in the Plains, Fish3 tuna, Fish8 coral cod and Fish12 pufferfish in the Ocean, Fish9 anglerfish and Fish12 in
    /// the Mistlands, Fish10 northern salmon in the Deep North, Fish11 magmafish in the Ashlands), each by a weight of
    /// chance. The Mountains have none (the game's tetra lives in caves). A fish the game lacks is left out when landed.
    /// </summary>
    public static class KwTrawlFish
    {
        private static readonly Dictionary<Heightmap.Biome, (string Prefab, int Weight)[]> Mix = new Dictionary<Heightmap.Biome, (string, int)[]>
        {
            [Heightmap.Biome.Meadows] = new[] { ("Fish1", 6), ("Fish2", 4) },
            [Heightmap.Biome.BlackForest] = new[] { ("Fish2", 4), ("Fish1", 3), ("Fish5", 3) },
            [Heightmap.Biome.Swamp] = new[] { ("Fish6", 1) },
            [Heightmap.Biome.Plains] = new[] { ("Fish7", 5), ("Fish2", 3), ("Fish1", 2) },
            [Heightmap.Biome.Ocean] = new[] { ("Fish3", 4), ("Fish8", 4), ("Fish12", 2) },
            [Heightmap.Biome.Mistlands] = new[] { ("Fish9", 7), ("Fish12", 3) },
            [Heightmap.Biome.DeepNorth] = new[] { ("Fish10", 1) },
            [Heightmap.Biome.AshLands] = new[] { ("Fish11", 1) },
        };

        /// <summary>Whether the water of this biome has fish for the net.</summary>
        public static bool Has(Heightmap.Biome biome) => Mix.ContainsKey(biome);

        /// <summary>A fish of this biome's water by chance (<paramref name="roll"/> 0 to 1), or null where there are none.</summary>
        public static string? Pick(Heightmap.Biome biome, float roll)
        {
            if (!Mix.TryGetValue(biome, out (string Prefab, int Weight)[] fish))
                return null;
            int total = 0;
            foreach ((string _, int weight) in fish)
                total += weight;
            float at = Mathf.Clamp01(roll) * total;
            foreach ((string prefab, int weight) in fish)
            {
                if (at < weight)
                    return prefab;
                at -= weight;
            }
            return fish[fish.Length - 1].Prefab;
        }
    }
}
