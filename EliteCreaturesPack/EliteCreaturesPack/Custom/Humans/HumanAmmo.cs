using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// Arrows for a human's bow, bolts for its crossbow, and it never runs out. The game's attack takes one from the
    /// stack each shot and refuses to start with none, so the stack is filled as the human is armed and again after
    /// every shot. A ranged weapon given without its ammunition gets the plainest of its kind (wood arrows, bone bolts;
    /// any other kind, the first the game has). None of it is loot: a creature's inventory goes with it when it dies.
    /// </summary>
    internal static class HumanAmmo
    {
        private static readonly Dictionary<string, string> Plainest = new Dictionary<string, string>
        {
            ["$ammo_arrows"] = "ArrowWood",
            ["$ammo_bolts"] = "BoltBone",
        };

        /// <summary>As the human is armed: a full stack for every kind of ammunition its weapons take.</summary>
        public static void Stock(Humanoid human)
        {
            Inventory inventory = human.GetInventory();
            foreach (string kind in Kinds(inventory))
            {
                Fill(inventory.GetAmmoItem(kind) ?? Give(human, kind));
            }
        }

        /// <summary>After a shot: the stack it came from full again.</summary>
        public static void Restock(Humanoid human, ItemDrop.ItemData weapon)
        {
            string kind = weapon.m_shared.m_ammoType;
            if (!string.IsNullOrEmpty(kind))
            {
                Fill(human.GetInventory().GetAmmoItem(kind));
            }
        }

        private static HashSet<string> Kinds(Inventory inventory)
        {
            var kinds = new HashSet<string>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.IsWeapon() && !string.IsNullOrEmpty(item.m_shared.m_ammoType))
                {
                    kinds.Add(item.m_shared.m_ammoType);
                }
            }
            return kinds;
        }

        private static void Fill(ItemDrop.ItemData? ammo)
        {
            if (ammo != null)
            {
                ammo.m_stack = Mathf.Max(ammo.m_stack, ammo.m_shared.m_maxStackSize);
            }
        }

        private static ItemDrop.ItemData? Give(Humanoid human, string kind)
        {
            GameObject? prefab = Find(kind);
            return prefab != null ? human.PickupPrefab(prefab, 0, autoequip: false) : null;
        }

        private static GameObject? Find(string kind)
        {
            ObjectDB? db = ObjectDB.instance;
            if (db == null)
            {
                return null;
            }
            GameObject? plainest = Plainest.TryGetValue(kind, out string name) ? db.GetItemPrefab(name) : null;
            return plainest != null ? plainest : FirstOfKind(db, kind);
        }

        private static GameObject? FirstOfKind(ObjectDB db, string kind)
        {
            foreach (GameObject item in db.m_items)
            {
                ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
                ItemDrop.ItemData.ItemType type = drop != null ? drop.m_itemData.m_shared.m_itemType : ItemDrop.ItemData.ItemType.None;
                bool ammo = type == ItemDrop.ItemData.ItemType.Ammo || type == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
                if (ammo && drop!.m_itemData.m_shared.m_ammoType == kind)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
