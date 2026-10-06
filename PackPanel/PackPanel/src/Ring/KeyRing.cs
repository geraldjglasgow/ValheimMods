using System.Collections.Generic;
using System;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Ring
{
    /// <summary>
    /// The keys the ring holds: the prefab names of Key Items in their order, each with its own ring cell (slot key1 holds
    /// the first key, key2 the second...). An item is a ring key by its prefab name. The ring cells are ordinary cells of
    /// the player's inventory, so a door, which checks the whole inventory for its key, finds a key on the ring.
    /// </summary>
    public static class KeyRing
    {
        private static string parsed;
        private static List<string> prefabs = new List<string>();

        /// <summary>The ring is part of the layout the settings ask for: the master switch and Key Ring on.</summary>
        public static bool On => InventorySettings.Enabled.Value && KeyRingSettings.KeyRing.Value;

        /// <summary>The layout in use has ring cells.</summary>
        public static bool Active => InventoryState.Active && InventoryState.CellsOf(SlotKind.Key).Count > 0;

        public static IReadOnlyList<string> Prefabs
        {
            get
            {
                Parse();
                return prefabs;
            }
        }

        /// <summary>The ring number (1 for the first key) of a prefab, or 0 when it is not a ring key.</summary>
        public static int NumberOf(string prefab)
        {
            IReadOnlyList<string> list = Prefabs;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], prefab, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            }
            return 0;
        }

        public static int NumberOf(ItemDrop.ItemData item) => item != null ? NumberOf(ItemNames.PrefabName(item)) : 0;

        public static bool IsKey(ItemDrop.ItemData item) => NumberOf(item) > 0;

        /// <summary>The ring cell of a key in a layout, or (-1, -1) for an item that is not a ring key or a layout without the ring.</summary>
        public static Vector2i CellOf(InventoryLayout layout, ItemDrop.ItemData item)
        {
            int number = NumberOf(item);
            IReadOnlyList<Vector2i> cells = layout != null ? layout.CellsOf(SlotKind.Key) : null;
            return number > 0 && cells != null && number <= cells.Count ? cells[number - 1] : new Vector2i(-1, -1);
        }

        /// <summary>The ring cell of a key in the layout in use.</summary>
        public static Vector2i CellOf(ItemDrop.ItemData item) => InventoryState.Active ? CellOf(InventoryState.Layout, item) : new Vector2i(-1, -1);

        /// <summary>The key of ring number <paramref name="number"/> as the item database has it, or null.</summary>
        public static ItemDrop.ItemData Key(int number)
        {
            IReadOnlyList<string> list = Prefabs;
            GameObject prefab = number >= 1 && number <= list.Count && ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(list[number - 1]) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData : null;
        }

        /// <summary>
        /// How many of each ring key the whole inventory holds, every world level together, by ring number (index 0 is
        /// unused). Each item is looked up once by its shared name (<see cref="KeyNames"/>), which needs no string made per
        /// item. Written into <paramref name="reuse"/> when it fits, so no array is made per count either.
        /// </summary>
        public static int[] Counts(Inventory inventory, int[] reuse)
        {
            Dictionary<string, List<int>> names = KeyNames.Get();
            int[] counts = reuse != null && reuse.Length == Prefabs.Count + 1 ? reuse : new int[Prefabs.Count + 1];
            Array.Clear(counts, 0, counts.Length);
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                string name = item.m_shared.m_name;
                if (name == null || !names.TryGetValue(name, out List<int> numbers))
                    continue;
                for (int n = 0; n < numbers.Count; n++)
                    counts[numbers[n]] += item.m_stack;
            }
            return counts;
        }

        private static void Parse()
        {
            string value = KeyRingSettings.KeyItems.Value ?? "";
            if (value == parsed)
                return;
            List<string> list = new List<string>();
            foreach (string part in value.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0 && !list.Exists(known => string.Equals(known, name, StringComparison.OrdinalIgnoreCase)))
                    list.Add(name);
            }
            prefabs = list;
            parsed = value;
        }
    }
}
