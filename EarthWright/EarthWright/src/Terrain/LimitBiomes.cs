using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>The height limits one biome overrides; NaN leaves the global setting.</summary>
    public struct BiomeLimit
    {
        public float Raise;
        public float Dig;

        public static readonly BiomeLimit None = new BiomeLimit { Raise = float.NaN, Dig = float.NaN };
    }

    /// <summary>
    /// The biomes a per-biome height limit can name, and which biome a world position belongs to. The biome is read from
    /// the world's biome map (the same one the game colours the ground and picks the environment by), an array lookup
    /// that gives every machine the same answer for the same point, so both owners of a shared edge agree.
    /// </summary>
    public static class LimitBiomes
    {
        public const int Slots = 9;

        public const string Names = "Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth, Ocean";

        /// <summary>The biome names by slot, as the game spells them (Heightmap.Biome).</summary>
        private static readonly string[] slotNames = { "Meadows", "Swamp", "Mountain", "BlackForest", "Plains", "AshLands", "DeepNorth", "Ocean", "Mistlands" };

        /// <summary>The slot of a single biome by name, case-insensitive; false for unknown names and combinations (All, Land).</summary>
        public static bool TryParse(string name, out int slot)
        {
            string trimmed = name != null ? name.Trim() : "";
            for (slot = 0; slot < Slots; slot++)
            {
                if (string.Equals(slotNames[slot], trimmed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            slot = -1;
            return false;
        }

        public static string NameOf(int slot) => slot >= 0 && slot < Slots ? slotNames[slot] : "?";

        /// <summary>0..8 for a single biome, -1 otherwise.</summary>
        public static int Slot(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows: return 0;
                case Heightmap.Biome.Swamp: return 1;
                case Heightmap.Biome.Mountain: return 2;
                case Heightmap.Biome.BlackForest: return 3;
                case Heightmap.Biome.Plains: return 4;
                case Heightmap.Biome.AshLands: return 5;
                case Heightmap.Biome.DeepNorth: return 6;
                case Heightmap.Biome.Ocean: return 7;
                case Heightmap.Biome.Mistlands: return 8;
                default: return -1;
            }
        }

        /// <summary>The biome at a world position, None before a world is loaded.</summary>
        public static Heightmap.Biome At(float x, float z)
        {
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null || generator.m_world == null)
                return Heightmap.Biome.None;
            BiomeSector sector = generator.GetBiomeSector(x, z);
            if (sector != null && sector != BiomeSector.EmptyMeadows && sector != BiomeSector.EmptyBlackForest)
                return sector.Biome;
            return generator.GetBiome(x, z);
        }

        public static Heightmap.Biome At(Vector3 world) => At(world.x, world.z);
    }
}
