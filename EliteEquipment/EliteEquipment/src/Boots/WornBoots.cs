using System.Collections.Generic;
using EliteEquipment.Core;
using UnityEngine;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The pair a character wears: the boots in its inventory that carry the game's equipped flag (saved with the
    /// inventory, the grid's equipped mark, <c>Inventory.GetEquippedItems</c>); at most one. Boots never go into the
    /// game's equipment fields, so this mod wears them (the game's <c>EquipItem</c> is answered for boots,
    /// <see cref="BootsEquipPatches"/>) and the game's own <c>UnequipItem</c> takes them off (its <c>IsItemEquiped</c>
    /// answers yes for the worn pair). The local player's answer is kept until any inventory changes or a pair comes on
    /// or off, since the stat sums ask it every physics step; anyone else's (a ragdoll, the menu's character) is looked up.
    /// </summary>
    public static class WornBoots
    {
        private static Humanoid owner;
        private static ItemDrop.ItemData pair;
        private static int seen = -1;
        private static bool stale = true;

        public static ItemDrop.ItemData Of(Humanoid humanoid)
        {
            if (humanoid == null || humanoid != Player.m_localPlayer)
                return Find(humanoid);
            if (stale || humanoid != owner || seen != InventoryChanges.Count || (pair != null && !pair.m_equipped))
            {
                owner = humanoid;
                seen = InventoryChanges.Count;
                stale = false;
                pair = Find(humanoid);
            }
            return pair;
        }

        /// <summary>A pair came on or off: the next question looks again.</summary>
        public static void Forget() => stale = true;

        /// <summary>
        /// Wears the pair with the game's equip guards, effects and refresh; the pair worn before comes off. True when it
        /// is worn afterwards. A pair already flagged (the load's <c>EquipInventoryItems</c>) stays on while the switch is
        /// off, so a client loading before the server's value arrives keeps it; the switch going off takes it off.
        /// </summary>
        public static bool Wear(Humanoid humanoid, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            bool fresh = !item.m_equipped;
            if (!CanWear(humanoid, item, fresh))
                return false;
            TakeOffOthers(humanoid, item);
            item.m_equipped = true;
            Forget();
            if (fresh)
                item.m_shared.m_equipEffect.Create(humanoid.transform.position, humanoid.transform.rotation, null, 1f, -1, humanoid.GetZDOID());
            humanoid.SetupEquipment();
            if (triggerEquipEffects)
                humanoid.TriggerEquipEffect(item);
            return true;
        }

        /// <summary>Every pair off through the game's UnequipItem (unequip all, the switch going off).</summary>
        public static void TakeOffAll(Humanoid humanoid) => TakeOffOthers(humanoid, null);

        private static void TakeOffOthers(Humanoid humanoid, ItemDrop.ItemData keep)
        {
            List<ItemDrop.ItemData> items = humanoid.GetInventory()?.GetAllItems();
            if (items == null)
                return;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = items[i];
                if (item == keep || !item.m_equipped || !BootSets.Is(item))
                    continue;
                humanoid.UnequipItem(item, triggerEquipEffects: false);
                item.m_equipped = false;
            }
            Forget();
        }

        /// <summary>The checks of the game's EquipItem that apply to armour, and the switch for a pair newly put on.</summary>
        private static bool CanWear(Humanoid humanoid, ItemDrop.ItemData item, bool fresh)
        {
            if (humanoid.GetInventory() == null || !humanoid.GetInventory().ContainsItem(item))
                return false;
            if (fresh && !BootsSettings.On)
            {
                humanoid.Message(MessageHud.MessageType.Center, "$ee_boots_off");
                return false;
            }
            if (humanoid.InAttack() || humanoid.InDodge())
                return false;
            if (humanoid.IsPlayer() && !humanoid.IsDead() && humanoid.IsSwimming() && !humanoid.IsOnGround())
                return false;
            return !item.m_shared.m_useDurability || item.m_durability > 0f;
        }

        private static ItemDrop.ItemData Find(Humanoid humanoid)
        {
            List<ItemDrop.ItemData> items = humanoid != null ? humanoid.GetInventory()?.GetAllItems() : null;
            if (items == null)
                return null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].m_equipped && BootSets.Is(items[i]))
                    return items[i];
            }
            return null;
        }
    }
}
