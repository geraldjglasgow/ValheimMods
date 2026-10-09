using System;
using System.Collections.Generic;
using PackPanel.Backpacks;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Tackle;

using ItemType = ItemDrop.ItemData.ItemType;

namespace PackPanel.Slots
{
    /// <summary>
    /// Which items each slot takes. Worn slots take the game's item types for them (helmet, chest, legs, shoulder,
    /// utility, trinket; Feet EliteEquipment's boots, by prefab name); food is a consumable that fills a food bar, a mead any
    /// other consumable; ammo includes bait and non-equipable ammo; the purse takes coins only; a ring cell only its own key (<see cref="KeyRing"/>); the
    /// Tacklebox slot PackPanel's tackleboxes and a box's cell bait (<see cref="TackleRules"/>). The backpack slot takes
    /// PackPanel's backpacks (<see cref="BackpackCatalog"/>), an item whose prefab name contains "backpack", or one listed
    /// in Backpack Items: the game has none, other mods do. It is worn for PackPanel's own packs, which are equipment
    /// (<see cref="BackpackEquip"/>): Utility items whose worn slot is the Backpack slot, never a Utility slot.
    /// </summary>
    public static class SlotRules
    {
        public const string CoinsName = "$item_coins";

        public static bool IsWorn(SlotKind kind) => kind <= SlotKind.Utility || kind == SlotKind.Trinket || kind == SlotKind.Feet;

        /// <summary>Whether a slot takes an item: its kind's rule, and for a ring cell the one key of that cell.</summary>
        public static bool Accepts(Slot slot, ItemDrop.ItemData item) =>
            slot.Kind == SlotKind.Key ? item == null || KeyRing.NumberOf(item) == slot.Number : Accepts(slot.Kind, item);

        public static bool Accepts(SlotKind kind, ItemDrop.ItemData item)
        {
            if (item == null)
                return true;
            switch (kind)
            {
                case SlotKind.Backpack: return IsBackpack(item);
                case SlotKind.Food: return IsFood(item);
                case SlotKind.Mead: return Is(item, ItemType.Consumable) && !IsFood(item);
                case SlotKind.Ammo: return Is(item, ItemType.Ammo) || Is(item, ItemType.AmmoNonEquipable);
                case SlotKind.Purse: return IsCoins(item);
                case SlotKind.Key: return KeyRing.IsKey(item);
                case SlotKind.Tacklebox: return TackleboxCatalog.Of(item) != null;
                case SlotKind.Tackle: return TackleRules.IsTackle(item);
                case SlotKind.Retired: return false;
                default: return WornKindOf(item) == kind;
            }
        }

        /// <summary>
        /// The worn slot kind of an item, or null when no worn slot takes it. EliteEquipment's boots are Legs items, so they are
        /// told by their prefab name first: boots go to Feet only, never to Legs, and leggings never to Feet.
        /// </summary>
        public static SlotKind? WornKindOf(ItemDrop.ItemData item)
        {
            if (BackpackCatalog.Of(item) != null)
                return SlotKind.Backpack;
            if (EliteEquipmentLink.IsBoots(item))
                return SlotKind.Feet;
            switch (item.m_shared.m_itemType)
            {
                case ItemType.Helmet: return SlotKind.Head;
                case ItemType.Chest: return SlotKind.Chest;
                case ItemType.Legs: return SlotKind.Legs;
                case ItemType.Shoulder: return SlotKind.Back;
                case ItemType.Utility: return SlotKind.Utility;
                case ItemType.Trinket: return SlotKind.Trinket;
                default: return null;
            }
        }

        public static bool IsCoins(ItemDrop.ItemData item) => item.m_shared.m_name == CoinsName;

        public static bool IsFood(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            return shared.m_itemType == ItemType.Consumable && (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f);
        }

        public static bool IsBackpack(ItemDrop.ItemData item)
        {
            if (BackpackCatalog.Of(item) != null)
                return true;   // PackPanel's own packs: not all have "backpack" in their prefab name (PackPanel_LoxHauler)
            string prefab = item.m_dropPrefab != null ? ItemNames.PrefabName(item) : "";
            if (prefab.IndexOf("backpack", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return ListedPacks().Contains(prefab);
        }

        private static HashSet<string> listedPacks;
        private static bool watchingList;

        /// <summary>Backpack Items as a set of prefab names, read from the setting once and again after it changed.</summary>
        private static HashSet<string> ListedPacks()
        {
            if (listedPacks != null)
                return listedPacks;
            if (!watchingList)
            {
                watchingList = true;
                InventorySettings.BackpackItems.SettingChanged += (sender, args) => listedPacks = null;
            }
            listedPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string listed in InventorySettings.BackpackItems.Value.Split(','))
            {
                if (listed.Trim().Length > 0)
                    listedPacks.Add(listed.Trim());
            }
            return listedPacks;
        }

        private static bool Is(ItemDrop.ItemData item, ItemType type) => item.m_shared.m_itemType == type;
    }
}
