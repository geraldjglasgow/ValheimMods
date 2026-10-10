using System;
using System.Collections.Generic;
using System.Linq;
using ItemType = ItemDrop.ItemData.ItemType;
using SkillType = Skills.SkillType;

namespace DevBridge.Studio
{
    /// <summary>
    /// The group the studio shows an item in (Shields, Swords, Helmets, Food...), from its item type and, for weapons,
    /// the skill it trains; groups come in this order, weapons first. Each group has words a search also finds it by,
    /// so "shield" finds every shield and "iron shield" the iron ones.
    /// </summary>
    internal static class ItemGroups
    {
        private static readonly (string Name, string Words)[] Order =
        {
            ("Swords", "sword"), ("Axes", "axe"), ("Maces", "mace club sledge"), ("Knives", "knife dagger"), ("Spears", "spear"),
            ("Atgeirs", "atgeir polearm"), ("Fist weapons", "fist claw unarmed"), ("Bows", "bow"), ("Crossbows", "crossbow"),
            ("Staffs", "staff magic"), ("Shields", "shield"), ("Ammo", "ammo arrow bolt"), ("Helmets", "helmet helm hood armor armour"),
            ("Chest armour", "chest armor armour"), ("Leg armour", "legs leggings armor armour"), ("Capes", "cape cloak"),
            ("Gloves", "gloves hands"), ("Utility", "utility belt"), ("Trinkets", "trinket"), ("Pickaxes", "pickaxe"),
            ("Tools", "tool"), ("Torches", "torch"), ("Food", "food"), ("Meads", "mead potion"), ("Consumables", "consumable"),
            ("Materials", "material"), ("Trophies", "trophy"), ("Fish", "fish"), ("Misc", "misc"),
        };

        private static readonly Dictionary<ItemType, string> ByType = new Dictionary<ItemType, string>
        {
            [ItemType.Shield] = "Shields", [ItemType.Helmet] = "Helmets", [ItemType.Chest] = "Chest armour",
            [ItemType.Legs] = "Leg armour", [ItemType.Shoulder] = "Capes", [ItemType.Hands] = "Gloves",
            [ItemType.Utility] = "Utility", [ItemType.Trinket] = "Trinkets", [ItemType.Ammo] = "Ammo",
            [ItemType.AmmoNonEquipable] = "Ammo", [ItemType.Torch] = "Torches", [ItemType.Trophy] = "Trophies",
            [ItemType.Fish] = "Fish", [ItemType.Material] = "Materials", [ItemType.Tool] = "Tools",
        };

        private static readonly Dictionary<SkillType, string> BySkill = new Dictionary<SkillType, string>
        {
            [SkillType.Swords] = "Swords", [SkillType.Axes] = "Axes", [SkillType.Clubs] = "Maces", [SkillType.Knives] = "Knives",
            [SkillType.Spears] = "Spears", [SkillType.Polearms] = "Atgeirs", [SkillType.Unarmed] = "Fist weapons",
            [SkillType.Bows] = "Bows", [SkillType.Crossbows] = "Crossbows", [SkillType.ElementalMagic] = "Staffs",
            [SkillType.BloodMagic] = "Staffs", [SkillType.Pickaxes] = "Pickaxes",
        };

        private static readonly ItemType[] Weapons = { ItemType.OneHandedWeapon, ItemType.TwoHandedWeapon, ItemType.TwoHandedWeaponLeft, ItemType.Bow };

        internal static string Of(ItemDrop.ItemData.SharedData shared)
        {
            if (ByType.TryGetValue(shared.m_itemType, out string group)) return group;
            if (shared.m_itemType == ItemType.Consumable) return Consumable(shared);
            if (!Weapons.Contains(shared.m_itemType)) return "Misc";
            return BySkill.TryGetValue(shared.m_skillType, out group) ? group : "Tools";
        }

        private static string Consumable(ItemDrop.ItemData.SharedData shared)
        {
            if (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f) return "Food";
            return shared.m_consumeStatusEffect ? "Meads" : "Consumables";
        }

        /// <summary>Where the group comes in the page, weapons first.</summary>
        internal static int Rank(string group) => Array.FindIndex(Order, entry => entry.Name == group);

        /// <summary>The group's name and the words a search also finds it by.</summary>
        internal static string Words(string group)
        {
            int rank = Rank(group);
            return rank < 0 ? group : group + " " + Order[rank].Words;
        }
    }
}
