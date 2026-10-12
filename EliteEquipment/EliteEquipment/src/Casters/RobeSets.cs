using System.Collections.Generic;
using System.Linq;

using HairType = ItemDrop.ItemData.HelmetHairType;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The seven Seer's robe sets (features/casters.md, "Armour: the Seer's robes"), each its own design in the workshop
    /// (user, 2026-10-11: each set "match color themes of materials used to craft them, but also be somewhat distinctly
    /// different"): Deerhide and Mossweave hoods, a bark witch hat, a fur hat, a sun hat, a chitin circlet, a flametal
    /// crown. Recipes sit beside the game's armour of the same biome (its station and level). Max eitr per piece follows the
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

        public static readonly IReadOnlyList<RobeSet> All = new[] { Meadows, BlackForest, Swamp, Mountains, Plains, Mistlands, Ashlands };

        public static readonly IReadOnlyList<GearPiece> Pieces = All.SelectMany(set => set.Pieces).ToArray();

        private static GearCost C(string item, int amount, int perLevel) => new GearCost(item, amount, perLevel);

        private static RobeSet Deerhide()
        {
            var set = new RobeSet { Tier = "meadows", Title = "Meadows", Name = "Deerhide Vestments", BootsName = "Deerhide Foot Wraps",
                BonusText = "Mend costs 25% less eitr.", Hair = HairType.HiddenHood, Armour = 1f, Movement = 0f, Eitr = 5f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetLeather", "Deerhide Hood", "A seer's deep hood of soft deer hide, draped over the shoulders.",
                C("DeerHide", 2, 1), C("LeatherScraps", 2, 1), C("Dandelion", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorLeatherChest", "Deerhide Robe", "A seer's long robe of deer hide, split for striding, runes stitched at the hem.",
                C("DeerHide", 4, 2), C("LeatherScraps", 3, 1), C("Dandelion", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorLeatherLegs", "Deerhide Trousers", "Deer-hide trousers ending in wrapped feet.",
                C("DeerHide", 3, 1), C("LeatherScraps", 3, 1), C("Dandelion", 2, 1));
            return set;
        }

        private static RobeSet Mossweave()
        {
            var set = new RobeSet { Tier = "blackforest", Title = "BlackForest", Name = "Mossweave", BootsName = "Mossweave Foot Wraps",
                BonusText = "Chants last 50% longer.", Hair = HairType.HiddenHood, Armour = 4f, Movement = -0.02f, Eitr = 8f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetTrollLeather", "Mossweave Hood", "A troll-hide hood with a crest of lichen.",
                C("TrollHide", 2, 1), C("Thistle", 3, 1), C("GreydwarfEye", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorTrollLeatherChest", "Mossweave Robe", "A slate troll-hide robe under a mantle of moss, thistle runes on every leaf of its hem.",
                C("TrollHide", 4, 2), C("Thistle", 4, 2), C("GreydwarfEye", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorTrollLeatherLegs", "Mossweave Trousers", "Troll-hide trousers ending in wrapped feet.",
                C("TrollHide", 3, 1), C("Thistle", 3, 1), C("GreydwarfEye", 2, 1));
            return set;
        }

        private static RobeSet Rootweave()
        {
            var set = new RobeSet { Tier = "swamp", Title = "Swamp", Name = "Rootweave", BootsName = "Rootweave Foot Wraps",
                BonusText = "Poison resistant.", Hair = HairType.HiddenHat, Armour = 7f, Movement = -0.02f, Eitr = 10f,
                Bonus = RobeSet.Resist(HitData.DamageType.Poison) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetRoot", "Rootweave Hat", "A crooked witch's hat woven of bark and roots, a blood-red cord at its band.",
                C("Root", 2, 1), C("ElderBark", 4, 2), C("Bloodbag", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorRootChest", "Rootweave Tunic", "A knee-length bark tunic, its shoulders plated with ancient bark and girded with woven roots.",
                C("Root", 4, 2), C("ElderBark", 6, 3), C("Bloodbag", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorRootLegs", "Rootweave Trousers", "Trousers wound with roots, ending in wrapped feet.",
                C("Root", 3, 1), C("ElderBark", 5, 2), C("Bloodbag", 2, 1));
            return set;
        }

        private static RobeSet Wolfwool()
        {
            var set = new RobeSet { Tier = "mountains", Title = "Mountains", Name = "Wolfwool", BootsName = "Wolfwool Boots",
                BonusText = "Frost resistant.", Hair = HairType.HiddenScarf, Armour = 10f, Movement = -0.02f, Eitr = 12f,
                Bonus = RobeSet.Resist(HitData.DamageType.Frost) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetDrake", "Wolfwool Hat", "A round hat of wolf fur over felt, studded with silver runes.",
                C("WolfPelt", 2, 1), C("Silver", 2, 1), C("WolfFang", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorWolfChest", "Wolfwool Coat", "A heavy wool coat with a broad collar of wolf pelt, silver clasps and fang charms.",
                C("WolfPelt", 5, 2), C("Silver", 4, 2), C("WolfFang", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorWolfLegs", "Wolfwool Trousers", "Dark wool trousers and fur-cuffed boots.",
                C("WolfPelt", 4, 2), C("Silver", 3, 1), C("WolfFang", 2, 1));
            return set;
        }

        private static RobeSet Sunweave()
        {
            var set = new RobeSet { Tier = "plains", Title = "Plains", Name = "Sunweave", BootsName = "Sunweave Boots",
                BonusText = "Your heals also restore 10 stamina.", Hair = HairType.HiddenHat, Armour = 13f, Movement = -0.02f, Eitr = 15f };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetPadded", "Sunweave Hat", "A broad-brimmed linen hat with a sun-gold band and a black-metal sun disc.",
                C("LinenThread", 10, 5), C("LoxPelt", 1, 1), C("BlackMetal", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorPaddedCuirass", "Sunweave Robe", "Layered linen robes with sun-gold borders and a lox pelt over one shoulder.",
                C("LinenThread", 20, 10), C("LoxPelt", 2, 1), C("BlackMetal", 4, 2));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorPaddedGreaves", "Sunweave Trousers", "Lox-leather trousers and fur-cuffed boots.",
                C("LinenThread", 15, 5), C("LoxPelt", 2, 1), C("BlackMetal", 3, 1));
            return set;
        }

        private static RobeSet Mistweave()
        {
            var set = new RobeSet { Tier = "mistlands", Title = "Mistlands", Name = "Mistweave", BootsName = "Mistweave Boots",
                BonusText = "Blood Magic +15.", Hair = HairType.HiddenHood, Armour = 16f, Movement = -0.02f, Eitr = 15f,
                Bonus = effect => RobeSet.BloodMagic(effect, 15f) };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetMage", "Mistweave Circlet", "A low circlet of chitin plates set with a pale eitr stone.",
                C("LinenThread", 10, 5), C("MushroomJotunPuffs", 5, 2), C("Carapace", 6, 3));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorMageChest", "Mistweave Robe", "A mist-dyed robe with a fan collar, carapace shoulders and a plated girdle.",
                C("LinenThread", 20, 10), C("MushroomJotunPuffs", 10, 5), C("Carapace", 10, 5));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorMageLegs", "Mistweave Trousers", "Indigo trousers and carapace-capped boots.",
                C("LinenThread", 15, 5), C("MushroomJotunPuffs", 8, 3), C("Carapace", 8, 4));
            return set;
        }

        private static RobeSet Emberweave()
        {
            var set = new RobeSet { Tier = "ashlands", Title = "Ashlands", Name = "Emberweave", BootsName = "Emberweave Boots",
                BonusText = "Fire resistant, Blood Magic +20.", Hair = HairType.HiddenScarf, Armour = 19f, Movement = -0.02f, Eitr = 15f,
                Bonus = effect => { RobeSet.Resist(HitData.DamageType.Fire)(effect); RobeSet.BloodMagic(effect, 20f); } };
            set.Head = set.Piece("hood", "HelmetMage", "HelmetMage_Ashlands", "Emberweave Crown", "A tall crown of flametal plates with a fan of celestial feathers.",
                C("AskHide", 4, 2), C("FlametalNew", 4, 2), C("CelestialFeather", 2, 1));
            set.Robe = set.Piece("robe", "ArmorMageChest", "ArmorMageChest_Ashlands", "Emberweave Robe", "A floor-length robe of scaled asksvin hide under a collar of celestial feathers.",
                C("AskHide", 8, 4), C("FlametalNew", 6, 3), C("CelestialFeather", 3, 1));
            set.Trousers = set.Piece("trousers", "ArmorLeatherLegs", "ArmorMageLegs_Ashlands", "Emberweave Trousers", "Scaled greaves and flametal-shod boots.",
                C("AskHide", 6, 3), C("FlametalNew", 5, 2), C("CelestialFeather", 2, 1));
            return set;
        }
    }
}
