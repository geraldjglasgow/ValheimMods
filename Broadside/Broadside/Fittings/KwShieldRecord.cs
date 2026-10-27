using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The shields hung on a railing's hooks as one ZDO text (<see cref="Key"/>): "3:&lt;item&gt;,7:&lt;item&gt;", each a
    /// hook's number (<see cref="KwShieldSlots"/>) and the shield itself, the whole item as the game saves an inventory (a
    /// one-item inventory in base 64: its prefab, durability, quality, variant, crafter, world level and custom data), so
    /// nothing about it is lost while it hangs. On the dock the text is the railing part's; on a launched ship, the ship's
    /// (every railing's hooks numbered together). A blueprint saves it with the railing as data only: nothing built from a
    /// blueprint gets it (<see cref="KwBpData"/>), so no shield is ever made twice.
    /// </summary>
    public static class KwShieldRecord
    {
        public const string Key = "kw_shields";
        public static readonly int Hash = Key.GetStableHashCode();

        /// <summary>Each hook's shield in a saved text, by hook number (an entry that does not read left out).</summary>
        public static Dictionary<int, string> Read(string? text)
        {
            Dictionary<int, string> hung = new Dictionary<int, string>();
            foreach (string entry in (text ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = entry.IndexOf(':');
                if (colon > 0 && int.TryParse(entry.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out int slot) && colon + 1 < entry.Length)
                    hung[slot] = entry.Substring(colon + 1);
            }
            return hung;
        }

        /// <summary>The saved text of hooks' shields, by hook number.</summary>
        public static string Write(Dictionary<int, string> hung) =>
            string.Join(",", hung.OrderBy(h => h.Key).Select(h => h.Key.ToString(CultureInfo.InvariantCulture) + ":" + h.Value));

        /// <summary>Whether an item can hang on a hook: a shield with a model the game's item stands can show.</summary>
        public static bool Hangs(ItemDrop.ItemData? item) =>
            item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield && item.m_dropPrefab != null && ItemStand.GetAttachPrefab(item.m_dropPrefab) != null;

        /// <summary>One of a stack of an item, unequipped, saved whole as the game saves an inventory.</summary>
        public static string Pack(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData one = item.Clone();
            (one.m_stack, one.m_equipped, one.m_gridPos) = (1, false, new Vector2i(0, 0));
            Inventory holder = new Inventory("kw_shield", null, 1, 1);
            holder.AddItem(one);
            ZPackage package = new ZPackage();
            holder.Save(package);
            return package.GetBase64();
        }

        /// <summary>The item a hook's saved shield is; null when it does not read or its prefab is not in this game.</summary>
        public static ItemDrop.ItemData? Unpack(string saved)
        {
            try
            {
                Inventory holder = new Inventory("kw_shield", null, 1, 1);
                holder.Load(new ZPackage(saved));
                ItemDrop.ItemData? item = holder.GetAllItems().FirstOrDefault();
                return item != null && item.m_dropPrefab != null ? item : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>A shield given to a player: into the inventory when there is room, else dropped at their feet.</summary>
        public static void Give(Player player, ItemDrop.ItemData item)
        {
            Inventory inventory = player.GetInventory();
            if (inventory.CanAddItem(item, 1) && inventory.AddItem(item))
            {
                player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, 1, item.GetIcon());
                return;
            }
            Transform at = player.transform;
            ItemDrop.DropItem(item, 1, at.position + at.forward * 0.6f + Vector3.up * 0.8f, at.rotation);
        }

        /// <summary>A saved shield dropped in the world (its railing or ship gone); nothing for one that does not read.</summary>
        public static void Drop(string saved, Vector3 at, Quaternion turn)
        {
            ItemDrop.ItemData? item = Unpack(saved);
            if (item != null)
                ItemDrop.DropItem(item, 1, at, turn);
        }
    }
}
