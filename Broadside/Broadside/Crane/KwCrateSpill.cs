using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A broken crate's items spilled round where it landed, on its owner (<see cref="KwCrateBreak"/>): each inventory slot
    /// dropped as one pickup holding its whole stack, by the game's own <c>ItemDrop.DropItem</c> (quality, durability, variant,
    /// crafter and custom data kept, as the game drops a destroyed chest's items), then the inventory emptied and saved, all
    /// before the crate is destroyed, so nothing is lost or doubled on any peer. Each lands within <see cref="Reach"/> of the
    /// landing on whatever surface is there (the ground, a floor, a deck, a cart's bed), in sight of the landing (not through
    /// a wall), clear of anything solid, a little over it so it settles there; over water it floats as the game's drops do; with
    /// no such spot in a few tries, at the landing itself. A stack of more than one is marked for its heap
    /// (<see cref="KwCrateHeap"/>).
    /// </summary>
    public static class KwCrateSpill
    {
        /// <summary>How far round the landing the items go (m).</summary>
        public const float Reach = 1.5f;

        private const int Tries = 6;
        private const float Eye = 0.5f, Over = 0.25f, Clear = 0.15f, Down = 3f;

        /// <summary>On the owner: every slot dropped whole round <paramref name="at"/>, the inventory emptied (saved by the Container).</summary>
        public static void Out(Container box, Vector3 at, int seed)
        {
            Inventory inventory = box.GetInventory();
            System.Random random = new System.Random(seed);
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(inventory.GetAllItems()), kept = new List<ItemDrop.ItemData>();
            for (int i = 0; i < items.Count; i++)
            {
                if (!Drop(items[i], Spot(at, i, random), random))
                    kept.Add(items[i]);
            }
            if (kept.Count == 0)
                inventory.RemoveAll();
            else
                Take(inventory, items, kept);
        }

        private static void Take(Inventory inventory, List<ItemDrop.ItemData> items, List<ItemDrop.ItemData> kept)
        {
            foreach (ItemDrop.ItemData item in items)
            {
                if (!kept.Contains(item))
                    inventory.RemoveItem(item);
            }
        }

        // An item with no drop prefab (never one loaded from a ZDO) stays in the inventory for the game's own drop.
        private static bool Drop(ItemDrop.ItemData item, Vector3 at, System.Random random)
        {
            if (item.m_dropPrefab == null)
                return false;
            ItemDrop drop = ItemDrop.DropItem(item, 0, at, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            if (item.m_stack > 1 && drop.m_nview != null && drop.m_nview.GetZDO() is ZDO zdo)
            {
                zdo.Set(KwCrateHeap.HeapHash, true);
                KwCrateHeap.Add(drop);
            }
            return true;
        }

        // A spot round the landing (spread on a sunflower, a few tries each), else the landing itself.
        private static Vector3 Spot(Vector3 at, int index, System.Random random)
        {
            Vector3 eye = at + Vector3.up * Eye;
            for (int t = 0; t < Tries; t++)
            {
                float angle = (index * 137.5f + t * 61f) * Mathf.Deg2Rad + (float)random.NextDouble() * 0.5f;
                float radius = Reach * Mathf.Sqrt(0.1f + 0.9f * (float)random.NextDouble());
                Vector3? found = Surface(eye, eye + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
                if (found.HasValue)
                    return found.Value;
            }
            return eye;
        }

        // The surface under a spot seen from the landing, a little over it (over the water where that is higher), or null.
        private static Vector3? Surface(Vector3 eye, Vector3 aim)
        {
            int solid = KwCargo.Solid;
            if (Physics.Linecast(eye, aim, solid, QueryTriggerInteraction.Ignore))
                return null;
            if (!Physics.Raycast(aim + Vector3.up * Eye, Vector3.down, out RaycastHit hit, Down, solid, QueryTriggerInteraction.Ignore))
                return null;
            Vector3 spot = hit.point + Vector3.up * Over;
            float? water = KwCargoCast.Water(spot, 0f);
            if (water.HasValue && water.Value > spot.y)
                spot.y = water.Value + 0.1f;
            return Physics.CheckSphere(spot, Clear, solid, QueryTriggerInteraction.Ignore) ? (Vector3?)null : spot;
        }
    }
}
