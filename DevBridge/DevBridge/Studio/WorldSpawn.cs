using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The Spawning tab's Spawn: copies of a prefab put in the world in front of the player as the game's spawn command
    /// makes them (marked cheated, items at full durability), facing the player: creatures and anything else that falls
    /// a little above the ground, the rest on the ground or the floor there. Creatures take a level (stars + 1) and can be
    /// tamed; items take a stack and an item level, and the amount is spread over stacks. The copies are networked objects
    /// this player owns, so everyone sees them, on a dedicated server too. Undo removes the last spawn's copies still in
    /// the world, loaded or not (picked up, broken or killed ones are gone already).
    /// </summary>
    internal static class WorldSpawn
    {
        private const int MostCopies = 50, MostBatches = 20;

        private static readonly List<List<ZDOID>> Batches = new List<List<ZDOID>>();

        internal struct Order
        {
            internal string Prefab;
            internal int Amount;
            internal int Level;
            internal bool Tamed;
            internal float Distance;
        }

        internal static Dictionary<string, object> Spawn(Order order)
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no player in a world: spawns go in front of your player");
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(order.Prefab) : null;
            if (!prefab) throw new BridgeException($"the game has no networked prefab {order.Prefab}");
            ItemDrop item = prefab.GetComponent<ItemDrop>();
            List<int> stacks = Stacks(item, Mathf.Clamp(order.Amount, 1, item ? 10000 : MostCopies));
            var batch = stacks.Select((stack, i) => Place(player, prefab, order, stack, i, stacks.Count))
                .Where(view => view && view.IsValid()).Select(view => view.GetZDO().m_uid).ToList();
            Remember(batch);
            return new Dictionary<string, object>
            {
                ["prefab"] = prefab.name, ["copies"] = batch.Count, ["amount"] = item ? stacks.Sum() : batch.Count, ["undo"] = Batches.Count,
            };
        }

        /// <summary>Removes the copies of the last spawn still in the world, claiming each first (another peer may own it now).</summary>
        internal static Dictionary<string, object> Undo()
        {
            if (Batches.Count == 0) throw new BridgeException("nothing spawned to take back");
            if (!ZNetScene.instance || ZDOMan.instance == null) throw new BridgeException("no world loaded");
            List<ZDOID> batch = Batches[Batches.Count - 1];
            Batches.RemoveAt(Batches.Count - 1);
            int removed = batch.Count(Remove);
            return new Dictionary<string, object> { ["removed"] = removed, ["undo"] = Batches.Count };
        }

        // A loaded copy goes the game's way; an unloaded one by its record, which every peer then drops.
        private static bool Remove(ZDOID id)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null) return false;
            GameObject loaded = ZNetScene.instance.FindInstance(id);
            zdo.SetOwner(ZDOMan.GetSessionID());
            if (loaded) ZNetScene.instance.Destroy(loaded);
            else ZDOMan.instance.DestroyZDO(zdo);
            return true;
        }

        // Items: the amount in full stacks; anything else: one copy each.
        private static List<int> Stacks(ItemDrop item, int amount)
        {
            if (!item) return Enumerable.Repeat(1, amount).ToList();
            int full = Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize);
            var stacks = new List<int>();
            for (int left = amount; left > 0; left -= full) stacks.Add(Math.Min(left, full));
            return stacks.Take(MostCopies).ToList();
        }

        private static ZNetView Place(Player player, GameObject prefab, Order order, int stack, int index, int count)
        {
            Vector3 at = Spot(player, prefab, order.Distance, index, count);
            Quaternion facing = Quaternion.Euler(0f, player.transform.eulerAngles.y + 180f, 0f);
            GameObject copy = UnityEngine.Object.Instantiate(prefab, at, facing);
            ZNetView view = copy.GetComponent<ZNetView>();
            if (view && view.IsValid()) view.GetZDO().SetCheated(!PlayerProfile.s_bypassCheatChecks);
            ItemDrop.OnCreateNew(copy, !PlayerProfile.s_bypassCheatChecks);
            Dress(copy, order, stack);
            return view;
        }

        // In front of the player, several in a loose circle there; what falls starts a metre up, the rest stands on what is below.
        private static Vector3 Spot(Player player, GameObject prefab, float distance, int index, int count)
        {
            Transform from = player.transform;
            Vector3 at = from.position + from.forward * Mathf.Clamp(distance, 1f, 40f);
            if (count > 1) at += Quaternion.Euler(0f, index * 137.5f, 0f) * Vector3.forward * (0.6f * Mathf.Sqrt(index + 1));
            if (prefab.GetComponent<Rigidbody>()) return at + Vector3.up;
            return ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(at, out float ground, 3) ? new Vector3(at.x, ground, at.z) : at;
        }

        private static void Dress(GameObject copy, Order order, int stack)
        {
            ItemDrop item = copy.GetComponent<ItemDrop>();
            if (item)
            {
                item.SetStack(stack);
                item.SetQuality(Mathf.Clamp(order.Level, 1, Mathf.Max(1, item.m_itemData.m_shared.m_maxQuality)));
                item.m_itemData.m_durability = item.m_itemData.GetMaxDurability();
            }
            Character character = copy.GetComponent<Character>();
            if (!character) return;
            if (order.Level > 1) character.SetLevel(Mathf.Clamp(order.Level, 1, 10));
            if (!order.Tamed) return;
            MonsterAI ai = copy.GetComponent<MonsterAI>();
            if (ai) ai.MakeTame();
            else character.SetTamed(true);
        }

        private static void Remember(List<ZDOID> batch)
        {
            if (batch.Count == 0) return;
            Batches.Add(batch);
            if (Batches.Count > MostBatches) Batches.RemoveAt(0);
        }
    }
}
