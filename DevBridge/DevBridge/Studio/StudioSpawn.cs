using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The Items tab's Spawn: items given to the local player as the game makes new ones (full durability, this world's
    /// level), into the inventory first and what does not fit dropped in front of the player. A list is
    /// "prefab[:amount[:level]]" entries separated by commas or new lines; an unknown prefab is skipped and named. The
    /// inventory is the player's own, so it works the same on a dedicated server.
    /// </summary>
    internal static class StudioSpawn
    {
        private const int Most = 10000;

        private struct Order
        {
            internal GameObject Prefab;
            internal int Amount;
            internal int Level;
        }

        internal static Dictionary<string, object> Give(string list)
        {
            Player player = Player.m_localPlayer;
            if (!player || !ObjectDB.instance) throw new BridgeException("no player in a world: spawned items go to your player");
            var missing = new List<string>();
            List<Order> orders = Parse(list, missing);
            if (orders.Count == 0 && missing.Count == 0) throw new BridgeException("give items=prefab[:amount[:level]],...");
            List<Dictionary<string, object>> given = orders.Select(order => Give(player, order)).ToList();
            return new Dictionary<string, object>
            {
                ["given"] = given,
                ["dropped"] = given.Sum(line => (int)line["dropped"]),
                ["missing"] = missing,
            };
        }

        private static List<Order> Parse(string list, List<string> missing)
        {
            var orders = new List<Order>();
            foreach (string entry in (list ?? "").Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Trim().Split(':');
                GameObject prefab = ObjectDB.instance.GetItemPrefab(parts[0].Trim());
                if (prefab && prefab.GetComponent<ItemDrop>()) orders.Add(new Order { Prefab = prefab, Amount = Number(parts, 1), Level = Number(parts, 2) });
                else if (parts[0].Trim().Length > 0) missing.Add(parts[0].Trim());
            }
            return orders;
        }

        private static int Number(string[] parts, int index) =>
            parts.Length > index && int.TryParse(parts[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? Mathf.Clamp(value, 1, Most) : 1;

        private static Dictionary<string, object> Give(Player player, Order order)
        {
            ItemDrop.ItemData.SharedData shared = order.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            int stack = Mathf.Max(1, shared.m_maxStackSize), level = Mathf.Clamp(order.Level, 1, Mathf.Max(1, shared.m_maxQuality)), dropped = 0;
            for (int left = order.Amount; left > 0; left -= stack) dropped += Add(player, Make(order.Prefab, Math.Min(left, stack), level));
            return new Dictionary<string, object>
            {
                ["prefab"] = order.Prefab.name,
                ["name"] = Localization.instance != null ? Localization.instance.Localize(shared.m_name) : order.Prefab.name,
                ["amount"] = order.Amount,
                ["level"] = level,
                ["dropped"] = dropped,
            };
        }

        private static ItemDrop.ItemData Make(GameObject prefab, int stack, int level)
        {
            ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = stack;
            item.m_quality = level;
            item.m_durability = item.GetMaxDurability();
            item.m_worldLevel = (byte)Game.m_worldLevel;
            return item;
        }

        // The inventory's AddItem leaves what did not fit in m_stack when it fails; that is dropped.
        private static int Add(Player player, ItemDrop.ItemData item)
        {
            if (player.GetInventory().AddItem(item)) return 0;
            Transform at = player.transform;
            ItemDrop.DropItem(item, item.m_stack, at.position + at.forward + Vector3.up, Quaternion.identity);
            return item.m_stack;
        }
    }
}
