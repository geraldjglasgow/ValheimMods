using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// On the captain's game, what piles another peer owns have handed over and no load has used yet (<see cref="KwPileAsk"/>),
    /// by the name inventories count the item by. The broadside's loads take from it first (<see cref="KwPileStore"/>).
    /// Whatever is left once no ask is waiting (an answer that came after its load went on from the pack, or a load that
    /// went another way) goes into the local player's pack, or onto the ground at his feet when it has no room, so nothing
    /// handed over is lost. Never sent or saved.
    /// </summary>
    public static class KwPileCredit
    {
        private static readonly Dictionary<string, int> held = new Dictionary<string, int>();
        private static readonly List<string> names = new List<string>();

        /// <summary>How many of the item are held.</summary>
        public static int Count(string name) => held.Count > 0 && held.TryGetValue(name, out int count) ? count : 0;

        /// <summary>Handed over by a pile's owner.</summary>
        public static void Add(string name, int amount)
        {
            if (amount > 0)
                held[name] = Count(name) + amount;
        }

        /// <summary>Up to `amount` of the item used; how many were.</summary>
        public static int Spend(string name, int amount)
        {
            int count = Count(name), spent = Mathf.Min(count, Mathf.Max(0, amount));
            if (spent == 0)
                return 0;
            if (spent == count)
                held.Remove(name);
            else
                held[name] = count - spent;
            return spent;
        }

        /// <summary>Everything held into the local player's pack (or at his feet); kept while there is no live local player.</summary>
        public static void Flush()
        {
            Player? player = Player.m_localPlayer;
            if (held.Count == 0 || player == null || player.IsDead())
                return;
            names.Clear();
            names.AddRange(held.Keys);
            foreach (string name in names)
                Give(player, name, held[name]);
            held.Clear();
        }

        // A full stack at a time: into the pack when it has room for all of it, else dropped at his feet.
        private static void Give(Player player, string name, int amount)
        {
            GameObject? prefab = Prefab(name);
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
            {
                Log.Warn($"Broadside: {amount} {name} from an ammunition pile have no item to be given back as.");
                return;
            }
            Inventory pack = player.GetInventory();
            for (int stack; amount > 0; amount -= stack)
            {
                stack = Mathf.Min(amount, Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize));
                if (!pack.CanAddItem(prefab!, stack) || !pack.AddItem(prefab!, stack))
                    ItemDrop.DropItem(drop.m_itemData, stack, player.transform.position + Vector3.up, Quaternion.identity);
            }
        }

        // The cannon item counted by this name (only cannon items go into a pile).
        private static GameObject? Prefab(string name)
        {
            foreach (KwItemDef item in KwArmory.Items)
            {
                if (KwArmWords.SharedName(item.Name) == name)
                    return ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item.Name) : null;
            }
            return null;
        }
    }
}
