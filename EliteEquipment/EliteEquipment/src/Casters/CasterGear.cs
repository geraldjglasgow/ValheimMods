using System.Collections.Generic;
using System.Linq;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>A material in a recipe: the game's item prefab, the amount to craft, the amount per upgrade level.</summary>
    public sealed class GearCost
    {
        public string Item;
        public int Amount;
        public int PerLevel;

        public GearCost(string item, int amount, int perLevel)
        {
            Item = item;
            Amount = amount;
            PerLevel = perLevel;
        }
    }

    /// <summary>
    /// One caster item (features/casters.md, "Items (Meadows numbers)"): its prefab, the game item it is a copy of, the
    /// workshop model (bundle and asset) it wears, its word (translations/English.yml: the name, and the description as the
    /// word + "_desc"), the eitr it gives while worn or wielded, its armour and its
    /// recipe, crafted beside <see cref="RecipeBeside"/> (same station and level). <see cref="Item"/> is the built prefab.
    /// </summary>
    public sealed class GearPiece
    {
        public string Prefab;
        public string Base;
        public string Bundle;
        public string Asset;
        public string Word;
        public float Eitr;
        public float Armour;
        /// <summary>A wand's heal power at quality 1, and its bolt's pierce and spirit (each).</summary>
        public float HealPower;
        public float Bolt;
        /// <summary>A robe piece's movement change, which part of its set it is ("hood", "robe", "trousers"), and its set.</summary>
        public float Movement;
        public string Part;
        public RobeSet Set;
        public string RecipeBeside;
        public GearCost[] Cost;

        public GameObject Item;

        public string Token => Lang.Token(Word);
        public string DescriptionToken => Lang.Token(Word + "_desc");
    }

    /// <summary>The caster gear built so far (the Meadows', and the Black Forest's wand) and the numbers the spells read from it.</summary>
    public static class CasterGear
    {
        public const float HealPowerPerQuality = 1.5f;
        public const float FocusHealPower = 3f;
        public const float FocusEitrRegen = 0.1f;

        /// <summary>
        /// The robes follow the game's own mage armour (user, 2026-10-11: "follow pattern in late game that is established"):
        /// the Eitr-weave has half the Carapace's armour (16 to 32), the Embla half the Flametal's (19 to 38), both +2 per
        /// level, and -2% movement on robe and trousers where the heavy set has -5%. The Meadows' heavy set is leather (2, no
        /// movement cost): 1 armour a piece, +2 per level, no movement cost.
        /// </summary>
        public const float ArmourPerLevel = 2f;

        public static readonly GearPiece Wand = new GearPiece
        {
            Prefab = "EE_Wand_Meadows", Base = "Club", Bundle = "ee_wand_meadows", Asset = "ee_wand_meadows",
            Word = "ee_wand_meadows", Eitr = 10f, HealPower = 15f, Bolt = 6f, RecipeBeside = "AxeFlint",
            Cost = new[] { new GearCost("Wood", 5, 2), new GearCost("Resin", 3, 1), new GearCost("Feathers", 2, 1), new GearCost("Dandelion", 3, 1) },
        };

        /// <summary>
        /// The Black Forest's wand (features/casters.md, "Wands": tier 2 at the forge, heal power 25, +15 eitr; its bolt about
        /// 40% of the bronze sword's 35). The model was made by ChatGPT from the workshop's brief (user, 2026-10-11).
        /// </summary>
        public static readonly GearPiece Greywood = new GearPiece
        {
            Prefab = "EE_Wand_BlackForest", Base = "Club", Bundle = "ee_wand_blackforest", Asset = "ee_wand_blackforest",
            Word = "ee_wand_blackforest", Eitr = 15f, HealPower = 25f, Bolt = 7f, RecipeBeside = "MaceBronze",
            Cost = new[] { new GearCost("FineWood", 4, 2), new GearCost("Bronze", 2, 1), new GearCost("GreydwarfEye", 6, 3) },
        };

        public static readonly IReadOnlyList<GearPiece> Wands = new[] { Wand, Greywood };

        public static readonly GearPiece Focus = new GearPiece
        {
            Prefab = "EE_Focus_Meadows", Base = "ShieldWood", Bundle = "ee_focus_meadows", Asset = "ee_focus_meadows",
            Word = "ee_focus_meadows", RecipeBeside = "ShieldWood",
            Cost = new[] { new GearCost("Feathers", 4, 2), new GearCost("LeatherScraps", 3, 1), new GearCost("Flint", 2, 1) },
        };

        /// <summary>Every robe piece of every set (<see cref="RobeSets"/>).</summary>
        public static readonly IReadOnlyList<GearPiece> Robes = RobeSets.Pieces;

        public static readonly IReadOnlyList<GearPiece> All = new[] { Wand, Greywood, Focus }.Concat(Robes).ToArray();

        public static GearPiece ByPrefab(string prefab)
        {
            foreach (GearPiece piece in All)
            {
                if (piece.Prefab == prefab)
                    return piece;
            }
            return null;
        }
    }
}
