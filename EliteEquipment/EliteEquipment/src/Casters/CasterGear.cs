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
        /// <summary>
        /// A wand's heal power at quality 1, and its bolt's pierce and spirit (each); a spellbook's heal power is what it adds
        /// to the wand's while held (<see cref="EliteEquipment.Casters.HealPower"/>).
        /// </summary>
        public float HealPower;
        public float Bolt;
        /// <summary>A robe piece's movement change, which part of its set it is ("hood", "robe", "trousers"), and its set.</summary>
        public float Movement;
        public string Part;
        public RobeSet Set;
        /// <summary>A spellbook's pages (<see cref="BookPages"/>) and its block power at quality 1 (+1 a level, <see cref="BookItem"/>).</summary>
        public int Pages;
        public float Block;
        /// <summary>
        /// A wand's eitr regen while wielded (the Mistlands' on), a spellbook's while held (the game's equipment modifier:
        /// 0.25 is +25%).
        /// </summary>
        public float EitrRegen;
        public string RecipeBeside;
        public GearCost[] Cost;

        public GameObject Item;

        public string Token => Lang.Token(Word);
        public string DescriptionToken => Lang.Token(Word + "_desc");
    }

    /// <summary>The caster gear built so far (the wands, the spellbooks, the robes) and the numbers the spells read from it.</summary>
    public static class CasterGear
    {
        public const float HealPowerPerQuality = 1.5f;

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

        /// <summary>
        /// The Swamp's wand (features/casters.md, "Wands": tier 3 at the forge, heal power 35, +20 eitr; its bolt about 40% of
        /// the iron sword's 55). Asked for by the user on 2026-11-02.
        /// </summary>
        public static readonly GearPiece Rootbound = new GearPiece
        {
            Prefab = "EE_Wand_Swamp", Base = "Club", Bundle = "ee_wand_swamp", Asset = "ee_wand_swamp",
            Word = "ee_wand_swamp", Eitr = 20f, HealPower = 35f, Bolt = 11f, RecipeBeside = "MaceIron",
            Cost = new[] { new GearCost("ElderBark", 6, 3), new GearCost("Iron", 3, 1), new GearCost("Root", 2, 1) },
        };

        /// <summary>
        /// The Mountains' wand (features/casters.md, "Wands": tier 4 at the forge, heal power 45, +25 eitr; materials the
        /// user's, 2026-11-02: "finewood, silver, ice gland"): its bolt frost and spirit, about 40% of the silver sword's 75.
        /// </summary>
        public static readonly GearPiece Frostsilver = new GearPiece
        {
            Prefab = "EE_Wand_Mountains", Base = "Club", Bundle = "ee_wand_mountains", Asset = "ee_wand_mountains",
            Word = "ee_wand_mountains", Eitr = 25f, HealPower = 45f, Bolt = 15f, RecipeBeside = "MaceSilver",
            Cost = new[] { new GearCost("FineWood", 4, 2), new GearCost("Silver", 4, 2), new GearCost("FreezeGland", 3, 1) },
        };

        /// <summary>
        /// The Plains' wand (features/casters.md, "Wands": tier 5 at the forge, heal power 60, +30 eitr; black metal, fine
        /// wood and needles; modelled from scratch, user 2026-11-02): its bolt pierce and spirit, about 40% of the black
        /// metal sword's 95.
        /// </summary>
        public static readonly GearPiece Blackmetal = new GearPiece
        {
            Prefab = "EE_Wand_Plains", Base = "Club", Bundle = "ee_wand_plains", Asset = "ee_wand_plains",
            Word = "ee_wand_plains", Eitr = 30f, HealPower = 60f, Bolt = 19f, RecipeBeside = "MaceNeedle",
            Cost = new[] { new GearCost("BlackMetal", 4, 2), new GearCost("FineWood", 4, 2), new GearCost("Needle", 5, 2) },
        };

        /// <summary>
        /// The Mistlands' wand (features/casters.md, "Wands": tier 6 at the galdr table, heal power 75, +30 eitr and +25%
        /// eitr regen; Yggdrasil wood, refined eitr and royal jelly; modelled from scratch, user 2026-11-02): its bolt pierce
        /// and spirit 22 each, a step above the Blackmetal's 19 (40% of the Mistwalker's 75 would be below it).
        /// </summary>
        public static readonly GearPiece Yggdrasil = new GearPiece
        {
            Prefab = "EE_Wand_Mistlands", Base = "Club", Bundle = "ee_wand_mistlands", Asset = "ee_wand_mistlands",
            Word = "ee_wand_mistlands", Eitr = 30f, EitrRegen = 0.25f, HealPower = 75f, Bolt = 22f, RecipeBeside = "StaffFireball",
            Cost = new[] { new GearCost("YggdrasilWood", 6, 3), new GearCost("Eitr", 4, 2), new GearCost("RoyalJelly", 3, 1) },
        };

        /// <summary>
        /// The Ashlands' wand (features/casters.md, "Wands": at the galdr table, heal power 90, +30 eitr and +40% eitr regen;
        /// blackwood, flametal and celestial feathers; user, 2026-11-02: "need ashlands wand with firebolt as main lmb"): it
        /// fires a firebolt (<see cref="Firebolt"/>), fire and spirit 27 each, 40% of the Nidhogg sword's 135.
        /// </summary>
        public static readonly GearPiece Ashwood = new GearPiece
        {
            Prefab = "EE_Wand_Ashlands", Base = "Club", Bundle = "ee_wand_ashlands", Asset = "ee_wand_ashlands",
            Word = "ee_wand_ashlands", Eitr = 30f, EitrRegen = 0.4f, HealPower = 90f, Bolt = 27f, RecipeBeside = "StaffFireball",
            Cost = new[] { new GearCost("Blackwood", 6, 3), new GearCost("FlametalNew", 3, 1), new GearCost("CelestialFeather", 2, 1) },
        };

        public static readonly IReadOnlyList<GearPiece> Wands = new[] { Wand, Greywood, Rootbound, Frostsilver, Blackmetal, Yggdrasil, Ashwood };

        /// <summary>
        /// The Meadows' spellbook (features/casters.md, "The spellbook"; it replaced the Raven Fetish on 2026-11-02, with the
        /// fetish's stats): the left hand's item whose pages are the spells a wand can cast, 2 of them (the user's tiers,
        /// 2026-11-02: 2, then 4, then 8; quality adds none). Workbench 1, beside the wood shield it copies.
        /// </summary>
        public static readonly GearPiece Book = new GearPiece
        {
            Prefab = "EE_Book_Meadows", Base = "ShieldWood", Bundle = "ee_book_meadows", Asset = "ee_book_meadows",
            Word = "ee_book_meadows", Pages = 2, Block = 2f, EitrRegen = 0.1f, HealPower = 3f, RecipeBeside = "ShieldWood",
            Cost = new[]
            {
                new GearCost("DeerHide", 3, 1), new GearCost("LeatherScraps", 4, 2), new GearCost("Resin", 2, 1),
                new GearCost("Feathers", 2, 1),
            },
        };

        /// <summary>
        /// The Swamp's spellbook (user, 2026-11-02: "one for swamp ... 4"): 4 pages, block 4, +15% eitr regen, wand heal
        /// power +6; a copy of the wood shield like the Meadows' book, at the forge beside the banded shield (level 2, as the
        /// Rootbound Wand's iron mace).
        /// </summary>
        public static readonly GearPiece Barkbound = new GearPiece
        {
            Prefab = "EE_Book_Swamp", Base = "ShieldWood", Bundle = "ee_book_swamp", Asset = "ee_book_swamp",
            Word = "ee_book_swamp", Pages = 4, Block = 4f, EitrRegen = 0.15f, HealPower = 6f, RecipeBeside = "ShieldBanded",
            Cost = new[] { new GearCost("ElderBark", 4, 2), new GearCost("Root", 3, 1), new GearCost("Iron", 2, 1) },
        };

        /// <summary>
        /// The Ashlands' spellbook (user, 2026-11-02: "another for ashlands ... 8"): 8 pages, block 8, +25% eitr regen, wand
        /// heal power +12; a copy of the wood shield, at the galdr table beside Dundr (the Ashlands' lightning staff, level 2).
        /// </summary>
        public static readonly GearPiece Cinderbound = new GearPiece
        {
            Prefab = "EE_Book_Ashlands", Base = "ShieldWood", Bundle = "ee_book_ashlands", Asset = "ee_book_ashlands",
            Word = "ee_book_ashlands", Pages = 8, Block = 8f, EitrRegen = 0.25f, HealPower = 12f, RecipeBeside = "StaffLightning",
            Cost = new[] { new GearCost("AskHide", 4, 2), new GearCost("FlametalNew", 3, 1), new GearCost("CharredBone", 4, 2) },
        };

        public static readonly IReadOnlyList<GearPiece> Books = new[] { Book, Barkbound, Cinderbound };

        /// <summary>Every robe piece of every set (<see cref="RobeSets"/>).</summary>
        public static readonly IReadOnlyList<GearPiece> Robes = RobeSets.Pieces;

        public static readonly IReadOnlyList<GearPiece> All = Wands.Concat(Books).Concat(Robes).ToArray();

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
