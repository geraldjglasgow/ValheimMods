namespace EliteCreaturesPack.Crafting
{
    /// <summary>
    /// Elite Crafting's level of each biome as its default file has it (<c>biomes:</c>; the ocean counts as the Mountain),
    /// for a creature whose spawn biomes are a setting. As Elite Crafting rates a creature by its spawn entries, the lowest
    /// level among the biomes counts.
    /// </summary>
    internal static class BiomeLevels
    {
        private static readonly (Heightmap.Biome Biome, int Level)[] Levels =
        {
            (Heightmap.Biome.Meadows, 1), (Heightmap.Biome.BlackForest, 2), (Heightmap.Biome.Swamp, 3),
            (Heightmap.Biome.Mountain, 4), (Heightmap.Biome.Ocean, 4), (Heightmap.Biome.Plains, 5),
            (Heightmap.Biome.Mistlands, 6), (Heightmap.Biome.AshLands, 7), (Heightmap.Biome.DeepNorth, 8),
        };

        /// <summary>The lowest level among the biomes set in <paramref name="biomes"/>; <paramref name="none"/> when none is.</summary>
        public static int Lowest(Heightmap.Biome biomes, int none)
        {
            int lowest = 0;
            foreach ((Heightmap.Biome biome, int level) in Levels)
            {
                if ((biomes & biome) != 0 && (lowest == 0 || level < lowest))
                {
                    lowest = level;
                }
            }
            return lowest > 0 ? lowest : none;
        }
    }
}
