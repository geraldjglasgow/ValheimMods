using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// Runes and pure essence going into a Rune Table and out of it again (rune-table.md section 5). In: Store all moves
    /// every rune and every pure essence the player carries into the table (the Inscribe tab's small button). Out: there
    /// is no way to take them back by hand; when the table is destroyed or taken down with the hammer, its ZDO owner drops
    /// every rune it holds and its pool as pure essence items where it stood, as a chest drops its contents.
    /// </summary>
    internal static class RuneStash
    {
        /// <summary>Moves every rune and pure essence in the inventory into the table; the count moved (0 without ownership).</summary>
        public static int StoreAll(TableStore store, Inventory inventory)
        {
            if (!store.Writable)
            {
                return 0;
            }
            int stored = 0;
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                int carried = RuneBag.Count(inventory, id);
                RuneBag.Remove(inventory, id, carried);
                store.AddRunes(id, carried);
                stored += carried;
            }
            int essence = RuneBag.CountPrefab(inventory, EssenceItem.PrefabName);
            RuneBag.RemovePrefab(inventory, EssenceItem.PrefabName, essence);
            store.AddEssence(essence);
            return stored + essence;
        }

        public static int Carried(Inventory inventory)
        {
            int count = 0;
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                count += RuneBag.Count(inventory, id);
            }
            return count + RuneBag.CountPrefab(inventory, EssenceItem.PrefabName);
        }

        /// <summary>On the table's owner as it is destroyed: every rune it holds dropped beside it, in full stacks.</summary>
        public static void DropAll(TableStore store, Vector3 at)
        {
            if (!store.Writable)
            {
                return;
            }
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                int held = store.Runes(id);
                if (held > 0 && store.TakeRunes(id, held))
                {
                    Drop(StonePrefabs.Get(id), held, at);
                }
            }
            int essence = store.Essence;
            if (essence > 0 && store.TakeEssence(essence))
            {
                Drop(EssenceItem.Prefab, essence, at);
            }
        }

        private static void Drop(GameObject? prefab, int count, Vector3 at)
        {
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
            {
                return;
            }
            int max = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            for (int left = count; left > 0; left -= max)
            {
                ItemDrop.ItemData item = drop.m_itemData.Clone();
                item.m_dropPrefab = prefab;
                item.m_stack = Mathf.Min(left, max);
                item.m_worldLevel = (byte)Game.m_worldLevel;
                Vector3 position = at + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
                ItemDrop.DropItem(item, 0, position, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            }
        }
    }
}
