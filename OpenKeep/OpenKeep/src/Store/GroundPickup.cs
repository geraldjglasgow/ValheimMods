using System;
using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Store
{
    /// <summary>
    /// Ground pickup (SPEC 2.2), hooked on the container's own once-a-second <c>CheckForChanges</c>: on the client
    /// that owns the container's ZDO, once per <c>Pickup Interval</c> (staggered by instance id), never while the
    /// container is in use, for prefabs with <c>pickup: true</c>. Dropped items within <c>Pickup Range</c> that have
    /// lain longer than <c>Pickup Delay</c> (the ZDO's spawn time), are wanted by the rule and are not inside a ward
    /// the local player may not use are added with the game's add method and destroyed through the scene, or reduced
    /// by what fitted, once this client owns them: a drop another client owns is asked for with the game's own pickup
    /// request and taken on a later sweep. Items that were placed as pieces are left alone. When several pickup chests reach a
    /// drop, it is left to the one that ranks first (<see cref="PickupOrder"/>: a chest holding the item before one
    /// that only accepts it, then the nearest to the drop), and to the next when that one is full.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.CheckForChanges))]
    public static class GroundPickup
    {
        private static readonly Dictionary<Container, float> nextSweep = new Dictionary<Container, float>();
        private static readonly List<ItemDrop> drops = new List<ItemDrop>();
        private static readonly PruneMark pruneMark = new PruneMark(64);

        [HarmonyPostfix]
        public static void Postfix(Container __instance)
        {
            Tick(__instance);
        }

        public static void Tick(Container container)
        {
            if (!StoreSettings.Enabled.Value || !StoreSettings.GroundPickup.Value || container == null)
                return;
            ZNetView view = container.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner() || container.IsInUse() || !StoreRules.PicksUp(container))
                return;
            if (Player.m_localPlayer == null || ZNetScene.instance == null || !Due(container))
                return;
            if (!ContainerScan.IsUsable(container, ContainerUse.Store))
                return;
            using (SaveHolds.Hold(container))
            {
                if (Sweep(container) > 0)
                    ContainerScan.Save(container);
            }
        }

        private static bool Due(Container container)
        {
            float now = Time.time;
            float interval = Mathf.Max(1f, StoreSettings.PickupInterval.Value);
            if (!nextSweep.TryGetValue(container, out float due))
            {
                if (pruneMark.Due(nextSweep.Count))
                    Prune();
                nextSweep[container] = now + interval * ((container.GetInstanceID() & 0xFF) / 256f);
                return false;
            }
            if (now < due)
                return false;
            nextSweep[container] = now + interval;
            return true;
        }

        private static void Prune()
        {
            List<Container> gone = new List<Container>();
            foreach (Container key in nextSweep.Keys)
            {
                if (key == null)
                    gone.Add(key);
            }
            foreach (Container key in gone)
                nextSweep.Remove(key);
            pruneMark.Pruned(nextSweep.Count);
        }

        private static int Sweep(Container container)
        {
            Vector3 origin = container.transform.position;
            float range = StoreSettings.PickupRange.Value;
            List<Container> rivals = null;
            int taken = 0;
            drops.AddRange(ItemDrop.s_instances);
            foreach (ItemDrop drop in drops)
            {
                if (drop == null || !Eligible(container, drop, origin, range))
                    continue;
                rivals = rivals ?? PickupOrder.Rivals(container, range);
                if (PickupOrder.IsFirst(container, drop, rivals, range))
                    taken += Take(container, drop);
            }
            drops.Clear();
            return taken;
        }

        private static bool Eligible(Container container, ItemDrop drop, Vector3 origin, float range)
        {
            if ((origin - drop.transform.position).sqrMagnitude > range * range)
                return false;
            ZNetView view = drop.m_nview;
            if (view == null || !view.IsValid() || drop.m_itemData == null || drop.m_itemData.m_shared == null || drop.IsPiece())
                return false;
            if (Age(view) < StoreSettings.PickupDelay.Value || !Wanted(container, drop.m_itemData))
                return false;
            return !CoreSettings.HonourWards.Value || PrivateArea.CheckAccess(drop.transform.position, 0f, false);
        }

        /// <summary>Seconds since the drop's ZDO spawn time; negative when the time is unknown.</summary>
        private static double Age(ZNetView view)
        {
            long ticks = view.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
            if (ticks == 0L || ZNet.instance == null)
                return -1.0;
            return (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds;
        }

        /// <summary>The container's rules want the item from the ground: not refused, and accepted or (with Pickup Only Held Items) already held.</summary>
        internal static bool Wanted(Container container, ItemDrop.ItemData item)
        {
            if (StoreRules.Refuses(container, item))
                return false;
            if (StoreRules.Accepts(container, item))
                return true;
            return !StoreSettings.PickupOnlyHeldItems.Value || container.GetInventory().ContainsItemByName(item.m_shared.m_name);
        }

        /// <summary>This client owns the drop now (a free one is claimed); one another client owns is asked for, with the game's backoff.</summary>
        private static bool Owned(ItemDrop drop)
        {
            ZNetView view = drop.m_nview;
            if (view.IsOwner())
                return true;
            if (view.GetZDO().HasOwner())
            {
                drop.RequestOwn();
                return false;
            }
            view.ClaimOwnership();
            return view.IsOwner();
        }

        /// <summary>
        /// Takes a drop this client owns (one nobody owns is claimed first, the way the game claims a free object). A drop
        /// another client owns is asked for with the game's own pickup request and taken on a later sweep, once it is
        /// ours, so two clients never both pick it up.
        /// </summary>
        private static int Take(Container container, ItemDrop drop)
        {
            if (!Owned(drop))
                return 0;
            drop.Load();
            ItemDrop.ItemData data = drop.m_itemData.Clone();
            int before = data.m_stack;
            if (before <= 0)
                return 0;
            if (container.GetInventory().AddItem(data))
            {
                ZNetScene.instance.Destroy(drop.gameObject);
                return before;
            }
            int taken = before - data.m_stack;
            if (taken > 0)
            {
                drop.m_itemData.m_stack = data.m_stack;
                drop.Save();
            }
            return taken;
        }
    }
}
