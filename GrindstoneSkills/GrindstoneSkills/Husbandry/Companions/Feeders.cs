using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The animal feeders loaded on this machine, what they hold and taking food out of one.
    /// <list type="bullet">
    /// <item>Every feeder adds itself in a Container.Awake postfix, only an instance with a ZDO (not the build ghost),
    /// and registers <see cref="Keys.RpcFeederTake"/> on its view beside the game's own container RPCs. A destroyed one
    /// (unloaded, taken down) drops out at once (<see cref="FeederLife"/>), and the list is emptied when the world's net
    /// scene goes, so it holds only the feeders loaded now and eating needs no physics search.</item>
    /// <item>A container's inventory is changed only by its owner, which saves it to the ZDO; every other machine reloads
    /// it from the ZDO within a second of a change (Container.CheckForChanges). That copy is what a creature's owner reads
    /// to decide what a feeder holds.</item>
    /// <item><see cref="TakeOne"/> removes one item on the spot when this machine owns the feeder, and otherwise asks the
    /// owner by RPC (item shared name), who removes one if it still has one.</item>
    /// </list>
    /// </summary>
    public static class Feeders
    {
        private static readonly List<Container> Loaded = new List<Container>();

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view != null && view.GetZDO() != null && view.GetZDO().GetPrefab() == FeederPrefab.Hash)
                    Track(__instance, view);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
        private static class WorldExit
        {
            [HarmonyPostfix]
            private static void Postfix() => Loaded.Clear();
        }

        /// <summary>
        /// A feeder that just woke: its RPC, its place in the list and the component that takes it out again. Apart from
        /// the postfix, so only a feeder pays for the handler's closure, not every container that wakes.
        /// </summary>
        private static void Track(Container feeder, ZNetView view)
        {
            view.Register<string>(Keys.RpcFeederTake, (sender, itemName) => Receive(feeder, itemName));
            feeder.gameObject.AddComponent<FeederLife>().Feeder = feeder;
            Loaded.Add(feeder);
        }

        /// <summary>Takes a destroyed feeder out of the list (<see cref="FeederLife"/>).</summary>
        public static void Forget(Container feeder) => Loaded.Remove(feeder);

        /// <summary>A loaded feeder whose view and inventory are there.</summary>
        public static bool IsUsable(Container feeder) =>
            feeder != null && feeder.m_nview != null && feeder.m_nview.IsValid() && feeder.GetInventory() != null;

        /// <summary>Whether the feeder still holds at least one of this food (any world level).</summary>
        public static bool Holds(Container feeder, ItemDrop food) =>
            IsUsable(feeder) && food != null && feeder.GetInventory().HaveItem(food.m_itemData.m_shared.m_name, false);

        /// <summary>The nearest loaded feeder within <paramref name="range"/> metres holding food the creature eats; null when none.</summary>
        public static Container Nearest(MonsterAI ai, float range, out ItemDrop food)
        {
            Loaded.RemoveAll(feeder => feeder == null);
            Vector3 position = ai.transform.position;
            float best = range * range;
            Container nearest = null;
            food = null;
            foreach (Container feeder in Loaded)
            {
                float distance = (feeder.transform.position - position).sqrMagnitude;
                ItemDrop eats = distance <= best && IsUsable(feeder) ? FoodIn(feeder, ai) : null;
                if (eats == null)
                    continue;
                best = distance;
                nearest = feeder;
                food = eats;
            }
            return nearest;
        }

        /// <summary>The first of the creature's foods (its own order, m_consumeItems) the feeder holds; null when none.</summary>
        public static ItemDrop FoodIn(Container feeder, MonsterAI ai)
        {
            Inventory inventory = feeder.GetInventory();
            foreach (ItemDrop item in ai.m_consumeItems)
                if (item != null && inventory.HaveItem(item.m_itemData.m_shared.m_name, false))
                    return item;
            return null;
        }

        /// <summary>Takes one item of this shared name out of the feeder, through its owner.</summary>
        public static void TakeOne(Container feeder, string itemName)
        {
            if (!IsUsable(feeder) || string.IsNullOrEmpty(itemName))
                return;
            if (feeder.IsOwner())
                Remove(feeder, itemName);
            else
                feeder.m_nview.InvokeRPC(Keys.RpcFeederTake, itemName);
        }

        private static void Receive(Container feeder, string itemName) =>
            Guard.Run(Keys.RpcFeederTake, () =>
            {
                if (IsUsable(feeder) && feeder.IsOwner() && !string.IsNullOrEmpty(itemName))
                    Remove(feeder, itemName);
            });

        /// <summary>On the feeder's owner: one item fewer; the game's change callback saves the inventory to the ZDO.</summary>
        private static void Remove(Container feeder, string itemName)
        {
            Inventory inventory = feeder.GetInventory();
            if (inventory.HaveItem(itemName, false))
                inventory.RemoveItem(itemName, 1, -1, false);
        }
    }
}
