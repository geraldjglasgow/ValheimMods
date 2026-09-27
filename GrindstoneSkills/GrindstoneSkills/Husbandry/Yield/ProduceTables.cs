using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which items each creature kind produces, discovered rather than listed and cached per prefab name: the items in
    /// the kind's own CharacterDrop (the prefab's, else the creature's) that are not trophies, have no food value
    /// (m_food, m_foodStamina and m_foodEitr all 0) and are not cooking inputs (<see cref="YieldCatalog.IsCookingInput"/>),
    /// weighted by the drop's chance. Hens give feathers, boars leather scraps, wolves pelts and fangs, lox pelts;
    /// meat and trophies never. A kind with none produces nothing. Read on the creature's owner; cleared whenever
    /// <see cref="YieldDiscovery"/> runs, since cooking inputs may have grown.
    /// </summary>
    public static class ProduceTables
    {
        private static readonly Dictionary<string, ProduceTable> tables = new Dictionary<string, ProduceTable>();

        public static void Clear() => tables.Clear();

        /// <summary>The creature's kind's table, built on first use.</summary>
        public static ProduceTable For(Character creature)
        {
            string prefabName = Herd.PrefabName(creature);
            if (tables.TryGetValue(prefabName, out ProduceTable table))
                return table;
            table = Build(Drops(creature, prefabName));
            tables[prefabName] = table;
            if (!table.IsEmpty)
                GrindstoneSkills.Log.LogInfo($"Husbandry: {prefabName} produces {string.Join(", ", table.Names)}.");
            return table;
        }

        private static CharacterDrop Drops(Character creature, string prefabName)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            CharacterDrop drops = prefab != null ? prefab.GetComponent<CharacterDrop>() : null;
            return drops != null ? drops : creature.GetComponent<CharacterDrop>();
        }

        private static ProduceTable Build(CharacterDrop drops)
        {
            ProduceTable table = new ProduceTable();
            if (drops == null || drops.m_drops == null)
                return table;
            foreach (CharacterDrop.Drop drop in drops.m_drops)
            {
                if (drop != null && IsProduce(drop.m_prefab))
                    table.Add(drop.m_prefab, drop.m_chance);
            }
            return table;
        }

        private static bool IsProduce(GameObject prefab)
        {
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            ItemDrop.ItemData.SharedData shared = item != null ? item.m_itemData?.m_shared : null;
            return shared != null && shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy
                && shared.m_food == 0f && shared.m_foodStamina == 0f && shared.m_foodEitr == 0f
                && !YieldCatalog.IsCookingInput(shared);
        }
    }
}
