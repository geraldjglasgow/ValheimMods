using System.Collections.Generic;
using System.Linq;

using HairType = ItemDrop.ItemData.HelmetHairType;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The eight Seer's robe sets (features/casters.md, "Armour: the Seer's robes"), each its own design in the workshop
    /// (user, 2026-10-11: each set "match color themes of materials used to craft them, but also be somewhat distinctly
    /// different"): a Deerhide hood, a Mossweave branch headdress (user, 2026-11-01: not a hood like the Meadows'), a bark
    /// witch hat, a Wolfwool wizard hat (user, 2026-11-02: it was a round fur hat), a sun hat, a chitin circlet, an Emberweave wizard hat
    /// (user, 2026-11-02: after a flametal crown and a skull headdress, "a wizard hat like headpiece, that looks cool"; the
    /// set black, grey and dark red, no white), and the Deep
    /// North's Rimegild cobalt-and-white wizard hat (user, 2026-11-02: "a brand new set created for the deep north"; the
    /// user's palette: ice-white robe, cobalt side panels, pearl breastplate, plum leather, silver tabard). Recipes sit
    /// beside the game's armour of the same biome (its station and level). Max eitr per piece follows the
    /// plan's table until the user decides between max eitr and eitr regen.
    /// </summary>
    public static class RobeSets
    {
        public static readonly RobeSet Meadows = Deerhide();
        public static readonly RobeSet BlackForest = Mossweave();
        public static readonly RobeSet Swamp = Rootweave();
        public static readonly RobeSet Mountains = Wolfwool();
        public static readonly RobeSet Plains = Sunweave();
        public static readonly RobeSet Mistlands = Mistweave();
        public static readonly RobeSet Ashlands = Emberweave();
        public static readonly RobeSet DeepNorth = Rimegild();

        public static readonly IReadOnlyList<RobeSet> All =
            new[] { Meadows, BlackForest, Swamp, Mountains, Plains, Mistlands, Ashlands, DeepNorth };

        public static readonly IReadOnlyList<GearPiece> Pieces = All.SelectMany(set => set.Pieces).ToArray();

        private static GearCost C(string item, int amount, int perLevel) => new GearCost(item, amount, perLevel);

        private static RobeSet Deerhide()
        {
            var set = new RobeSet { Tier = "meadows", Title = "Meadows",
                Hair = HairType.HiddenHood, Armour = 1f, Movement = 0f, Eitr = 5f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetLeather",
                C("DeerHide", 2, 1), C("LeatherScraps", 2, 1), C("Dandelion", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorLeatherChest",
                C("DeerHide", 4, 2), C("LeatherScraps", 3, 1), C("Dandelion", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorLeatherLegs",
                C("DeerHide", 3, 1), C("LeatherScraps", 3, 1), C("Dandelion", 2, 1));
            return set;
        }

        private static RobeSet Mossweave()
        {
            var set = new RobeSet { Tier = "blackforest", Title = "BlackForest",
                Hair = HairType.HiddenHat, Armour = 4f, Movement = -0.02f, Eitr = 8f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetTrollLeather",
                C("TrollHide", 2, 1), C("Thistle", 3, 1), C("GreydwarfEye", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorTrollLeatherChest",
                C("TrollHide", 4, 2), C("Thistle", 4, 2), C("GreydwarfEye", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorTrollLeatherLegs",
                C("TrollHide", 3, 1), C("Thistle", 3, 1), C("GreydwarfEye", 2, 1));
            return set;
        }

        private static RobeSet Rootweave()
        {
            var set = new RobeSet { Tier = "swamp", Title = "Swamp",
                Hair = HairType.HiddenHat, Armour = 7f, Movement = -0.02f, Eitr = 10f,
                Bonus = RobeSet.Resist(HitData.DamageType.Poison) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetRoot",
                C("Root", 2, 1), C("ElderBark", 4, 2), C("Bloodbag", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorRootChest",
                C("Root", 4, 2), C("ElderBark", 6, 3), C("Bloodbag", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorRootLegs",
                C("Root", 3, 1), C("ElderBark", 5, 2), C("Bloodbag", 2, 1));
            return set;
        }

        private static RobeSet Wolfwool()
        {
            var set = new RobeSet { Tier = "mountains", Title = "Mountains",
                Hair = HairType.HiddenHat, Armour = 10f, Movement = -0.02f, Eitr = 12f,
                Bonus = RobeSet.Resist(HitData.DamageType.Frost) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetDrake",
                C("WolfPelt", 2, 1), C("Silver", 2, 1), C("WolfFang", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorWolfChest",
                C("WolfPelt", 5, 2), C("Silver", 4, 2), C("WolfFang", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorWolfLegs",
                C("WolfPelt", 4, 2), C("Silver", 3, 1), C("WolfFang", 2, 1));
            return set;
        }

        private static RobeSet Sunweave()
        {
            var set = new RobeSet { Tier = "plains", Title = "Plains",
                Hair = HairType.HiddenHat, Armour = 13f, Movement = -0.02f, Eitr = 15f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetPadded",
                C("LinenThread", 10, 5), C("LoxPelt", 1, 1), C("BlackMetal", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorPaddedCuirass",
                C("LinenThread", 20, 10), C("LoxPelt", 2, 1), C("BlackMetal", 4, 2));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorPaddedGreaves",
                C("LinenThread", 15, 5), C("LoxPelt", 2, 1), C("BlackMetal", 3, 1));
            return set;
        }

        private static RobeSet Mistweave()
        {
            var set = new RobeSet { Tier = "mistlands", Title = "Mistlands",
                Hair = HairType.HiddenHood, Armour = 16f, Movement = -0.02f, Eitr = 15f,
                Bonus = effect => RobeSet.BloodMagic(effect, 15f) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetMage",
                C("LinenThread", 10, 5), C("MushroomJotunPuffs", 5, 2), C("Carapace", 6, 3));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorMageChest",
                C("LinenThread", 20, 10), C("MushroomJotunPuffs", 10, 5), C("Carapace", 10, 5));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorMageLegs",
                C("LinenThread", 15, 5), C("MushroomJotunPuffs", 8, 3), C("Carapace", 8, 4));
            return set;
        }

        private static RobeSet Emberweave()
        {
            var set = new RobeSet { Tier = "ashlands", Title = "Ashlands",
                // HiddenScarf, not HiddenHat: HiddenHat's long hairs go through the tall collar
                Hair = HairType.HiddenScarf, Armour = 19f, Movement = -0.02f, Eitr = 15f,
                Bonus = effect => { RobeSet.Resist(HitData.DamageType.Fire)(effect); RobeSet.BloodMagic(effect, 20f); } };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetMage_Ashlands",
                C("AskHide", 4, 2), C("FlametalNew", 4, 2), C("CelestialFeather", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorMageChest_Ashlands",
                C("AskHide", 8, 4), C("FlametalNew", 6, 3), C("CelestialFeather", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorMageLegs_Ashlands",
                C("AskHide", 6, 3), C("FlametalNew", 5, 2), C("CelestialFeather", 2, 1));
            return set;
        }

        /// <summary>
        /// Tier 8, a step above the Emberweave: half the Deep North's heavy armour (44), frost resistant (the cold biome)
        /// and Blood Magic +25; beside the game's Deep North mage set (the Caller's) with the Deep North's Norn thread,
        /// seal hide and moose sinew.
        /// </summary>
        private static RobeSet Rimegild()
        {
            var set = new RobeSet { Tier = "deepnorth", Title = "DeepNorth",
                Hair = HairType.HiddenHat, Armour = 22f, Movement = -0.02f, Eitr = 15f,
                Bonus = effect => { RobeSet.Resist(HitData.DamageType.Frost)(effect); RobeSet.BloodMagic(effect, 25f); } };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetDNMage",
                C("NornThread", 6, 3), C("SealHide", 4, 2), C("MooseSinew", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorDeepNorthMageChest",
                C("NornThread", 12, 6), C("SealHide", 6, 3), C("MooseSinew", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorDeepNorthMagelegs",
                C("NornThread", 9, 4), C("SealHide", 5, 2), C("MooseSinew", 2, 1));
            return set;
        }
    }
}
