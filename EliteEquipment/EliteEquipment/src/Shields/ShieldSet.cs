using EliteEquipment.Core;
using UnityEngine;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// One new shield: its kind (buckler or round), its prefab and the workshop model it wears, the game item it is copied
    /// from (block and bash sounds, effects, quality levels, equip timing), the game shield of its biome (station, station
    /// level, repair station and upgrade kits come from that shield's recipe), its name word and its numbers. The item is
    /// made once per process.
    /// </summary>
    public sealed class ShieldSet
    {
        public ShieldKind Kind;
        public string Prefab;
        public string Asset;
        public string Base;
        public string Shield;

        /// <summary>
        /// The game item whose own material the model wears as it is (its UVs are laid on that item's atlas: the same
        /// texture, metal and shine); null when the model wears its own bake on the base item's material.
        /// </summary>
        public string Look;
        public string Name;
        public string Description;
        public float Block;
        public float Deflection;
        public float DeflectionPerLevel;
        public float Weight;
        public float Durability;
        /// <summary>Smoothness of the parts that are not metal, and of the metal (the model's metal mask), as the biome's shield.</summary>
        public float Gloss;
        public float MetalGloss;
        public RecipePart[] Cost;

        /// <summary>The item prefab, once made; null when it could not be.</summary>
        public GameObject Item;

        /// <summary>The name word, "$" and the workshop asset's name ("$ee_buckler_wood", "$ee_shield_bronze").</summary>
        public string Token => "$" + Word;

        public string Word => Asset;
    }
}
