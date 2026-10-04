using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>
    /// Item prefabs by name: in one creature's item lists (its attacks), in the ObjectDB, or in any creature's lists.
    /// A creature's attack items (troll_punch, Greydwarf_throw) live only in its lists, not in the network scene, and
    /// the ObjectDB may not list them either.
    /// </summary>
    internal static class ItemPrefabs
    {
        internal static GameObject OfCreature(GameObject creature, string name)
        {
            Humanoid humanoid = creature.GetComponent<Humanoid>();
            if (!humanoid) throw new BridgeException($"{creature.name} carries no items (no Humanoid); for its own values give field=<Component>.<member>");
            List<GameObject> items = Carried(humanoid).ToList();
            return items.FirstOrDefault(item => Same(item.name, name))
                ?? throw new BridgeException($"{creature.name} has no item {name}; it carries {string.Join(", ", items.Select(item => item.name))}");
        }

        internal static GameObject Named(string name)
        {
            ObjectDB db = ObjectDB.instance ? ObjectDB.instance : throw new BridgeException("no world loaded");
            GameObject item = db.GetItemPrefab(name);
            if (!item) item = db.m_items.FirstOrDefault(prefab => prefab && Same(prefab.name, name));
            if (!item) item = CreatureItems().FirstOrDefault(prefab => Same(prefab.name, name));
            return item ? item : throw new BridgeException($"no item {name} in the ObjectDB or in any creature's items (search with /prefabs?filter=)");
        }

        /// <summary>Everything a creature can carry: default items, random weapons, armour, shields, sets and items, and its unarmed attack.</summary>
        internal static IEnumerable<GameObject> Carried(Humanoid humanoid)
        {
            IEnumerable<GameObject> lists = new[] { humanoid.m_defaultItems, humanoid.m_randomWeapon, humanoid.m_randomArmor, humanoid.m_randomShield }
                .Where(list => list != null).SelectMany(list => list);
            IEnumerable<GameObject> sets = (humanoid.m_randomSets ?? new Humanoid.ItemSet[0]).Where(set => set?.m_items != null).SelectMany(set => set.m_items);
            IEnumerable<GameObject> randoms = (humanoid.m_randomItems ?? new Humanoid.RandomItem[0]).Where(random => random != null).Select(random => random.m_prefab);
            GameObject unarmed = humanoid.m_unarmedWeapon ? humanoid.m_unarmedWeapon.gameObject : null;
            return lists.Concat(sets).Concat(randoms).Concat(new[] { unarmed }).Where(item => item).Distinct();
        }

        /// <summary>The item prefab's shared data, and the ObjectDB's too when that is another object of the same name.</summary>
        internal static List<object> Templates(GameObject item)
        {
            GameObject listed = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(item.name) : null;
            return new[] { item, listed }.Where(prefab => prefab).Select(prefab => prefab.GetComponent<ItemDrop>())
                .Where(drop => drop && drop.m_itemData?.m_shared != null).Select(drop => (object)drop.m_itemData.m_shared)
                .Distinct(SameObject.Instance).ToList();
        }

        private static IEnumerable<GameObject> CreatureItems()
        {
            ZNetScene scene = ZNetScene.instance ? ZNetScene.instance : throw new BridgeException("no world loaded");
            return scene.m_prefabs.Where(prefab => prefab).Select(prefab => prefab.GetComponent<Humanoid>()).Where(humanoid => humanoid).SelectMany(Carried);
        }

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
