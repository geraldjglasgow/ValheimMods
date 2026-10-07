using System.Collections.Generic;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Stones;
using EliteCrafting.Text;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table paying for one rune use (rune-table.md section 4): the rune from the table's store first, then from
    /// the player's own inventory, and, when an essence was chosen, its cost from the table's one pool of pure essence. Made by
    /// the Inscribe tab per press; pays on the client that holds the table open (its ZDO owner).
    /// </summary>
    internal sealed class TableSupply : IRuneSupply
    {
        private readonly TableStore _store;
        private readonly Inventory _inventory;
        private readonly Essence? _essence;

        public TableSupply(TableStore store, Inventory inventory, Essence? essence)
        {
            _store = store;
            _inventory = inventory;
            _essence = essence;
        }

        public ISet<string>? Favoured => _essence?.Inscriptions;


        public int Held(string? runeId) => runeId == null ? 0 : _store.Runes(runeId) + RuneBag.Count(_inventory, runeId);

        /// <summary>Pure essence this use can pay from: the table's pool and the essence the player carries.</summary>
        public int EssenceHeld() => _store.Essence + RuneBag.CountPrefab(_inventory, EssenceItem.PrefabName);

        public StoneMessage? Shortfall(StoneJob job)
        {
            if (!_store.Writable)
            {
                return new StoneMessage("table_lost", new string[0]);
            }
            if (_essence == null)
            {
                return null;
            }
            int cost = Essences.CostFor(ItemTier.Of(job.Target));
            return EssenceHeld() >= cost ? null : new StoneMessage("not_enough_essence", new[] { cost.ToString() });
        }

        public void Pay(StoneJob job)
        {
            string? runeId = job.Def?.Id;
            if (runeId != null && job.Cost > 0)
            {
                int fromTable = System.Math.Min(_store.Runes(runeId), job.Cost);
                _store.TakeRunes(runeId, fromTable);
                RuneBag.Remove(_inventory, runeId, job.Cost - fromTable);
            }
            if (_essence != null)
            {
                int cost = Essences.CostFor(ItemTier.Of(job.Target));
                int fromTable = System.Math.Min(_store.Essence, cost);
                _store.TakeEssence(fromTable);
                RuneBag.RemovePrefab(_inventory, EssenceItem.PrefabName, cost - fromTable);
            }
        }
    }

    /// <summary>Runes (by rune id) and pure essence (by prefab) in an inventory, counted and taken wherever they lie in it.</summary>
    internal static class RuneBag
    {
        public static int Count(Inventory inventory, string runeId) => CountPrefab(inventory, StoneCatalog.PrefabFor(runeId));

        public static void Remove(Inventory inventory, string runeId, int count) =>
            RemovePrefab(inventory, StoneCatalog.PrefabFor(runeId), count);

        public static int CountPrefab(Inventory inventory, string prefab)
        {
            int count = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (ItemTier.PrefabName(item) == prefab)
                {
                    count += item.m_stack;
                }
            }
            return count;
        }

        public static void RemovePrefab(Inventory inventory, string prefab, int count)
        {
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            for (int i = items.Count - 1; i >= 0 && count > 0; i--)
            {
                ItemDrop.ItemData item = items[i];
                if (ItemTier.PrefabName(item) != prefab)
                {
                    continue;
                }
                int take = System.Math.Min(item.m_stack, count);
                inventory.RemoveItem(item, take);
                count -= take;
            }
        }
    }
}
