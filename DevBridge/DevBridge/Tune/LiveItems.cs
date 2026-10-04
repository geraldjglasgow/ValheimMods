using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>
    /// Every loaded copy of an item's shared data: world drops, the inventories of loaded creatures and players (and the
    /// players' eaten foods), and loaded containers. The game gives an item made from its prefab (a creature's attack, an
    /// item loaded from a save, a world drop) its own copy of the shared data, while a stack split shares it, so the
    /// same object can turn up more than once; the caller writes each object once. A copy is the item's when its drop
    /// prefab has the item's name. A creature's attack item the ObjectDB does not list has no drop prefab: it counts
    /// when its carrier's own lists hold the item and its shared name matches.
    /// </summary>
    internal static class LiveItems
    {
        internal static List<TuneRoot> Copies(GameObject prefab)
        {
            ItemDrop own = prefab.GetComponent<ItemDrop>();
            string token = own ? own.m_itemData.m_shared.m_name : null;
            var found = new List<TuneRoot>();
            Add(found, ItemDrop.s_instances.Where(drop => drop).Select(drop => drop.m_itemData), prefab, token, false, "world");
            foreach (Character character in Character.GetAllCharacters())
                if (character is Humanoid humanoid && humanoid) Carried(found, humanoid, prefab, token);
            foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
                Add(found, container.GetInventory()?.GetAllItems(), prefab, token, false, "container " + Utils.GetPrefabName(container.gameObject));
            return found;
        }

        private static void Carried(List<TuneRoot> found, Humanoid humanoid, GameObject prefab, string token)
        {
            // A copy without a drop prefab is known only by its shared name, which a creature's attacks often share (the
            // Troll's punch and slam are both "slap"): match by name only when no other carried item has it.
            List<GameObject> carried = ItemPrefabs.Carried(humanoid).ToList();
            bool listed = carried.Any(item => item == prefab || item.name == prefab.name)
                && carried.Count(item => item.GetComponent<ItemDrop>() is ItemDrop drop && drop.m_itemData.m_shared.m_name == token) == 1;
            Player player = humanoid as Player;
            string where = player ? "player " + player.GetPlayerName() : Utils.GetPrefabName(humanoid.gameObject);
            Add(found, humanoid.GetInventory().GetAllItems(), prefab, token, listed, where);
            if (player) Add(found, player.m_foods.Select(food => food.m_item), prefab, token, false, where + " food");
        }

        private static void Add(List<TuneRoot> found, IEnumerable<ItemDrop.ItemData> items, GameObject prefab, string token, bool listed, string where)
        {
            if (items == null) return;
            foreach (ItemDrop.ItemData item in items)
                if (IsCopy(item, prefab, token, listed)) found.Add(new TuneRoot(item.m_shared, where));
        }

        private static bool IsCopy(ItemDrop.ItemData item, GameObject prefab, string token, bool listed)
        {
            if (item?.m_shared == null) return false;
            if (item.m_dropPrefab) return item.m_dropPrefab.name == prefab.name;
            return listed && item.m_shared.m_name == token;
        }
    }
}
