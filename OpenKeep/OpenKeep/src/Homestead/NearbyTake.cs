using System;
using System.Collections.Generic;
using OpenKeep.Core;
using OpenKeep.Reach;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Takes a fire's fuel or a station's ore and fuel out of the containers near it (not near a player), nearest
    /// first. The caller finds them with <c>ContainerScan.Nearby</c> (usable: loaded, not open by another player, the
    /// local player's ward and privacy access, the section 0 switches, the prefab enabled); here each is claimed, the
    /// units removed and the container saved. What may be taken is Reach's: the caller's predicate (the station's own
    /// items narrowed by its prefab's <c>stations:</c> allow/deny, <c>StationAccepts</c>), per stack the container
    /// prefab's allow/deny (<c>ReachRules.RuleFor</c>), and the game's world level rule, which keeps items of an
    /// easier world level out of stations as <c>Inventory.GetItem</c> and <c>HaveItem</c> do. Needs a local player
    /// (the access checks are that player's), so a dedicated server takes nothing.
    /// </summary>
    public static class NearbyTake
    {
        /// <summary>
        /// Removes up to <paramref name="wanted"/> units; returns how many left the containers. <paramref name="removed"/>,
        /// when given, hears each stack taken from with the count, so a caller can pass the item on to the game.
        /// </summary>
        public static int Take(List<Container> containers, Func<ItemDrop.ItemData, bool> accepts, int wanted, Action<ItemDrop.ItemData, int> removed = null)
        {
            Func<ItemDrop.ItemData, bool> usable = item => item.m_worldLevel >= Game.m_worldLevel && accepts(item);
            int taken = 0;
            foreach (Container container in containers)
            {
                if (taken >= wanted)
                    break;
                if (ReachCount.CountIn(container, usable) <= 0 || !ContainerScan.Claim(container))
                    continue;
                taken += TakeFrom(container, usable, wanted - taken, removed);
            }
            return taken;
        }

        /// <summary>Removes from the acceptable stacks of one claimed container, then saves it through the game.</summary>
        private static int TakeFrom(Container container, Func<ItemDrop.ItemData, bool> accepts, int wanted, Action<ItemDrop.ItemData, int> removed)
        {
            ContainerRule rule = ReachRules.RuleFor(container);
            Inventory inventory = container.GetInventory();
            int taken = 0;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (taken >= wanted)
                    break;
                if (!accepts(item) || !rule.Accepts(item))
                    continue;
                int take = Math.Min(item.m_stack, wanted - taken);
                if (!inventory.RemoveItem(item, take))
                    continue;
                taken += take;
                removed?.Invoke(item, take);
            }
            if (taken > 0)
                ContainerScan.Save(container);
            return taken;
        }
    }
}
