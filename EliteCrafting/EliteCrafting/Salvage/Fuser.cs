using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Stones;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// Fusing (salvage.md section 6): a shard stack in the player's own inventory with at least its fuse count becomes
    /// one stone of its target kind (Shift: every full set that fits, SAL-10). Instant, no confirm: nothing is lost.
    /// Works whether salvage is switched on or not - owned shards stay the players' - but never into a disabled or
    /// undefined stone (SAL-11, IMP-106). Local client, own inventory, synced fuse counts; no RPC.
    /// </summary>
    internal static class Fuser
    {
        /// <summary>One of the five shard prefabs (a registry lookup; anything else returns at once).</summary>
        public static bool IsShard(ItemDrop.ItemData item) => StonePrefabs.EntryOf(item.m_dropPrefab)?.IsShard == true;

        public static void Fuse(Player player, Inventory inventory, ItemDrop.ItemData shards, bool all)
        {
            RuleSet rules = ActiveRules.Current;
            FragmentDef? fragment = rules.Economy.Salvage.FragmentForPrefab(ItemTier.PrefabName(shards));
            StoneDef? stone = fragment == null ? null : rules.Stone(fragment.Stone);
            GameObject? prefab = stone != null && stone.Enabled ? StonePrefabs.Get(stone.Id) : null;
            if (fragment == null || stone == null || prefab == null)
            {
                string name = Words.Localize(stone?.Name ?? shards.m_shared.m_name);
                StoneFeedback.Show(player, new StoneMessage("stone_disabled", new[] { name }));
                return;
            }
            Place(player, inventory, shards, new FuseTarget(fragment, stone, prefab), all);
        }

        private static void Place(Player player, Inventory inventory, ItemDrop.ItemData shards, FuseTarget target, bool all)
        {
            int fuse = target.Fragment.Fuse;
            if (shards.m_stack < fuse || !inventory.ContainsItem(shards))
            {
                Show(player, "fuse_short", fuse.ToString(), target.ShardName(shards), target.StoneName);
                return;
            }
            int wanted = all ? shards.m_stack / fuse : 1;
            int sets = ShardRoom.FittingSets(wanted, shards.m_stack, fuse, ShardRoom.PartialRoom(inventory, target.Prefab),
                inventory.GetEmptySlots(), ShardRoom.MaxStack(target.Prefab));
            if (sets == 0)
            {
                Show(player, "fuse_no_room", target.StoneName);
                return;
            }
            string shardName = target.ShardName(shards);
            inventory.RemoveItem(shards, sets * fuse);
            ShardRoom.Add(inventory, target.Prefab, sets);
            Show(player, sets == 1 ? "fused" : "fused_many", (sets * fuse).ToString(), shardName, target.StoneName, sets.ToString());
            GrindText.PlaySound();
        }

        private static void Show(Player player, string id, params string[] words) =>
            StoneFeedback.Show(player, new StoneMessage(id, words));

        private sealed class FuseTarget
        {
            public FuseTarget(FragmentDef fragment, StoneDef stone, GameObject prefab)
            {
                Fragment = fragment;
                Prefab = prefab;
                StoneName = Words.Localize(stone.Name);
            }

            public FragmentDef Fragment { get; }
            public GameObject Prefab { get; }
            public string StoneName { get; }

            public string ShardName(ItemDrop.ItemData shards) => Words.Localize(shards.m_shared.m_name);
        }
    }
}
