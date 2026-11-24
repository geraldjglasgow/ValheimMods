using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Which page a creature or a dungeon chest gives (features/casters.md, "The spellbook", "Where pages come from"): one
    /// of the loot pages (<see cref="Spell.Loot"/>: never a boss's) of the source's biome and the biomes before it,
    /// <see cref="OwnBiome"/> times as likely from its own biome's. The biome is the world's at the source's x and z, so a
    /// dungeon (built high above its entrance) counts as the biome outside it; the sea counts as the Mountains (a sailing
    /// tier). Asked on the machine that makes the game's own loot, so every player finds the same item.
    /// </summary>
    public static class PageDrops
    {
        private const float OwnBiome = 3f;

        public static bool Roll(float chance) => Random.value < chance;

        /// <summary>A page prefab for a source at this position, or null when no page fits.</summary>
        public static GameObject Pick(Vector3 position)
        {
            int tier = Tier(position);
            float total = 0f;
            foreach (Spell spell in Spells.All)
                total += Weight(spell, tier);
            float roll = Random.value * total;
            foreach (Spell spell in Spells.All)
            {
                float weight = Weight(spell, tier);
                roll -= weight;
                if (weight > 0f && roll <= 0f)
                    return ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(spell.Item) : null;
            }
            return null;
        }

        private static float Weight(Spell spell, int tier)
        {
            if (!spell.Loot || spell.Tier > tier)
                return 0f;
            return spell.Tier == tier ? OwnBiome : 1f;
        }

        /// <summary>The biome's place in the game's order, as <see cref="Spell.Tier"/> counts it.</summary>
        public static int Tier(Vector3 position)
        {
            WorldGenerator world = WorldGenerator.instance;
            switch (world != null ? world.GetBiome(position.x, position.z) : Heightmap.Biome.Meadows)
            {
                case Heightmap.Biome.BlackForest:
                    return 2;
                case Heightmap.Biome.Swamp:
                    return 3;
                case Heightmap.Biome.Mountain:
                case Heightmap.Biome.Ocean:
                    return 4;
                case Heightmap.Biome.Plains:
                    return 5;
                case Heightmap.Biome.Mistlands:
                    return 6;
                case Heightmap.Biome.AshLands:
                case Heightmap.Biome.DeepNorth:
                    return 7;
                default:
                    return 1;
            }
        }
    }
}
