using System;
using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Tackle
{
    /// <summary>
    /// What a tacklebox's cells take: fishing bait, and the items named in Tackle Items (the user's choice: bait only by
    /// default). Bait is what the game's fishing rod shoots: non-equipable ammo of the rod's own ammo type, read from the
    /// rod in the item database rather than written here; before the database has the rod, any non-equipable ammo (the
    /// game's only such items are its baits).
    /// </summary>
    public static class TackleRules
    {
        private const string Rod = "FishingRod";
        private static string baitType;
        private static string parsed;
        private static HashSet<string> listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Coins and ring keys stay with the purse and the ring even when Tackle Items names them.</summary>
        public static bool IsTackle(ItemDrop.ItemData item) =>
            item != null && (IsBait(item) || (IsListed(item) && !SlotRules.IsCoins(item) && !KeyRing.IsKey(item)));

        public static bool IsBait(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.AmmoNonEquipable)
                return false;
            string bait = BaitType();
            return bait == null || item.m_shared.m_ammoType == bait;
        }

        private static string BaitType()
        {
            if (baitType == null && ObjectDB.instance != null)
            {
                GameObject rod = ObjectDB.instance.GetItemPrefab(Rod);
                ItemDrop drop = rod != null ? rod.GetComponent<ItemDrop>() : null;
                string ammo = drop != null ? drop.m_itemData.m_shared.m_ammoType : null;
                baitType = string.IsNullOrEmpty(ammo) ? null : ammo;
            }
            return baitType;
        }

        private static bool IsListed(ItemDrop.ItemData item)
        {
            string value = TackleboxSettings.TackleItems.Value ?? "";
            if (value != parsed)
                Parse(value);
            return listed.Count > 0 && listed.Contains(ItemNames.PrefabName(item));
        }

        private static void Parse(string value)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in value.Split(','))
            {
                if (part.Trim().Length > 0)
                    names.Add(part.Trim());
            }
            listed = names;
            parsed = value;
        }
    }
}
