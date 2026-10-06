using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Which pickup chest takes a drop (the user's rule for every automatic store, 2026-09-29: "it should try to store
    /// in the chest closest to it. if that chest is full then the next closest"). Among the pickup chests whose range
    /// reaches the drop and that would take it now (a pickup chest, not in use, wanting the item, room for one more), a
    /// chest that already holds the item comes before one that only accepts it, and a nearer one before a farther one,
    /// measured from the drop. A chest sweeps a drop only when none ranks before it; a full chest has no room, so the
    /// next one takes the drop on its own sweep. Every chest sweeps on its own owner's client, so each owner decides from
    /// the same replicated contents; a drop a nearer chest will take simply waits for that chest's next sweep. So a
    /// kiln's coal dropping at its output goes into the nearest pickup chest holding coal, and on when that one is full.
    /// </summary>
    internal static class PickupOrder
    {
        /// <summary>A chest that holds the item ranks this far ahead of one that only accepts it, whatever the distances.</summary>
        private const float HolderLead = 100000f;

        /// <summary>
        /// The other usable pickup chests that could reach a drop this chest reaches: within twice the pickup range of
        /// it. Found once per sweep; <see cref="IsFirst"/> narrows them to the ones in range of each drop.
        /// </summary>
        public static List<Container> Rivals(Container container, float range)
        {
            List<Container> rivals = ContainerScan.Nearby(container.transform.position, range * 2f, ContainerUse.Stow);
            rivals.RemoveAll(rival => rival == container || !StowRules.PicksUp(rival));
            return rivals;
        }

        /// <summary>True when no rival in reach of the drop ranks before <paramref name="container"/> and can take it now.</summary>
        public static bool IsFirst(Container container, ItemDrop drop, List<Container> rivals, float range)
        {
            ItemDrop.ItemData item = drop.m_itemData;
            Vector3 at = drop.transform.position;
            float rank = Rank(container, item, at);
            float reach = range * range;
            foreach (Container rival in rivals)
            {
                if (rival == null || (rival.transform.position - at).sqrMagnitude > reach)
                    continue;
                if (Rank(rival, item, at) < rank && TakesNow(rival, item))
                    return false;
            }
            return true;
        }

        /// <summary>Holders first, then nearer first: the lower, the sooner.</summary>
        private static float Rank(Container container, ItemDrop.ItemData item, Vector3 at)
        {
            bool holds = container.GetInventory().ContainsItemByName(item.m_shared.m_name);
            return (holds ? 0f : HolderLead) + Vector3.Distance(container.transform.position, at);
        }

        /// <summary>The rival would sweep this item up on its own turn: a pickup chest, free, wanting it, with room for one.</summary>
        private static bool TakesNow(Container rival, ItemDrop.ItemData item) =>
            StowRules.PicksUp(rival) && !rival.IsInUse() && GroundPickup.Wanted(rival, item) && rival.GetInventory().CanAddItem(item, 1);
    }
}
