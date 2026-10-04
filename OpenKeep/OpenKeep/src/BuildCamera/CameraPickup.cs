using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Camera Pickup, after the local player's fixed update (where the game's own auto pickup works around the body):
    /// items within the player's auto pickup range of the camera fly to it and land in the inventory, by the game's
    /// rules and path (<c>Player.AutoPickup</c>): auto pickup on (its key), the item's own auto pickup flag, not a
    /// piece, not a unique item already had, not stuck in tar, room and carry weight, ownership asked for first,
    /// <c>Humanoid.Pickup</c> at the end. With Pickup Needs Resting or Pickup Min Comfort unmet, the items stay where
    /// they are and the pickup panel says what is missing.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
    public static class CameraPickup
    {
        private const float PullSpeed = 15f;
        private const float TakeDistance = 0.3f;

        private static readonly Collider[] hits = new Collider[100];
        private static readonly List<ItemDrop> items = new List<ItemDrop>();
        private static readonly List<FloatingTerrainDummy> dummies = new List<FloatingTerrainDummy>();
        private static int mask;

        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (!CameraState.IsOut(__instance) || !CameraSettings.CameraPickup.Value || !Player.m_enableAutoPickup)
                return;
            if (__instance.IsDead() || __instance.IsTeleporting() || Collect(__instance) == 0)
                return;
            string missing = CameraNeeds.Describe(__instance, CameraSettings.PickupNeedsResting, CameraSettings.PickupMinComfort);
            if (missing != null)
            {
                PickupPanel.Show(missing);
                return;
            }
            for (int i = 0; i < items.Count; i++)
                Pull(__instance, items[i], dummies[i], Time.fixedDeltaTime);
        }

        /// <summary>The items the game would pick up, lying within range of the camera.</summary>
        private static int Collect(Player player)
        {
            items.Clear();
            dummies.Clear();
            if (mask == 0)
                mask = LayerMask.GetMask("item");
            int count = Physics.OverlapSphereNonAlloc(CameraState.Position, player.m_autoPickupRange, hits, mask);
            for (int i = 0; i < count; i++)
            {
                ItemDrop item = ItemOf(hits[i], out FloatingTerrainDummy dummy);
                if (item == null || items.Contains(item) || !Wanted(player, item))
                    continue;
                items.Add(item);
                dummies.Add(dummy);
            }
            return items.Count;
        }

        private static ItemDrop ItemOf(Collider collider, out FloatingTerrainDummy dummy)
        {
            dummy = null;
            Rigidbody body = collider.attachedRigidbody;
            if (body == null)
                return null;
            ItemDrop item = body.GetComponent<ItemDrop>();
            if (item != null)
                return item;
            dummy = body.GetComponent<FloatingTerrainDummy>();
            return dummy != null && dummy.m_parent != null ? dummy.m_parent.GetComponent<ItemDrop>() : null;
        }

        private static bool Wanted(Player player, ItemDrop item)
        {
            ZNetView view = item.GetComponent<ZNetView>();
            return item.m_autoPickup && !item.IsPiece() && view != null && view.IsValid()
                && !player.HaveUniqueKey(item.m_itemData.m_shared.m_name);
        }

        /// <summary>One step of the game's pickup: own it, check room and weight, take it when close, else pull it in.</summary>
        private static void Pull(Player player, ItemDrop item, FloatingTerrainDummy dummy, float dt)
        {
            if (!item.CanPickup())
            {
                item.RequestOwn();
                return;
            }
            if (item.InTar() || !Fits(player, item))
                return;
            Vector3 target = CameraState.Position;
            float distance = Vector3.Distance(item.transform.position, target);
            if (distance > player.m_autoPickupRange)
                return;
            if (distance < TakeDistance)
            {
                player.Pickup(item.gameObject);
                return;
            }
            Vector3 step = (target - item.transform.position).normalized * PullSpeed * dt;
            item.transform.position += step;
            if (dummy != null)
                dummy.transform.position += step;
        }

        private static bool Fits(Player player, ItemDrop item)
        {
            item.Load();
            Inventory inventory = player.GetInventory();
            return inventory.CanAddItem(item.m_itemData)
                && item.m_itemData.GetWeight() + inventory.GetTotalWeight() <= player.GetMaxCarryWeight();
        }
    }
}
